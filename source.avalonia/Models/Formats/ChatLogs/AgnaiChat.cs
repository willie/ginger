#nullable enable

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ginger.Models.Formats.ChatLogs;

/// <summary>
/// Parser for Agnai.chat export format.
/// </summary>
public class AgnaiChat
{
    public class Message
    {
        [JsonProperty("userId", NullValueHandling = NullValueHandling.Ignore)]
        public string? userId;

        [JsonProperty("characterId", NullValueHandling = NullValueHandling.Ignore)]
        public string? characterId;

        [JsonProperty("msg")]
        public string text = "";
    }

    [JsonProperty("messages")]
    public Message[] messages = Array.Empty<Message>();

    public static AgnaiChat? FromJson(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<AgnaiChat>(json);
        }
        catch
        {
            return null;
        }
    }

    public ChatHistory ToChat()
    {
        var result = new List<ChatHistory.Message>();
        foreach (var message in messages)
        {
            DateTime messageTime = DateTime.Now;
            bool isUser = message.userId != null;

            if (!string.IsNullOrEmpty(message.text))
            {
                string text = message.text;
                text = text.Replace("<START>", "");
                text = GingerString.FromTavern(text).ToString();

                result.Add(new ChatHistory.Message
                {
                    speaker = isUser ? 0 : 1,
                    creationDate = messageTime,
                    updateDate = messageTime,
                    activeSwipe = 0,
                    swipes = new[] { text },
                });
            }
        }

        return new ChatHistory
        {
            messages = result.ToArray(),
        };
    }

    public static bool Validate(string jsonData)
    {
        try
        {
            var parsed = JsonConvert.DeserializeObject<AgnaiChat>(jsonData);
            return parsed?.messages != null;
        }
        catch
        {
            return false;
        }
    }
}
