using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ginger.Models;

/// <summary>
/// Faraday/Backyard AI character card format (V1-V4).
/// Used in .byaf archives and Backyard AI exports.
/// </summary>
public class FaradayCard
{
    [JsonProperty("character", Required = Required.Always)]
    public FaradayData Data { get; set; } = new();

    [JsonProperty("version")]
    public int Version { get; set; } = 4;

    public class FaradayData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("aiDisplayName")]
        public string DisplayName { get; set; } = "";

        [JsonProperty("aiName")]
        public string Name { get; set; } = "";

        [JsonProperty("aiPersona")]
        public string Persona { get; set; } = "";

        [JsonProperty("scenario")]
        public string Scenario { get; set; } = "";

        [JsonProperty("basePrompt")]
        public string System { get; set; } = "";

        [JsonProperty("customDialogue")]
        public string Example { get; set; } = "";

        [JsonProperty("firstMessage")]
        public string Greeting { get; set; } = "";

        [JsonProperty("createdAt")]
        public string CreationDate { get; set; } = "";

        [JsonProperty("updatedAt")]
        public string UpdateDate { get; set; } = "";

        [JsonProperty("grammar")]
        public string? Grammar { get; set; }

        [JsonProperty("isNSFW")]
        public bool IsNSFW { get; set; }

        [JsonProperty("loreItems")]
        public LoreEntry[]? LoreItems { get; set; }

        // Model parameters
        [JsonProperty("temperature")]
        public decimal Temperature { get; set; } = 1.2m;

        [JsonProperty("topK")]
        public int TopK { get; set; } = 30;

        [JsonProperty("topP")]
        public decimal TopP { get; set; } = 0.9m;

        [JsonProperty("minP")]
        public decimal MinP { get; set; } = 0.1m;

        [JsonProperty("repeatPenalty")]
        public decimal RepeatPenalty { get; set; } = 1.05m;
    }

    public class LoreEntry
    {
        [JsonProperty("key")]
        public string Key { get; set; } = "";

        [JsonProperty("value")]
        public string Value { get; set; } = "";
    }

    public static FaradayCard? FromJson(string json)
    {
        try
        {
            var jObject = JObject.Parse(json);

            // Check for Faraday format markers
            if (jObject["character"] != null || jObject["version"] != null)
            {
                return JsonConvert.DeserializeObject<FaradayCard>(json);
            }
        }
        catch
        {
        }
        return null;
    }

    public static bool Validate(string json)
    {
        try
        {
            var jObject = JObject.Parse(json);
            return jObject["character"] != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Convert to the common CharacterCard format.
    /// </summary>
    public CharacterCard ToCharacterCard(byte[]? portraitData = null)
    {
        var card = new CharacterCard
        {
            SourceFormat = CharacterCard.CardFormat.Faraday,
            Name = Data.DisplayName,
            SpokenName = Data.Name,
            Persona = Data.Persona ?? "",
            Scenario = Data.Scenario ?? "",
            System = Data.System ?? "",
            Example = Data.Example ?? "",
            Greeting = Data.Greeting ?? "",
            PortraitData = portraitData,
        };

        // Convert lorebook
        if (Data.LoreItems != null && Data.LoreItems.Length > 0)
        {
            card.Lorebook = new Lorebook();
            int id = 1;
            foreach (var entry in Data.LoreItems)
            {
                card.Lorebook.Entries.Add(new Lorebook.LorebookEntry
                {
                    Id = id++,
                    Keys = new[] { entry.Key },
                    Content = entry.Value,
                    Enabled = true,
                });
            }
        }

        return card;
    }
}

/// <summary>
/// Character format used in .byaf archive files.
/// Different from FaradayCard - uses flat structure with different property names.
/// </summary>
public class ByafCharacter
{
    [JsonProperty("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonProperty("id")]
    public string Id { get; set; } = "";

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonProperty("persona")]
    public string Persona { get; set; } = "";

    [JsonProperty("images")]
    public ByafImage[]? Images { get; set; }

    [JsonProperty("loreItems")]
    public ByafLoreItem[]? LoreItems { get; set; }

    [JsonProperty("isNSFW")]
    public bool IsNSFW { get; set; }

    [JsonProperty("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonProperty("updatedAt")]
    public string? UpdatedAt { get; set; }

    public class ByafImage
    {
        [JsonProperty("path")]
        public string Path { get; set; } = "";

        [JsonProperty("label")]
        public string? Label { get; set; }
    }

    public class ByafLoreItem
    {
        [JsonProperty("key")]
        public string Key { get; set; } = "";

        [JsonProperty("value")]
        public string Value { get; set; } = "";
    }

    public static ByafCharacter? FromJson(string json)
    {
        try
        {
            var jObject = JObject.Parse(json);
            // BYAF archive character format has schemaVersion and no "character" wrapper
            if (jObject["schemaVersion"] != null && jObject["character"] == null)
            {
                return JsonConvert.DeserializeObject<ByafCharacter>(json);
            }
        }
        catch
        {
        }
        return null;
    }

    /// <summary>
    /// Convert to the common CharacterCard format.
    /// </summary>
    public CharacterCard ToCharacterCard(byte[]? portraitData = null)
    {
        var card = new CharacterCard
        {
            SourceFormat = CharacterCard.CardFormat.Faraday,
            Name = DisplayName,
            SpokenName = Name,
            Persona = Persona ?? "",
            PortraitData = portraitData,
        };

        // Convert lorebook
        if (LoreItems != null && LoreItems.Length > 0)
        {
            card.Lorebook = new Lorebook();
            int id = 1;
            foreach (var entry in LoreItems)
            {
                card.Lorebook.Entries.Add(new Lorebook.LorebookEntry
                {
                    Id = id++,
                    Keys = new[] { entry.Key },
                    Content = entry.Value,
                    Enabled = true,
                });
            }
        }

        return card;
    }

    /// <summary>
    /// Get the first image path from the images array.
    /// </summary>
    public string? GetFirstImagePath()
    {
        return Images != null && Images.Length > 0 ? Images[0].Path : null;
    }
}

/// <summary>
/// Manifest for .byaf archive files.
/// </summary>
public class ByafManifest
{
    [JsonProperty("characters")]
    public string[] Characters { get; set; } = Array.Empty<string>();

    [JsonProperty("scenarios")]
    public string[] Scenarios { get; set; } = Array.Empty<string>();

    [JsonProperty("author")]
    public AuthorInfo? Author { get; set; }

    public class AuthorInfo
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("backyardURL")]
        public string? BackyardUrl { get; set; }
    }

    public static ByafManifest? FromJson(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<ByafManifest>(json);
        }
        catch
        {
            return null;
        }
    }
}
