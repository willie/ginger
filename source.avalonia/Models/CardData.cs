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
    public TextStyle Style { get; set; } = TextStyle.None;

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
    }
}
