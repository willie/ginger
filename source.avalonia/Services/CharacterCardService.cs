// =============================================================================
// PORT ORIGIN: Consolidated from multiple WinForms files:
//              - source/src/Model/GingerCharacter.cs (format dispatch logic)
//              - source/src/Model/Formats/FileUtil.cs (file I/O operations)
//              - Various format parsers in source/src/Model/Formats/
// STATUS: consolidated
// CHANGES: Static methods consolidated into service class
//          Sync I/O -> async/await patterns
//          Format detection and dispatch centralized
// =============================================================================
using System;
using System.Collections.Generic;
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
    /// Supports Ginger XML, Tavern V2/V3, and Faraday (EXIF) formats.
    /// </summary>
    private async Task<(LoadResult result, CharacterCard? card)> LoadFromPngAsync(string filePath)
    {
        // Read PNG image data
        byte[] imageData = await File.ReadAllBytesAsync(filePath);

        // Extract all embedded data (EXIF, PNG chunks)
        var embedded = await Task.Run(() => PngMetadata.ExtractAllData(filePath));

        if (embedded.IsEmpty)
            return (LoadResult.NoDataFound, null);

        // Priority order: Ginger > Faraday > TavernV3 > TavernV2

        // 1. Try Ginger native format (XML) - preserves recipes
        if (!string.IsNullOrEmpty(embedded.GingerXml))
        {
            try
            {
                var gingerCard = GingerCardV1.FromXml(embedded.GingerXml);
                if (gingerCard != null)
                {
                    var card = CharacterCard.FromGingerV1(gingerCard, imageData);

                    // Also load V3 assets if available
                    if (!string.IsNullOrEmpty(embedded.TavernJsonV3))
                    {
                        var tavernV3 = TavernCardV3.FromJson(embedded.TavernJsonV3, out _);
                        if (tavernV3?.data?.assets != null && embedded.EmbeddedAssets != null)
                        {
                            card.EmbeddedAssets = embedded.EmbeddedAssets;
                        }
                    }

                    return (LoadResult.Success, card);
                }
            }
            catch { }
        }

        // 2. Try Faraday format (from EXIF) - Backyard AI PNGs
        if (!string.IsNullOrEmpty(embedded.FaradayJson))
        {
            try
            {
                // Try FaradayCardV4 first
                var faradayV4 = FaradayCardV4.FromJson(embedded.FaradayJson);
                if (faradayV4 != null)
                {
                    var card = CharacterCard.FromFaradayV4(faradayV4, imageData);
                    return (LoadResult.Success, card);
                }

                // Try older Faraday formats
                var faraday = FaradayCard.FromJson(embedded.FaradayJson);
                if (faraday != null)
                {
                    var card = faraday.ToCharacterCard(imageData);
                    return (LoadResult.Success, card);
                }
            }
            catch { }
        }

        // 3. Try TavernCardV3 format
        if (!string.IsNullOrEmpty(embedded.TavernJsonV3))
        {
            try
            {
                var tavernV3 = TavernCardV3.FromJson(embedded.TavernJsonV3, out _);
                if (tavernV3 != null)
                {
                    var card = CharacterCard.FromTavernV3(tavernV3, imageData);
                    card.EmbeddedAssets = embedded.EmbeddedAssets;
                    return (LoadResult.Success, card);
                }
                // Fall back to V2 parser if V3 parsing fails
                var tavernV2 = TavernCardV2.FromJson(embedded.TavernJsonV3, out _);
                if (tavernV2 != null)
                {
                    var card = CharacterCard.FromTavernV2(tavernV2, imageData);
                    card.SourceFormat = CharacterCard.CardFormat.TavernV3;
                    return (LoadResult.Success, card);
                }
            }
            catch { }
        }

        // 4. Try TavernCardV2 format (most common)
        if (!string.IsNullOrEmpty(embedded.TavernJsonV2))
        {
            try
            {
                var tavernV2 = TavernCardV2.FromJson(embedded.TavernJsonV2, out _);
                if (tavernV2 != null)
                {
                    var card = CharacterCard.FromTavernV2(tavernV2, imageData);
                    return (LoadResult.Success, card);
                }

                // Try TavernV1 (simpler format)
                var tavernV1 = TavernCardV1.FromJson(embedded.TavernJsonV2);
                if (tavernV1 != null)
                {
                    var card = tavernV1.ToCharacterCard(imageData);
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
                ".xml" => await SaveToGingerXmlAsync(filePath, card),
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
    /// Writes all formats: TavernV2 (chara), TavernV3 (ccv3), Ginger XML, and Faraday EXIF.
    /// </summary>
    private async Task<bool> SaveToPngAsync(string filePath, CharacterCard card)
    {
        // Need existing PNG data (portrait)
        if (card.PortraitData == null || card.PortraitData.Length == 0)
            return false;

        var metadata = new Dictionary<string, string>();

        // 1. TavernV2 format (chara) - most widely supported
        var tavernV2 = card.ToTavernV2();
        string tavernV2Json = tavernV2.ToJson();
        string tavernV2Base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(tavernV2Json));
        metadata["chara"] = tavernV2Base64;

        // 2. TavernV3 format (ccv3) - includes assets
        var tavernV3 = card.ToTavernV3();
        string tavernV3Json = tavernV3.ToJson();
        string tavernV3Base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(tavernV3Json));
        metadata["ccv3"] = tavernV3Base64;

        // 3. Ginger XML format (compressed) - preserves recipes
        if (card.GingerData != null)
        {
            string gingerXml = card.GingerData.ToXml();
            if (!string.IsNullOrEmpty(gingerXml))
            {
                string gingerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(gingerXml));
                metadata["ginger"] = gingerBase64;
            }
        }

        // 4. Embedded assets (for V3 compatibility)
        if (card.EmbeddedAssets != null)
        {
            const string assetPrefix = "chara-ext-asset_";
            foreach (var asset in card.EmbeddedAssets)
            {
                string assetBase64 = Convert.ToBase64String(asset.Value);
                metadata[assetPrefix + asset.Key] = assetBase64;
            }
        }

        // Write PNG chunks (ginger chunk is compressed)
        byte[] outputData = WritePngWithAllChunks(card.PortraitData, metadata);
        await File.WriteAllBytesAsync(filePath, outputData);

        // 5. Write Faraday EXIF data
        var faradayCard = card.ToFaradayV4();
        string faradayJson = faradayCard.ToJson();
        if (!string.IsNullOrEmpty(faradayJson))
        {
            string faradayBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(faradayJson));
            PngMetadata.WriteExifUserComment(filePath, faradayBase64);
        }

        return true;
    }

    /// <summary>
    /// Write PNG with mixed compression (ginger compressed, others uncompressed).
    /// </summary>
    private static byte[] WritePngWithAllChunks(byte[] pngData, Dictionary<string, string> chunks)
    {
        // Separate chunks by compression needs
        var uncompressedChunks = new Dictionary<string, string>();
        var compressedChunks = new Dictionary<string, string>();

        foreach (var kvp in chunks)
        {
            if (kvp.Key == "ginger")
                compressedChunks[kvp.Key] = kvp.Value;
            else
                uncompressedChunks[kvp.Key] = kvp.Value;
        }

        // Write uncompressed chunks first
        byte[] result = PngMetadata.WriteTextChunks(pngData, uncompressedChunks, compress: false);

        // Then write compressed chunks
        if (compressedChunks.Count > 0)
            result = PngMetadata.WriteTextChunks(result, compressedChunks, compress: true);

        return result;
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
            context = BuildTextGenContext(card),
            greeting = card.Greeting,
            example = card.Example,
        };
        string yaml = textGenCard.ToYaml();
        if (yaml == null)
            return false;

        await File.WriteAllTextAsync(filePath, yaml);
        return true;
    }

    /// <summary>
    /// Build the context field for TextGenWebUI format.
    /// This combines persona, personality, scenario, and system prompt.
    /// </summary>
    private static string BuildTextGenContext(CharacterCard card)
    {
        var sb = new StringBuilder();

        // System prompt first (if present)
        if (!string.IsNullOrWhiteSpace(card.System))
        {
            sb.AppendLine(card.System);
            sb.AppendLine();
        }

        // Persona / Description
        if (!string.IsNullOrWhiteSpace(card.Persona))
        {
            sb.AppendLine(card.Persona);
        }

        // Personality (merged with persona)
        if (!string.IsNullOrWhiteSpace(card.Personality))
        {
            if (sb.Length > 0)
                sb.AppendLine();
            sb.AppendLine(card.Personality);
        }

        // Scenario
        if (!string.IsNullOrWhiteSpace(card.Scenario))
        {
            if (sb.Length > 0)
                sb.AppendLine();
            sb.AppendLine(card.Scenario);
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Save a character card to a Ginger XML file (native format with full recipe data).
    /// </summary>
    private async Task<bool> SaveToGingerXmlAsync(string filePath, CharacterCard card)
    {
        // Create GingerCardV1 from Current model (which has the full recipe structure)
        var gingerCard = GingerCardV1.Create();

        // Update basic metadata from the card
        gingerCard.name = card.Name;
        gingerCard.creator = card.Creator;
        gingerCard.comment = card.CreatorNotes;
        gingerCard.versionString = card.Version;
        gingerCard.userGender = card.UserGender;
        gingerCard.tags = card.Tags.ToArray();

        // Generate XML
        string xml = gingerCard.ToXml();
        if (xml == null)
            return false;

        await File.WriteAllTextAsync(filePath, xml, System.Text.Encoding.UTF8);
        return true;
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

    /// <summary>
    /// Parse a character card from JSON string.
    /// </summary>
    public CharacterCard? ParseFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            // Try TavernCardV3 format first
            var tavernV3 = TavernCardV3.FromJson(json, out _);
            if (tavernV3 != null)
                return CharacterCard.FromTavernV3(tavernV3);

            // Try TavernCardV2 format
            var tavernV2 = TavernCardV2.FromJson(json, out _);
            if (tavernV2 != null)
                return CharacterCard.FromTavernV2(tavernV2);

            // Try Agnaistic format
            if (AgnaisticCard.Validate(json))
            {
                var agn = AgnaisticCard.FromJson(json, out _);
                if (agn != null)
                    return CharacterCard.FromAgnaistic(agn);
            }

            // Try Pygmalion format
            var pyg = PygmalionCard.FromJson(json);
            if (pyg != null)
                return pyg.ToCharacterCard();

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parse a character card from YAML string.
    /// </summary>
    public CharacterCard? ParseFromYaml(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            return null;

        try
        {
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            // Try to deserialize as TavernCardV2
            var tavern = deserializer.Deserialize<TavernCardV2>(yaml);
            if (tavern != null && !string.IsNullOrEmpty(tavern.data?.name))
                return CharacterCard.FromTavernV2(tavern);

            return null;
        }
        catch
        {
            return null;
        }
    }
}
