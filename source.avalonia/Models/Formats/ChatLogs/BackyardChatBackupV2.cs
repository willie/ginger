using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ginger.Models.Formats.ChatLogs;

/// <summary>
/// Backyard AI chat backup format (version 2).
/// </summary>
public class BackyardChatBackupV2
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? name;

    [JsonProperty("createdAt", NullValueHandling = NullValueHandling.Ignore)]
    public long? createdAt;

    [JsonProperty("updatedAt", NullValueHandling = NullValueHandling.Ignore)]
    public long? updatedAt;

    [JsonProperty("chat")]
    public Chat chat = new();

    [JsonProperty("version")]
    public int version = 2;

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

    public static BackyardChatBackupV2 FromBackupChat(BackupData.Chat chatData)
    {
        var backup = new BackyardChatBackupV2();
        var items = new List<ChatItem>();

        if (chatData.history?.messages != null)
        {
            int startIndex = chatData.history.hasGreeting ? 1 : 0;
            string? lastUserMessage = null;

            for (int i = startIndex; i < chatData.history.messages.Length; i++)
            {
                var msg = chatData.history.messages[i];

                if (msg.speaker == 0) // User
                {
                    // If previous was also user, add empty response
                    if (i > startIndex && chatData.history.messages[i - 1].speaker == 0)
                    {
                        items.Add(new ChatItem
                        {
                            input = lastUserMessage ?? "",
                            output = "",
                            timestamp = new DateTimeOffset(chatData.history.messages[i - 1].creationDate).ToUnixTimeMilliseconds()
                        });
                    }
                    lastUserMessage = msg.text;
                }
                else // Character
                {
                    items.Add(new ChatItem
                    {
                        input = lastUserMessage ?? "",
                        output = msg.text ?? "",
                        timestamp = new DateTimeOffset(msg.creationDate).ToUnixTimeMilliseconds()
                    });
                    lastUserMessage = null;
                }
            }
        }

        backup.name = chatData.name;
        backup.createdAt = new DateTimeOffset(chatData.creationDate).ToUnixTimeMilliseconds();
        backup.updatedAt = new DateTimeOffset(chatData.updateDate).ToUnixTimeMilliseconds();
        backup.chat.items = items.ToArray();

        return backup;
    }

    public BackupData.Chat ToBackupChat()
    {
        var messages = new List<ChatHistory.Message>();

        foreach (var item in chat.items)
        {
            var inputTime = item.timestamp != 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(item.timestamp).DateTime
                : DateTime.Now;
            var outputTime = inputTime.AddMilliseconds(10);

            if (!string.IsNullOrEmpty(item.input))
            {
                messages.Add(new ChatHistory.Message
                {
                    speaker = 0,
                    creationDate = inputTime,
                    updateDate = inputTime,
                    activeSwipe = 0,
                    swipes = new[] { item.input }
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
                    swipes = new[] { item.output }
                });
            }
        }

        return new BackupData.Chat
        {
            name = name ?? "Chat",
            creationDate = createdAt.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds(createdAt.Value).DateTime
                : DateTime.Now,
            updateDate = updatedAt.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds(updatedAt.Value).DateTime
                : DateTime.Now,
            history = new ChatHistory
            {
                messages = messages.ToArray()
            }
        };
    }

    public static BackyardChatBackupV2? FromJson(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<BackyardChatBackupV2>(json);
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
