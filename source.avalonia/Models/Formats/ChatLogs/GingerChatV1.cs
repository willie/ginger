using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ginger.Integration;

namespace Ginger.Models.Formats.ChatLogs;

using ChatInstance = Backyard.ChatInstance;

/// <summary>
/// Parser for Ginger Chat V1 format (legacy).
/// </summary>
public class GingerChatV1
{
    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string? title;

    [JsonProperty("createdAt")]
    public long createdAt;

    [JsonProperty("speakers")]
    [JsonConverter(typeof(JsonSpeakerListConverter))]
    public SpeakerList speakers = new();

    [JsonProperty("staging", NullValueHandling = NullValueHandling.Ignore)]
    public Staging? staging;

    [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
    public Parameters? parameters;

    [JsonProperty("background", NullValueHandling = NullValueHandling.Ignore)]
    public string? backgroundName;

    public class SpeakerList : List<Speaker>
    {
        public Speaker this[string id]
        {
            get
            {
                for (int i = 0; i < this.Count; ++i)
                {
                    if (this[i].id == id)
                        return this[i];
                }
                return default;
            }
        }
    }

    public class Message
    {
        [JsonProperty("speaker")]
        public string speakerId = "";

        [JsonProperty("text")]
        public string text = "";

        [JsonProperty("timestamp")]
        public long timestamp;

        [JsonProperty("regens", NullValueHandling = NullValueHandling.Ignore)]
        public string[]? regens;
    }

    [JsonProperty("messages")]
    public Message[] messages = Array.Empty<Message>();

    public struct Speaker
    {
        public string id;
        public string name;
    }

    public class Staging
    {
        [JsonProperty("system")]
        public string? system;

        [JsonProperty("greeting")]
        public string? greeting;

        [JsonProperty("scenario")]
        public string? scenario;

        [JsonProperty("example")]
        public string? example;

        [JsonProperty("grammar")]
        public string? grammar;

        [JsonProperty("authorNote", NullValueHandling = NullValueHandling.Ignore)]
        public string? authorNote;

        [JsonProperty("pruneExampleChat")]
        public bool pruneExampleChat = true;

        [JsonProperty("ttsAutoPlay")]
        public bool ttsAutoPlay;

        [JsonProperty("ttsInputFilter", NullValueHandling = NullValueHandling.Ignore)]
        public string? ttsInputFilter;
    }

    public class Parameters
    {
        [JsonProperty("model", NullValueHandling = NullValueHandling.Ignore)]
        public string? model;

        [JsonProperty("temperature")]
        public decimal temperature = 1.2m;

        [JsonProperty("topP")]
        public decimal topP = 0.9m;

        [JsonProperty("minP")]
        public decimal minP = 0.1m;

        [JsonProperty("topK")]
        public int topK = 30;

        [JsonProperty("minPEnabled")]
        public bool minPEnabled = true;

        [JsonProperty("repeatLastN")]
        public int repeatLastN = 256;

        [JsonProperty("repeatPenalty")]
        public decimal repeatPenalty = 1.05m;

        [JsonProperty("promptTemplate", NullValueHandling = NullValueHandling.Ignore)]
        public string? promptTemplate;
    }

    public static GingerChatV1? FromChat(ChatInstance chatInstance, SpeakerList speakers)
    {
        if (speakers == null || chatInstance == null)
            return null;

        var lsMessages = new List<Message>(chatInstance.history.count);

        foreach (var message in chatInstance.history.messages)
        {
            if (message.swipes.Length > 1)
            {
                lsMessages.Add(new Message
                {
                    speakerId = speakers[message.speaker].id,
                    text = message.text,
                    regens = message.swipes,
                    timestamp = message.creationDate.ToUnixTimeMilliseconds(),
                });
            }
            else
            {
                lsMessages.Add(new Message
                {
                    speakerId = speakers[message.speaker].id,
                    text = message.text,
                    timestamp = message.creationDate.ToUnixTimeMilliseconds(),
                });
            }
        }

        return new GingerChatV1
        {
            title = chatInstance.name,
            speakers = speakers,
            createdAt = chatInstance.creationDate.ToUnixTimeMilliseconds(),
            messages = lsMessages.ToArray(),
        };
    }

    public static GingerChatV1? FromBackup(BackupData.Chat backup)
    {
        if (backup == null)
            return null;

        var speakers = new SpeakerList();
        if (backup.participants != null)
        {
            for (int i = 0; i < backup.participants.Length; ++i)
            {
                speakers.Add(new Speaker
                {
                    id = i.ToString(),
                    name = backup.participants[i],
                });
            }
        }

        var lsMessages = new List<Message>(backup.history?.count ?? 0);

        if (backup.history?.messages != null)
        {
            foreach (var message in backup.history.messages)
            {
                if (message.swipes.Length > 1)
                {
                    lsMessages.Add(new Message
                    {
                        speakerId = speakers[message.speaker].id,
                        text = message.text,
                        regens = message.swipes,
                        timestamp = message.creationDate.ToUnixTimeMilliseconds(),
                    });
                }
                else
                {
                    lsMessages.Add(new Message
                    {
                        speakerId = speakers[message.speaker].id,
                        text = message.text,
                        timestamp = message.creationDate.ToUnixTimeMilliseconds(),
                    });
                }
            }
        }

        var chat = new GingerChatV1
        {
            title = backup.name,
            speakers = speakers,
            createdAt = backup.creationDate.ToUnixTimeMilliseconds(),
            messages = lsMessages.ToArray(),
            backgroundName = backup.backgroundName,
        };

        if (backup.staging != null)
        {
            chat.staging = new Staging
            {
                system = backup.staging.system ?? "",
                scenario = backup.staging.scenario ?? "",
                greeting = backup.staging.greeting?.text ?? "",
                example = backup.staging.example ?? "",
                grammar = backup.staging.grammar ?? "",
                authorNote = backup.staging.authorNote ?? "",
                pruneExampleChat = backup.staging.pruneExampleChat,
            };
        }

        if (backup.parameters != null)
        {
            chat.parameters = new Parameters
            {
                model = backup.parameters.model,
                temperature = backup.parameters.temperature,
                topP = backup.parameters.topP,
                minP = backup.parameters.minP,
                topK = backup.parameters.topK,
                minPEnabled = backup.parameters.minPEnabled,
                repeatLastN = backup.parameters.repeatLastN,
                repeatPenalty = backup.parameters.repeatPenalty,
                promptTemplate = backup.parameters.promptTemplate,
            };
        }

        return chat;
    }

    public ChatHistory? ToChat()
    {
        int index = 0;
        var indexById = speakers.ToDictionary(s => s.id, s => index++);

        var result = new List<ChatHistory.Message>();
        foreach (var message in this.messages)
        {
            DateTime timestamp = DateTimeExtensions.FromUnixTime(message.timestamp);

            if (!indexById.TryGetValue(message.speakerId, out int speakerIdx))
                return null; // Error

            string[] swipes;
            int activeSwipe;
            if (message.regens != null && message.regens.Length > 0)
            {
                activeSwipe = Array.IndexOf(message.regens, message.text);
                if (activeSwipe == -1 && message.text != null)
                {
                    swipes = new string[message.regens.Length + 1];
                    swipes[swipes.Length - 1] = message.text;
                    Array.Copy(message.regens, swipes, message.regens.Length);
                    activeSwipe = swipes.Length - 1;
                }
                else
                {
                    swipes = message.regens;
                    if (activeSwipe == -1) activeSwipe = 0;
                }
            }
            else
            {
                swipes = new[] { message.text };
                activeSwipe = 0;
            }

            for (int i = 0; i < swipes.Length; ++i)
                Anonymize(ref swipes[i]);

            result.Add(new ChatHistory.Message
            {
                speaker = speakerIdx,
                creationDate = timestamp,
                updateDate = timestamp,
                activeSwipe = activeSwipe,
                swipes = swipes,
            });
        }

        return new ChatHistory
        {
            name = Utility.FirstNonEmpty(this.title, ChatInstance.DefaultName),
            messages = result.ToArray(),
        };
    }

    public BackupData.Chat ToBackupChat()
    {
        var chat = new BackupData.Chat
        {
            name = title ?? ChatInstance.DefaultName,
            creationDate = DateTimeExtensions.FromUnixTime(createdAt),
            updateDate = DateTimeExtensions.FromUnixTime(createdAt),
            backgroundName = backgroundName,
            history = ToChat(),
        };

        if (this.staging != null)
        {
            chat.staging = new ChatStaging
            {
                system = this.staging.system ?? "",
                scenario = this.staging.scenario ?? "",
                greeting = CharacterMessage.FromString(this.staging.greeting ?? ""),
                example = this.staging.example ?? "",
                grammar = this.staging.grammar ?? "",
                pruneExampleChat = this.staging.pruneExampleChat,
                authorNote = this.staging.authorNote ?? "",
            };
        }

        if (this.parameters != null)
        {
            chat.parameters = new ChatParameters
            {
                model = this.parameters.model,
                temperature = this.parameters.temperature,
                topP = this.parameters.topP,
                minP = this.parameters.minP,
                minPEnabled = this.parameters.minPEnabled,
                topK = this.parameters.topK,
                repeatPenalty = this.parameters.repeatPenalty,
                repeatLastN = this.parameters.repeatLastN,
                promptTemplate = this.parameters.promptTemplate,
            };
        }

        return chat;
    }

    private void Anonymize(ref string text)
    {
        if (speakers == null || speakers.Count == 0)
            return;

        StringBuilder sb = new StringBuilder(text);
        if (speakers.Count > 0)
            Utility.ReplaceWholeWord(sb, speakers[0].name, GingerString.UserMarker, StringComparison.Ordinal);
        if (speakers.Count > 1)
            Utility.ReplaceWholeWord(sb, speakers[1].name, GingerString.CharacterMarker, StringComparison.Ordinal);
        text = sb.ToString();
    }

    public static GingerChatV1? FromJson(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<GingerChatV1>(json);
        }
        catch
        {
            return null;
        }
    }

    public string? ToJson()
    {
        try
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
            });
        }
        catch
        {
            return null;
        }
    }

    public static bool Validate(string jsonData)
    {
        try
        {
            var parsed = JsonConvert.DeserializeObject<GingerChatV1>(jsonData);
            return parsed?.messages != null && parsed?.speakers != null;
        }
        catch
        {
            return false;
        }
    }

    private class JsonSpeakerListConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SpeakerList);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            SpeakerList list = new SpeakerList();
            JObject jObj = JObject.Load(reader);

            foreach (JProperty jProp in (JToken)jObj)
            {
                list.Add(new Speaker
                {
                    id = jProp.Name.ToString(),
                    name = jProp.Value?.ToString() ?? "",
                });
            }

            return list;
        }

        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is SpeakerList speakers)
            {
                writer.WriteStartObject();
                foreach (var speaker in speakers)
                {
                    writer.WritePropertyName(speaker.id);
                    writer.WriteValue(speaker.name);
                }
                writer.WriteEndObject();
            }
        }
    }
}
