using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Ginger.Integration;

namespace Ginger.Models.Formats.ChatLogs;

public class GingerChatV2
{
    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string? title;

    [JsonProperty("createdAt")]
    public long createdAt;

    [JsonProperty("users")]
    public Dictionary<string, string> speakers = new();

    [JsonProperty("staging", NullValueHandling = NullValueHandling.Ignore)]
    public Staging? staging;

    [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
    public Parameters? parameters;

    [JsonProperty("background", NullValueHandling = NullValueHandling.Ignore)]
    public string? backgroundName;

    [JsonProperty("messages")]
    public Message[] messages = Array.Empty<Message>();

    public class Message
    {
        [JsonProperty("user")]
        public string speakerId = "";

        [JsonProperty("text")]
        public string text = "";

        [JsonProperty("timestamp")]
        public long timestamp;

        [JsonProperty("alt-texts", NullValueHandling = NullValueHandling.Ignore)]
        public string[]? regens;
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

    public static GingerChatV2 FromBackyardChat(Integration.Backyard.ChatInstance chatInstance, string[] participantNames)
    {
        if (chatInstance.history == null)
            return new GingerChatV2();

        var speakers = new Dictionary<string, string>();
        for (int i = 0; i < participantNames.Length; i++)
        {
            speakers[i.ToString()] = participantNames[i];
        }

        var messages = new List<Message>();
        if (chatInstance.history.messages != null)
        {
            foreach (var msg in chatInstance.history.messages)
            {
                var message = new Message
                {
                    speakerId = msg.speaker.ToString(),
                    text = msg.text ?? "",
                    timestamp = new DateTimeOffset(msg.creationDate).ToUnixTimeMilliseconds()
                };

                if (msg.swipes != null && msg.swipes.Length > 1)
                {
                    message.regens = msg.swipes;
                }

                messages.Add(message);
            }
        }

        return new GingerChatV2
        {
            title = chatInstance.name,
            createdAt = new DateTimeOffset(chatInstance.creationDate).ToUnixTimeMilliseconds(),
            speakers = speakers,
            messages = messages.ToArray()
        };
    }

    public static GingerChatV2 FromBackupChat(BackupData.Chat backup)
    {
        if (backup?.history == null)
            return new GingerChatV2();

        var speakers = new Dictionary<string, string>();
        if (backup.participants != null)
        {
            for (int i = 0; i < backup.participants.Length; i++)
            {
                speakers[i.ToString()] = backup.participants[i];
            }
        }

        var messages = new List<Message>();
        if (backup.history.messages != null)
        {
            foreach (var msg in backup.history.messages)
            {
                var message = new Message
                {
                    speakerId = msg.speaker.ToString(),
                    text = msg.text ?? "",
                    timestamp = new DateTimeOffset(msg.creationDate).ToUnixTimeMilliseconds()
                };

                if (msg.swipes != null && msg.swipes.Length > 1)
                {
                    message.regens = msg.swipes;
                }

                messages.Add(message);
            }
        }

        var chat = new GingerChatV2
        {
            title = backup.name,
            createdAt = new DateTimeOffset(backup.creationDate).ToUnixTimeMilliseconds(),
            speakers = speakers,
            messages = messages.ToArray(),
            backgroundName = backup.backgroundName
        };

        if (backup.staging != null)
        {
            chat.staging = new Staging
            {
                system = backup.staging.system,
                scenario = backup.staging.scenario,
                greeting = backup.staging.greeting?.text,
                example = backup.staging.example,
                grammar = backup.staging.grammar,
                authorNote = backup.staging.authorNote,
                pruneExampleChat = backup.staging.pruneExampleChat
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
                promptTemplate = backup.parameters.promptTemplate
            };
        }

        return chat;
    }

    public ChatHistory ToHistory()
    {
        var speakerIndex = speakers.Keys.Select((k, i) => (k, i)).ToDictionary(x => x.k, x => x.i);

        var result = new List<ChatHistory.Message>();
        foreach (var msg in messages)
        {
            if (!speakerIndex.TryGetValue(msg.speakerId, out int speakerIdx))
                speakerIdx = 0;

            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(msg.timestamp).DateTime;

            string[] swipes;
            int activeSwipe;

            if (msg.regens != null && msg.regens.Length > 0)
            {
                activeSwipe = Array.IndexOf(msg.regens, msg.text);
                if (activeSwipe == -1 && !string.IsNullOrEmpty(msg.text))
                {
                    swipes = new string[msg.regens.Length + 1];
                    Array.Copy(msg.regens, swipes, msg.regens.Length);
                    swipes[^1] = msg.text;
                    activeSwipe = swipes.Length - 1;
                }
                else
                {
                    swipes = msg.regens;
                    if (activeSwipe == -1) activeSwipe = 0;
                }
            }
            else
            {
                swipes = new[] { msg.text };
                activeSwipe = 0;
            }

            result.Add(new ChatHistory.Message
            {
                speaker = speakerIdx,
                creationDate = timestamp,
                updateDate = timestamp,
                activeSwipe = activeSwipe,
                swipes = swipes
            });
        }

        return new ChatHistory
        {
            name = title ?? "Chat",
            messages = result.ToArray()
        };
    }

    public static GingerChatV2? FromJson(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<GingerChatV2>(json);
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
                Formatting = Formatting.Indented
            });
        }
        catch
        {
            return null;
        }
    }
}
