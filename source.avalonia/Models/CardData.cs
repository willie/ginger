using System;
using System.Collections.Generic;

namespace Ginger.Models;

/// <summary>
/// Simplified CardData for MVP - will be expanded to match original
/// </summary>
public class CardData
{
    public string Uuid { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Creator { get; set; } = "";
    public string Comment { get; set; } = "";
    public string UserGender { get; set; } = "";
    public string VersionString { get; set; } = "";
    public string UserPlaceholder { get; set; } = "User";
    public HashSet<string> Tags { get; set; } = new();
    public DateTime? CreationDate { get; set; }

    public DetailLevel Detail { get; set; } = DetailLevel.Normal;
    public TextStyle textStyle { get; set; } = TextStyle.Default;
    public Flag extraFlags { get; set; } = Flag.Default;

    // For Backyard integration
    public AssetCollection assets { get; set; } = new AssetCollection();
    public ImageRef portraitImage { get; set; }
    public string creator { get; set; }
    public DateTime? creationDate { get; set; }

    public enum DetailLevel
    {
        Low = -1,
        Normal = 0,
        High = 1,
    }

    public enum TextStyle
    {
        None = 0,
        Chat,       // Asterisks
        Novel,      // Quotes
        Mixed,      // Quotes + Asterisks
        Decorative, // Decorative quotes
        Bold,       // Double asterisks
        Parentheses,// Parentheses instead of asterisks
        Japanese,   // Japanese quotes
        Default = None,
    }

    [Flags]
    public enum Flag : int
    {
        None = 0,
        PruneScenario = 1 << 0,
        UserPersonaInScenario = 1 << 1,

        OmitPersonality = 1 << 23,
        OmitUserPersona = 1 << 24,
        OmitSystemPrompt = 1 << 25,
        OmitAttributes = 1 << 26,
        OmitScenario = 1 << 27,
        OmitExample = 1 << 28,
        OmitGreeting = 1 << 29,
        OmitGrammar = 1 << 30,
        OmitLore = 1 << 31,

        Default = None,
    }
}
