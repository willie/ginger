using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ginger.Models;

namespace Ginger.Services;

/// <summary>
/// Service for loading and saving character cards in various formats.
/// </summary>
public class CharacterCardService
{
    public enum LoadResult
    {
        Success,
        FileNotFound,
        InvalidFormat,
        NoDataFound,
        ReadError,
    }

    /// <summary>
    /// Load a character card from a file. Supports PNG, JSON, YAML, and BYAF formats.
    /// </summary>
    public async Task<(LoadResult result, CharacterCard? card)> LoadAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return (LoadResult.FileNotFound, null);

        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        try
        {
            return ext switch
            {
                ".png" => await LoadFromPngAsync(filePath),
                ".json" => await LoadFromJsonAsync(filePath),
                ".yaml" => await LoadFromYamlAsync(filePath),
                ".byaf" => await LoadFromByafAsync(filePath),
                _ => (LoadResult.InvalidFormat, null)
            };
        }
        catch
        {
            return (LoadResult.ReadError, null);
        }
    }

    /// <summary>
    /// Load a character card from a PNG file with embedded metadata.
    /// </summary>
    private async Task<(LoadResult result, CharacterCard? card)> LoadFromPngAsync(string filePath)
    {
        // Read PNG metadata
        var metadata = await Task.Run(() => PngMetadata.ReadTextChunks(filePath));

        if (metadata.Count == 0)
            return (LoadResult.NoDataFound, null);

        // Read PNG image data
        byte[] imageData = await File.ReadAllBytesAsync(filePath);

        // Try to find character data in various formats
        string? charaBase64 = null;
        string? ccv3Base64 = null;

        if (metadata.TryGetValue("ccv3", out ccv3Base64))
        {
            // TavernCardV3 format
            try
            {
                byte[] jsonBytes = Convert.FromBase64String(ccv3Base64);
                string json = Encoding.UTF8.GetString(jsonBytes);
                var tavernV2 = TavernCardV2.FromJson(json, out _);
                if (tavernV2 != null)
                {
                    var card = CharacterCard.FromTavernV2(tavernV2, imageData);
                    card.SourceFormat = CharacterCard.CardFormat.TavernV3;
                    return (LoadResult.Success, card);
                }
            }
            catch { }
        }

        if (metadata.TryGetValue("chara", out charaBase64))
        {
            // TavernCardV2 format (most common)
            try
            {
                byte[] jsonBytes = Convert.FromBase64String(charaBase64);
                string json = Encoding.UTF8.GetString(jsonBytes);
                var tavernV2 = TavernCardV2.FromJson(json, out _);
                if (tavernV2 != null)
                {
                    var card = CharacterCard.FromTavernV2(tavernV2, imageData);
                    return (LoadResult.Success, card);
                }
            }
            catch { }
        }

        // Try ginger format
        if (metadata.TryGetValue("ginger", out string? gingerBase64))
        {
            // Ginger native format (XML) - for now just fall through
            // TODO: Implement GingerCardV1 parser
        }

        return (LoadResult.NoDataFound, null);
    }

    /// <summary>
    /// Load a character card from a JSON file.
    /// </summary>
    private async Task<(LoadResult result, CharacterCard? card)> LoadFromJsonAsync(string filePath)
    {
        string json = await File.ReadAllTextAsync(filePath);

        // Try TavernCardV2 format first
        var tavernV2 = TavernCardV2.FromJson(json, out _);
        if (tavernV2 != null)
        {
            var card = CharacterCard.FromTavernV2(tavernV2);
            return (LoadResult.Success, card);
        }

        // Try Faraday format
        if (FaradayCard.Validate(json))
        {
            var faraday = FaradayCard.FromJson(json);
            if (faraday != null)
            {
                var card = faraday.ToCharacterCard();
                return (LoadResult.Success, card);
            }
        }

        return (LoadResult.InvalidFormat, null);
    }

    /// <summary>
    /// Load a character card from a .byaf (Backyard AI Archive) file.
    /// </summary>
    private async Task<(LoadResult result, CharacterCard? card)> LoadFromByafAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

                ByafManifest? manifest = null;
                ByafCharacter? byafCharacter = null;
                FaradayCard? faradayCharacter = null;
                byte[]? portraitData = null;
                string? characterPath = null;

                // Read manifest
                var manifestEntry = archive.GetEntry("manifest.json");
                if (manifestEntry != null)
                {
                    using var reader = new StreamReader(manifestEntry.Open());
                    string json = reader.ReadToEnd();
                    manifest = ByafManifest.FromJson(json);
                }

                if (manifest == null || manifest.Characters.Length == 0)
                    return (LoadResult.NoDataFound, null);

                // Get first character
                characterPath = manifest.Characters[0];
                var charEntry = archive.GetEntry(characterPath);
                if (charEntry != null)
                {
                    using var reader = new StreamReader(charEntry.Open());
                    string json = reader.ReadToEnd();

                    // Try BYAF archive format first (flat structure with schemaVersion)
                    byafCharacter = ByafCharacter.FromJson(json);

                    // Fall back to FaradayCard format (nested "character" structure)
                    if (byafCharacter == null)
                        faradayCharacter = FaradayCard.FromJson(json);
                }

                if (byafCharacter == null && faradayCharacter == null)
                    return (LoadResult.InvalidFormat, null);

                // Try to find portrait image from character's images array
                string charDir = Path.GetDirectoryName(characterPath)?.Replace('\\', '/') ?? "";
                string? imagePath = byafCharacter?.GetFirstImagePath();

                if (!string.IsNullOrEmpty(imagePath))
                {
                    string fullPath = string.IsNullOrEmpty(charDir) ? imagePath : $"{charDir}/{imagePath}";
                    var imgEntry = archive.Entries.FirstOrDefault(e =>
                        e.FullName.Replace('\\', '/').Equals(fullPath, StringComparison.OrdinalIgnoreCase));

                    if (imgEntry != null)
                    {
                        using var ms = new MemoryStream();
                        using var imgStream = imgEntry.Open();
                        imgStream.CopyTo(ms);
                        portraitData = ms.ToArray();
                    }
                }

                // Fallback: look for any image in the character's images folder
                if (portraitData == null && !string.IsNullOrEmpty(charDir))
                {
                    var anyImage = archive.Entries.FirstOrDefault(e =>
                        e.FullName.Replace('\\', '/').StartsWith($"{charDir}/images/", StringComparison.OrdinalIgnoreCase) &&
                        (e.Name.EndsWith(".png") || e.Name.EndsWith(".jpg") || e.Name.EndsWith(".webp")));

                    if (anyImage != null)
                    {
                        using var ms = new MemoryStream();
                        using var imgStream = anyImage.Open();
                        imgStream.CopyTo(ms);
                        portraitData = ms.ToArray();
                    }
                }

                // Convert to CharacterCard
                CharacterCard card;
                if (byafCharacter != null)
                    card = byafCharacter.ToCharacterCard(portraitData);
                else
                    card = faradayCharacter!.ToCharacterCard(portraitData);

                // Add creator info from manifest
                if (manifest.Author != null && !string.IsNullOrEmpty(manifest.Author.Name))
                {
                    card.Creator = manifest.Author.Name;
                    if (!string.IsNullOrEmpty(manifest.Author.BackyardUrl))
                        card.CreatorNotes = $"From Backyard AI: {manifest.Author.BackyardUrl}";
                }

                return (LoadResult.Success, card);
            }
            catch
            {
                return (LoadResult.ReadError, null);
            }
        });
    }

    /// <summary>
    /// Load a character card from a YAML file (TextGenWebUI format).
    /// </summary>
    private async Task<(LoadResult result, CharacterCard? card)> LoadFromYamlAsync(string filePath)
    {
        string yaml = await File.ReadAllTextAsync(filePath);

        // Try TextGenWebUI format
        var textGenCard = TextGenWebUICard.FromYaml(yaml);
        if (textGenCard != null)
        {
            var card = textGenCard.ToCharacterCard();
            return (LoadResult.Success, card);
        }

        return (LoadResult.InvalidFormat, null);
    }

    /// <summary>
    /// Save a character card to a file.
    /// </summary>
    public async Task<bool> SaveAsync(string filePath, CharacterCard card)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        try
        {
            return ext switch
            {
                ".png" => await SaveToPngAsync(filePath, card),
                ".json" => await SaveToJsonAsync(filePath, card),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Save a character card to a PNG file with embedded metadata.
    /// </summary>
    private async Task<bool> SaveToPngAsync(string filePath, CharacterCard card)
    {
        // Need existing PNG data (portrait)
        if (card.PortraitData == null || card.PortraitData.Length == 0)
            return false;

        // Convert to TavernCardV2 JSON
        var tavernCard = card.ToTavernV2();
        string json = tavernCard.ToJson();
        string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        // Write PNG with metadata
        var metadata = new System.Collections.Generic.Dictionary<string, string>
        {
            { "chara", base64 }
        };

        byte[] outputData = PngMetadata.WriteTextChunks(card.PortraitData, metadata);
        await File.WriteAllBytesAsync(filePath, outputData);

        return true;
    }

    /// <summary>
    /// Save a character card to a JSON file.
    /// </summary>
    private async Task<bool> SaveToJsonAsync(string filePath, CharacterCard card)
    {
        var tavernCard = card.ToTavernV2();
        string json = tavernCard.ToJson();
        await File.WriteAllTextAsync(filePath, json);
        return true;
    }
}
