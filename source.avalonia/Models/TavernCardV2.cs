using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Ginger.Models;

public class TavernCardV2
{
    public TavernCardV2()
    {
        data = new Data();
    }

    [JsonProperty("data", Required = Required.Always)]
    public Data data;

    [JsonProperty("spec", Required = Required.Always)]
    public string spec = "chara_card_v2";

    [JsonProperty("spec_version", Required = Required.Always)]
    public string spec_version = "2.0";

    public class Data
    {
        [JsonProperty("name", Required = Required.Always)]
        public string name = "";

        [JsonProperty("description")]
        public string persona = "";

        [JsonProperty("personality")]
        public string personality = "";

        [JsonProperty("scenario")]
        public string scenario = "";

        [JsonProperty("first_mes")]
        public string greeting = "";

        [JsonProperty("mes_example")]
        public string example = "";

        [JsonProperty("system_prompt")]
        public string system = "";

        [JsonProperty("creator_notes")]
        public string creator_notes = "";

        [JsonProperty("post_history_instructions")]
        public string post_history_instructions = "";

        [JsonProperty("alternate_greetings")]
        public string[] alternate_greetings = Array.Empty<string>();

        [JsonProperty("character_book")]
        public CharacterBook? character_book;

        [JsonProperty("tags")]
        public string[] tags = Array.Empty<string>();

        [JsonProperty("creator")]
        public string creator = "";

        [JsonProperty("character_version")]
        public string character_version = "";

        [JsonProperty("extensions")]
        public Dictionary<string, object>? extensions;
    }

    public class CharacterBook
    {
        [JsonProperty("name")]
        public string? name;

        [JsonProperty("description")]
        public string? description;

        [JsonProperty("scan_depth")]
        public int scan_depth = 50;

        [JsonProperty("token_budget")]
        public int token_budget = 500;

        [JsonProperty("recursive_scanning")]
        public bool recursive_scanning = false;

        [JsonProperty("entries", Required = Required.Always)]
        public Entry[] entries = Array.Empty<Entry>();

        [JsonProperty("extensions")]
        public Dictionary<string, object>? extensions;

        public class Entry
        {
            [JsonProperty("id")]
            public int id;

            [JsonProperty("keys")]
            public string[]? keys;

            [JsonProperty("secondary_keys")]
            public string[] secondary_keys = Array.Empty<string>();

            [JsonProperty("comment")]
            public string comment = "";

            [JsonProperty("content", Required = Required.Always)]
            public string content = "";

            [JsonProperty("constant")]
            public bool constant = false;

            [JsonProperty("selective")]
            public bool selective = false;

            [JsonProperty("insertion_order", Required = Required.Always)]
            public int insertion_order = 100;

            [JsonProperty("enabled", Required = Required.Always)]
            public bool enabled = true;

            [JsonProperty("position")]
            public string position = "before_char";

            [JsonProperty("case_sensitive")]
            public bool case_sensitive = false;

            [JsonProperty("name")]
            public string name = "";

            [JsonProperty("priority")]
            public int priority = 10;

            [JsonProperty("extensions")]
            public Dictionary<string, object>? extensions;
        }
    }

    public static TavernCardV2? FromJson(string json, out int errors)
    {
        errors = 0;
        var lsErrors = new List<string>();
        var settings = new JsonSerializerSettings
        {
            Error = delegate(object? sender, ErrorEventArgs args)
            {
                if (args.ErrorContext.Error.Message.Contains(".extensions"))
                {
                    args.ErrorContext.Handled = true;
                    return;
                }
                if (args.ErrorContext.Error.Message.Contains("Required"))
                    return;

                lsErrors.Add(args.ErrorContext.Error.Message);
                args.ErrorContext.Handled = true;
            },
        };

        try
        {
            var jObject = JObject.Parse(json);

            // Check if it's a V2 card
            var specToken = jObject["spec"];
            if (specToken != null && specToken.ToString() == "chara_card_v2")
            {
                var card = JsonConvert.DeserializeObject<TavernCardV2>(json, settings);
                if (card != null && Validate(card))
                {
                    errors = lsErrors.Count;
                    return card;
                }
            }

            // Try V1 format (simpler structure without spec)
            if (jObject["name"] != null && (jObject["description"] != null || jObject["personality"] != null))
            {
                var cardV1 = JsonConvert.DeserializeObject<TavernCardV1>(json, settings);
                if (cardV1 != null)
                {
                    errors = lsErrors.Count;
                    return FromV1(cardV1);
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static TavernCardV2 FromV1(TavernCardV1 card)
    {
        return new TavernCardV2
        {
            data = new Data
            {
                name = card.name ?? "",
                persona = card.description ?? "",
                personality = card.personality ?? "",
                scenario = card.scenario ?? "",
                greeting = card.first_mes ?? "",
                example = card.mes_example ?? "",
            }
        };
    }

    public string ToJson()
    {
        try
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
            });
        }
        catch
        {
            return "";
        }
    }

    private static bool Validate(TavernCardV2 card)
    {
        if (card.data == null)
            return false;
        if (string.IsNullOrEmpty(card.data.name))
            card.data.name = "Unnamed";
        return true;
    }

    public static bool Validate(string jsonData)
    {
        try
        {
            var jObject = JObject.Parse(jsonData);
            var specToken = jObject["spec"];
            if (specToken != null && specToken.ToString() == "chara_card_v2")
                return true;
            // Also accept V1 format
            return jObject["name"] != null;
        }
        catch
        {
            return false;
        }
    }
}

public class TavernCardV1
{
    [JsonProperty("name")]
    public string? name;

    [JsonProperty("description")]
    public string? description;

    [JsonProperty("personality")]
    public string? personality;

    [JsonProperty("scenario")]
    public string? scenario;

    [JsonProperty("first_mes")]
    public string? first_mes;

    [JsonProperty("mes_example")]
    public string? mes_example;
}
