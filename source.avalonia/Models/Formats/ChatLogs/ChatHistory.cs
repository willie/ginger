using System;

namespace Ginger.Models.Formats.ChatLogs;

/// <summary>
/// Represents a chat history with messages.
/// </summary>
public class ChatHistory
{
    public string name = "";
    public Message[] messages = Array.Empty<Message>();

    public int count => messages?.Length ?? 0;
    public bool hasGreeting => messages?.Length > 0 && messages[0].speaker != 0;

    public class Message
    {
        public int speaker;
        public DateTime creationDate;
        public DateTime updateDate;
        public int activeSwipe;
        public string[] swipes = Array.Empty<string>();

        public string text => swipes != null && activeSwipe >= 0 && activeSwipe < swipes.Length
            ? swipes[activeSwipe]
            : "";
    }
}

/// <summary>
/// Backup data structure for chat export/import.
/// </summary>
public class BackupData
{
    public class Chat
    {
        public string name = "";
        public DateTime creationDate;
        public DateTime updateDate;
        public string? backgroundName;
        public ChatHistory? history;
        public string[]? participants;
        public ChatStaging? staging;
        public ChatParameters? parameters;
    }
}

public class ChatStaging
{
    public string system = "";
    public string scenario = "";
    public CharacterMessage greeting = new();
    public string example = "";
    public string grammar = "";
    public string authorNote = "";
    public bool pruneExampleChat = true;
    public bool ttsAutoPlay = false;
    public string? ttsInputFilter;
}

public class CharacterMessage
{
    public string text = "";

    public static CharacterMessage FromString(string? text)
    {
        return new CharacterMessage { text = text ?? "" };
    }
}

public class ChatParameters
{
    public string model = "";
    public decimal temperature = 1.2m;
    public decimal topP = 0.9m;
    public decimal minP = 0.1m;
    public int topK = 30;
    public bool minPEnabled = true;
    public int repeatLastN = 256;
    public decimal repeatPenalty = 1.05m;
    public string? promptTemplate;
}
