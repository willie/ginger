using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ginger.Models.Formats.ChatLogs;

/// <summary>
/// Parser for Text Generation WebUI chat format.
/// </summary>
public class TextGenWebUIChat
{
    [JsonProperty("internal")]
    public string[][] internal_chat = Array.Empty<string[]>();

    [JsonProperty("visible")]
    public string[][] visible_chat = Array.Empty<string[]>();

    public static TextGenWebUIChat FromChat(ChatHistory? chatHistory)
    {
        if (chatHistory == null || chatHistory.count == 0)
        {
            return new TextGenWebUIChat
            {
                internal_chat = Array.Empty<string[]>(),
                visible_chat = Array.Empty<string[]>(),
            };
        }

        var lsInternal = new List<string[]>(chatHistory.count / 2 + 1);
        var lsVisible = new List<string[]>(chatHistory.count / 2 + 1);

        // Greeting
        int iMsg = 0;
        if (chatHistory.hasGreeting) // Include greeting
        {
            lsInternal.Add(new[] { "<|BEGIN-VISIBLE-CHAT|>", chatHistory.messages[0].text });
            lsVisible.Add(new[] { "", chatHistory.messages[0].text });
            iMsg = 1;
        }

        string? lastMessage = null;
        for (; iMsg < chatHistory.messages.Length; ++iMsg)
        {
            var message = chatHistory.messages[iMsg];
            if (message.speaker == 0) // Is user
            {
                // Check if previous message was from user
                if (iMsg > 0 && chatHistory.messages[iMsg - 1].speaker == 0)
                {
                    lsInternal.Add(new[] { lastMessage ?? "", "" });
                    lsVisible.Add(new[] { lastMessage ?? "", "" });
                }
                lastMessage = message.text;
            }
            else
            {
                lsInternal.Add(new[] { lastMessage ?? "", message.text ?? "" });
                lsVisible.Add(new[] { lastMessage ?? "", message.text ?? "" });
                lastMessage = null;
            }
        }

        return new TextGenWebUIChat
        {
            internal_chat = lsInternal.ToArray(),
            visible_chat = lsVisible.ToArray(),
        };
    }

    public ChatHistory? ToChat()
    {
        var messages = new List<ChatHistory.Message>();
        foreach (var texts in visible_chat)
        {
            if (texts == null || texts.Length != 2)
                return null;

            DateTime messageTime = DateTime.Now;

            string userMessage = texts[0] ?? "";
            string characterMessage = texts[1] ?? "";

            if (!string.IsNullOrEmpty(userMessage))
            {
                messages.Add(new ChatHistory.Message
                {
                    speaker = 0,
                    creationDate = messageTime,
                    updateDate = messageTime,
                    activeSwipe = 0,
                    swipes = new[] { userMessage },
                });
            }

            if (!string.IsNullOrEmpty(characterMessage))
            {
                messages.Add(new ChatHistory.Message
                {
                    speaker = 1,
                    creationDate = messageTime,
                    updateDate = messageTime,
                    activeSwipe = 0,
                    swipes = new[] { characterMessage },
                });
            }
        }

        DateTime time = DateTime.Now;
        for (int i = messages.Count - 1; i >= 0; --i)
        {
            messages[i].creationDate = time;
            messages[i].updateDate = time;
            time -= TimeSpan.FromMilliseconds(50);
        }

        return new ChatHistory
        {
            messages = messages.ToArray(),
        };
    }

    public static TextGenWebUIChat? FromJson(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<TextGenWebUIChat>(json);
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
            var parsed = JsonConvert.DeserializeObject<TextGenWebUIChat>(jsonData);
            return parsed?.internal_chat != null && parsed?.visible_chat != null;
        }
        catch
        {
            return false;
        }
    }
}
