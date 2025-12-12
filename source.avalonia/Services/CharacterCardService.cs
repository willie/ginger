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
                ".charx" => await LoadFromCharxAsync(filePath),
                ".xml" => await LoadFromXmlAsync(filePath),
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
                var tavernV3 = TavernCardV3.FromJson(json, out _);
                if (tavernV3 != null)
                {
                    var card = CharacterCard.FromTavernV3(tavernV3, imageData);
                    return (LoadResult.Success, card);
                }
                // Fall back to V2 parser if V3 fails
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

        // Try Ginger native format (XML)
        if (metadata.TryGetValue("ginger", out string? gingerBase64))
        {
            try
            {
                byte[] xmlBytes = Convert.FromBase64String(gingerBase64);
                string xml = Encoding.UTF8.GetString(xmlBytes);
                var gingerCard = GingerCardV1.FromXml(xml);
                if (gingerCard != null)
                {
                    var card = CharacterCard.FromGingerV1(gingerCard, imageData);
                    return (LoadResult.Success, card);
                }
            }
            catch { }
        }

        return (LoadResult.NoDataFound, null);
    }

    /// <summary>
    /// Load a character card from a JSON file.
    /// </summary>
    private async Task<(LoadResult result, CharacterCard? card)> LoadFromJsonAsync(string filePath)
    {
        string json = await File.ReadAllTextAsync(filePath);

        // Try TavernCardV3 format first (newest)
        var tavernV3 = TavernCardV3.FromJson(json, out _);
        if (tavernV3 != null)
        {
            var card = CharacterCard.FromTavernV3(tavernV3);
            return (LoadResult.Success, card);
        }

        // Try TavernCardV2 format
        var tavernV2 = TavernCardV2.FromJson(json, out _);
        if (tavernV2 != null)
        {
            var card = CharacterCard.FromTavernV2(tavernV2);
            return (LoadResult.Success, card);
        }

        // Try Agnaistic format
        if (AgnaisticCard.Validate(json))
        {
            var agn = AgnaisticCard.FromJson(json, out _);
            if (agn != null)
            {
                var card = CharacterCard.FromAgnaistic(agn);
                return (LoadResult.Success, card);
            }
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

        // Try Pygmalion format
        var pygmalion = PygmalionCard.FromJson(json);
        if (pygmalion != null)
        {
            var card = pygmalion.ToCharacterCard();
            return (LoadResult.Success, card);
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
    /// Load a character card from a .charx (Character Archive) file.
    /// CHARX is a zip archive containing card.json and optional assets.
    /// </summary>
    private async Task<(LoadResult result, CharacterCard? card)> LoadFromCharxAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

                // Look for card.json (standard CHARX format)
                var cardEntry = archive.GetEntry("card.json");
                if (cardEntry == null)
                {
                    // Try alternate names
                    cardEntry = archive.Entries.FirstOrDefault(e =>
                        e.Name.Equals("card.json", StringComparison.OrdinalIgnoreCase) ||
                        e.Name.Equals("character.json", StringComparison.OrdinalIgnoreCase));
                }

                if (cardEntry == null)
                    return (LoadResult.NoDataFound, null);

                string json;
                using (var reader = new StreamReader(cardEntry.Open()))
                {
                    json = reader.ReadToEnd();
                }

                // Try TavernCardV3 format first
                var tavernV3 = TavernCardV3.FromJson(json, out _);
                if (tavernV3 != null)
                {
                    var card = CharacterCard.FromTavernV3(tavernV3);
                    card.SourceFormat = CharacterCard.CardFormat.TavernV3;

                    // Try to find portrait image
                    card.PortraitData = ExtractPortraitFromArchive(archive);
                    return (LoadResult.Success, card);
                }

                // Try TavernCardV2 format
                var tavernV2 = TavernCardV2.FromJson(json, out _);
                if (tavernV2 != null)
                {
                    var card = CharacterCard.FromTavernV2(tavernV2);

                    // Try to find portrait image
                    card.PortraitData = ExtractPortraitFromArchive(archive);
                    return (LoadResult.Success, card);
                }

                return (LoadResult.InvalidFormat, null);
            }
            catch
            {
                return (LoadResult.ReadError, null);
            }
        });
    }

    /// <summary>
    /// Extract portrait image from a zip archive.
    /// </summary>
    private static byte[]? ExtractPortraitFromArchive(ZipArchive archive)
    {
        // Look for common image file names
        var imageEntry = archive.Entries.FirstOrDefault(e =>
            e.Name.Equals("avatar.png", StringComparison.OrdinalIgnoreCase) ||
            e.Name.Equals("portrait.png", StringComparison.OrdinalIgnoreCase) ||
            e.Name.Equals("image.png", StringComparison.OrdinalIgnoreCase) ||
            e.Name.Equals("card.png", StringComparison.OrdinalIgnoreCase));

        // Fall back to any PNG/JPG/WEBP in root or assets folder
        if (imageEntry == null)
        {
            imageEntry = archive.Entries.FirstOrDefault(e =>
                !e.FullName.Contains('/') &&
                (e.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                 e.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                 e.Name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)));
        }

        if (imageEntry == null)
        {
            imageEntry = archive.Entries.FirstOrDefault(e =>
                e.FullName.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) &&
                (e.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                 e.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                 e.Name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)));
        }

        if (imageEntry != null)
        {
            using var ms = new MemoryStream();
            using var imgStream = imageEntry.Open();
            imgStream.CopyTo(ms);
            return ms.ToArray();
        }

        return null;
    }

    /// <summary>
    /// Load a character card from a standalone XML file (Ginger native format).
    /// </summary>
    private async Task<(LoadResult result, CharacterCard? card)> LoadFromXmlAsync(string filePath)
    {
        string xml = await File.ReadAllTextAsync(filePath);

        var gingerCard = GingerCardV1.FromXml(xml);
        if (gingerCard != null)
        {
            var card = CharacterCard.FromGingerV1(gingerCard);
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
                ".yaml" => await SaveToYamlAsync(filePath, card),
                ".charx" => await SaveToCharxAsync(filePath, card),
                ".byaf" => await SaveToByafAsync(filePath, card),
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

    /// <summary>
    /// Save a character card to a YAML file (TextGenWebUI format).
    /// </summary>
    private async Task<bool> SaveToYamlAsync(string filePath, CharacterCard card)
    {
        var textGenCard = new TextGenWebUICard
        {
            name = card.Name,
            context = CombinePersonaAndScenario(card),
            greeting = card.Greeting,
            example = card.Example,
        };
        string yaml = textGenCard.ToYaml();
        if (yaml == null)
            return false;

        await File.WriteAllTextAsync(filePath, yaml);
        return true;
    }

    private static string CombinePersonaAndScenario(CharacterCard card)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(card.Persona))
            sb.AppendLine(card.Persona);
        if (!string.IsNullOrWhiteSpace(card.Scenario))
            sb.AppendLine(card.Scenario);
        return sb.ToString().Trim();
    }

    /// <summary>
    /// Save a character card to a CHARX archive (zip with card.json).
    /// </summary>
    private async Task<bool> SaveToCharxAsync(string filePath, CharacterCard card)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                using var archive = new ZipArchive(fs, ZipArchiveMode.Create);

                // Add card.json
                var tavernCard = card.ToTavernV2();
                string json = tavernCard.ToJson();
                var cardEntry = archive.CreateEntry("card.json");
                using (var writer = new StreamWriter(cardEntry.Open()))
                {
                    writer.Write(json);
                }

                // Add portrait image if available
                if (card.PortraitData != null && card.PortraitData.Length > 0)
                {
                    var imageEntry = archive.CreateEntry("avatar.png");
                    using var imageStream = imageEntry.Open();
                    imageStream.Write(card.PortraitData, 0, card.PortraitData.Length);
                }

                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Save a character card to a Backyard Archive (.byaf) file.
    /// </summary>
    private async Task<bool> SaveToByafAsync(string filePath, CharacterCard card)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                using var archive = new ZipArchive(fs, ZipArchiveMode.Create);

                string charId = Guid.NewGuid().ToString();
                string charPath = $"characters/{charId}/character.json";
                string imagePath = $"characters/{charId}/images/avatar.png";

                // Create manifest
                var manifest = new ByafManifest
                {
                    Characters = new[] { charPath }
                };
                string manifestJson = Newtonsoft.Json.JsonConvert.SerializeObject(manifest);
                var manifestEntry = archive.CreateEntry("manifest.json");
                using (var writer = new StreamWriter(manifestEntry.Open()))
                {
                    writer.Write(manifestJson);
                }

                // Create character JSON (using ByafCharacter format)
                var byafChar = new ByafCharacter
                {
                    SchemaVersion = 1,
                    Id = charId,
                    Name = card.SpokenName ?? card.Name,
                    DisplayName = card.Name,
                    Persona = card.Persona,
                };

                // Add image reference if portrait exists
                if (card.PortraitData != null && card.PortraitData.Length > 0)
                {
                    byafChar.Images = new[] { new ByafCharacter.ByafImage { Path = "images/avatar.png" } };
                }

                // Add lore items
                if (card.Lorebook?.Entries.Count > 0)
                {
                    byafChar.LoreItems = card.Lorebook.Entries
                        .Select(e => new ByafCharacter.ByafLoreItem
                        {
                            Key = e.Keys.Length > 0 ? e.Keys[0] : "",
                            Value = e.Content
                        })
                        .ToArray();
                }

                string charJson = Newtonsoft.Json.JsonConvert.SerializeObject(byafChar);
                var charEntry = archive.CreateEntry(charPath);
                using (var writer = new StreamWriter(charEntry.Open()))
                {
                    writer.Write(charJson);
                }

                // Add portrait image
                if (card.PortraitData != null && card.PortraitData.Length > 0)
                {
                    var imageEntry = archive.CreateEntry(imagePath);
                    using var imageStream = imageEntry.Open();
                    imageStream.Write(card.PortraitData, 0, card.PortraitData.Length);
                }

                return true;
            }
            catch
            {
                return false;
            }
        });
    }
}
