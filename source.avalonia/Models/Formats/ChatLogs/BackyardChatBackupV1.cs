#nullable enable
#pragma warning disable CS8600, CS8601, CS8604, CS8619 // Nullable reference type warnings

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Ginger.Integration;

namespace Ginger.Models.Formats.ChatLogs;

using ChatInstance = Backyard.ChatInstance;

/// <summary>
/// Parser for Backyard Chat Backup V1 format (legacy).
/// </summary>
public class BackyardChatBackupV1
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? name;

    [JsonProperty("createdAt", NullValueHandling = NullValueHandling.Ignore)]
    public long? createdAt;

    [JsonProperty("updatedAt", NullValueHandling = NullValueHandling.Ignore)]
    public long? updatedAt;

    [JsonProperty("staging", NullValueHandling = NullValueHandling.Ignore)]
    public Staging? staging;

    [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
    public Parameters? parameters;

    [JsonProperty("background", NullValueHandling = NullValueHandling.Ignore)]
    public string? backgroundName;

    // Same as Backyard chat
    [JsonProperty("chat")]
    public Chat chat = new();

    // Same as Backyard chat
    [JsonProperty("version")]
    public int version = 2; // For Backyard compatibility

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

    public class Chat
    {
        [JsonProperty("ChatItems")]
        public ChatItem[] items = Array.Empty<ChatItem>();
    }

    public class ChatItem
    {
        [JsonProperty("input")]
        public string input = "";

        [JsonProperty("output")]
        public string output = "";

        [JsonProperty("createdAt")]
        public long timestamp;
    }

    public static BackyardChatBackupV1 FromChat(BackupData.Chat chat)
    {
        BackyardChatBackupV1 backup = new BackyardChatBackupV1();
        var lsEntries = new List<ChatItem>((chat.history?.count ?? 0) / 2 + 1);

        int iMsg = 0;
        if (chat.history?.hasGreeting == true) // Do not include greeting
            iMsg = 1;

        string? lastMessage = null;
        if (chat.history?.messages != null)
        {
            for (; iMsg < chat.history.messages.Length; ++iMsg)
            {
                var message = chat.history.messages[iMsg];
                if (message.speaker == 0) // Is user
                {
                    // Check if previous message was from user
                    if (iMsg > 0 && chat.history.messages[iMsg - 1].speaker == 0)
                    {
                        lsEntries.Add(new ChatItem
                        {
                            input = lastMessage ?? "",
                            output = "",
                            timestamp = chat.history.messages[iMsg - 1].creationDate.ToUnixTimeMilliseconds(),
                        });
                    }
                    lastMessage = message.text;
                }
                else // Is character
                {
                    lsEntries.Add(new ChatItem
                    {
                        input = lastMessage ?? "",
                        output = message.text ?? "",
                        timestamp = message.creationDate.ToUnixTimeMilliseconds(),
                    });
                    lastMessage = null;
                }
            }
        }

        backup.name = chat.name ?? ChatInstance.DefaultName;
        backup.createdAt = chat.creationDate.ToUnixTimeMilliseconds();
        backup.updatedAt = chat.updateDate.ToUnixTimeMilliseconds();
        backup.backgroundName = chat.backgroundName;

        if (chat.staging != null)
        {
            backup.staging = new Staging
            {
                system = chat.staging.system ?? "",
                scenario = chat.staging.scenario ?? "",
                greeting = chat.staging.greeting?.text ?? "",
                example = chat.staging.example ?? "",
                grammar = chat.staging.grammar ?? "",
                authorNote = chat.staging.authorNote ?? "",
                pruneExampleChat = chat.staging.pruneExampleChat,
            };
        }

        if (chat.parameters != null)
        {
            backup.parameters = new Parameters
            {
                model = chat.parameters.model,
                temperature = chat.parameters.temperature,
                topP = chat.parameters.topP,
                minP = chat.parameters.minP,
                topK = chat.parameters.topK,
                minPEnabled = chat.parameters.minPEnabled,
                repeatLastN = chat.parameters.repeatLastN,
                repeatPenalty = chat.parameters.repeatPenalty,
                promptTemplate = chat.parameters.promptTemplate,
            };
        }

        backup.chat.items = lsEntries.ToArray();
        return backup;
    }

    public BackupData.Chat ToChat()
    {
        var messages = new List<ChatHistory.Message>();
        foreach (var item in chat.items)
        {
            DateTime inputTime;
            DateTime outputTime;

            if (item.timestamp != 0)
            {
                inputTime = DateTimeExtensions.FromUnixTime(item.timestamp);
                outputTime = DateTimeExtensions.FromUnixTime(item.timestamp) + TimeSpan.FromMilliseconds(10);
            }
            else
            {
                inputTime = DateTime.Now;
                outputTime = DateTime.Now;
            }

            if (!string.IsNullOrEmpty(item.input))
            {
                messages.Add(new ChatHistory.Message
                {
                    speaker = 0,
                    creationDate = inputTime,
                    updateDate = inputTime,
                    activeSwipe = 0,
                    swipes = new[] { item.input },
                });
            }

            if (!string.IsNullOrEmpty(item.output))
            {
                messages.Add(new ChatHistory.Message
                {
                    speaker = 1,
                    creationDate = outputTime,
                    updateDate = outputTime,
                    activeSwipe = 0,
                    swipes = new[] { item.output },
                });
            }
        }

        ChatStaging? staging = null;
        ChatParameters? parameters = null;

        if (this.staging != null)
        {
            staging = new ChatStaging
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
            parameters = new ChatParameters
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

        return new BackupData.Chat
        {
            name = name ?? ChatInstance.DefaultName,
            creationDate = createdAt.HasValue ? DateTimeExtensions.FromUnixTime(createdAt.Value) : DateTime.Now,
            updateDate = updatedAt.HasValue ? DateTimeExtensions.FromUnixTime(updatedAt.Value) : DateTime.Now,
            staging = staging,
            parameters = parameters,
            backgroundName = backgroundName,
            history = new ChatHistory
            {
                messages = messages.ToArray(),
            },
        };
    }

    public static BackyardChatBackupV1? FromJson(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<BackyardChatBackupV1>(json);
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
            var parsed = JsonConvert.DeserializeObject<BackyardChatBackupV1>(jsonData);
            return parsed?.chat?.items != null;
        }
        catch
        {
            return false;
        }
    }
}
