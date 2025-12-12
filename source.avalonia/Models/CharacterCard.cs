using System;
using System.Collections.Generic;

namespace Ginger.Models;

/// <summary>
/// Represents a loaded character card with all its data.
/// </summary>
public class CharacterCard
{
    public string Name { get; set; } = "";
    public string SpokenName { get; set; } = "";
    public string? Gender { get; set; }
    public string Creator { get; set; } = "";
    public string Version { get; set; } = "";
    public string CreatorNotes { get; set; } = "";
    public HashSet<string> Tags { get; set; } = new();

    // Character content
    public string Persona { get; set; } = "";
    public string Personality { get; set; } = "";
    public string Scenario { get; set; } = "";
    public string Greeting { get; set; } = "";
    public string Example { get; set; } = "";
    public string System { get; set; } = "";
    public string PostHistoryInstructions { get; set; } = "";
    public List<string> AlternateGreetings { get; set; } = new();

    // Lorebook
    public Lorebook? Lorebook { get; set; }

    // Portrait image data
    public byte[]? PortraitData { get; set; }

    // User settings
    public string UserPlaceholder { get; set; } = "User";
    public string? UserGender { get; set; }

    // Notes
    public string Notes { get; set; } = "";

    // Source format
    public CardFormat SourceFormat { get; set; } = CardFormat.Unknown;

    public enum CardFormat
    {
        Unknown,
        TavernV1,
        TavernV2,
        TavernV3,
        Faraday,
        Ginger,
        Agnaistic,
        Pygmalion,
        TextGenWebUI,
    }

    /// <summary>
    /// Create a CharacterCard from a TavernCardV2 object.
    /// </summary>
    public static CharacterCard FromTavernV2(TavernCardV2 tavern, byte[]? portraitData = null)
    {
        var card = new CharacterCard
        {
            SourceFormat = CardFormat.TavernV2,
            Name = tavern.data.name,
            SpokenName = tavern.data.name,
            Creator = tavern.data.creator,
            Version = tavern.data.character_version,
            CreatorNotes = tavern.data.creator_notes,
            Tags = new HashSet<string>(tavern.data.tags ?? Array.Empty<string>()),
            Persona = tavern.data.persona,
            Personality = tavern.data.personality,
            Scenario = tavern.data.scenario,
            Greeting = tavern.data.greeting,
            Example = tavern.data.example,
            System = tavern.data.system,
            PostHistoryInstructions = tavern.data.post_history_instructions,
            AlternateGreetings = new List<string>(tavern.data.alternate_greetings ?? Array.Empty<string>()),
            PortraitData = portraitData,
        };

        // Convert lorebook
        if (tavern.data.character_book != null)
        {
            card.Lorebook = Lorebook.FromTavernCharacterBook(tavern.data.character_book);
        }

        return card;
    }

    /// <summary>
    /// Convert this CharacterCard to a TavernCardV2 for export.
    /// </summary>
    public TavernCardV2 ToTavernV2()
    {
        var tavern = new TavernCardV2
        {
            spec = "chara_card_v2",
            spec_version = "2.0",
            data = new TavernCardV2.Data
            {
                name = Name,
                creator = Creator,
                character_version = Version,
                creator_notes = CreatorNotes,
                tags = Tags.Count > 0 ? new List<string>(Tags).ToArray() : Array.Empty<string>(),
                persona = Persona,
                personality = Personality,
                scenario = Scenario,
                greeting = Greeting,
                example = Example,
                system = System,
                post_history_instructions = PostHistoryInstructions,
                alternate_greetings = AlternateGreetings.ToArray(),
            }
        };

        // Convert lorebook
        if (Lorebook != null && Lorebook.Entries.Count > 0)
        {
            tavern.data.character_book = Lorebook.ToTavernCharacterBook();
        }

        return tavern;
    }
}

/// <summary>
/// Represents a lorebook/world info for a character.
/// </summary>
public class Lorebook
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int ScanDepth { get; set; } = 50;
    public int TokenBudget { get; set; } = 500;
    public bool RecursiveScanning { get; set; } = false;
    public List<LorebookEntry> Entries { get; set; } = new();

    public class LorebookEntry
    {
        public int Id { get; set; }
        public string[] Keys { get; set; } = Array.Empty<string>();
        public string[] SecondaryKeys { get; set; } = Array.Empty<string>();
        public string Name { get; set; } = "";
        public string Comment { get; set; } = "";
        public string Content { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public bool Constant { get; set; } = false;
        public bool Selective { get; set; } = false;
        public bool CaseSensitive { get; set; } = false;
        public int InsertionOrder { get; set; } = 100;
        public int Priority { get; set; } = 10;
        public string Position { get; set; } = "before_char";
    }

    public static Lorebook FromTavernCharacterBook(TavernCardV2.CharacterBook book)
    {
        var lorebook = new Lorebook
        {
            Name = book.name ?? "",
            Description = book.description ?? "",
            ScanDepth = book.scan_depth,
            TokenBudget = book.token_budget,
            RecursiveScanning = book.recursive_scanning,
        };

        foreach (var entry in book.entries ?? Array.Empty<TavernCardV2.CharacterBook.Entry>())
        {
            lorebook.Entries.Add(new LorebookEntry
            {
                Id = entry.id,
                Keys = entry.keys ?? Array.Empty<string>(),
                SecondaryKeys = entry.secondary_keys,
                Name = entry.name,
                Comment = entry.comment,
                Content = entry.content,
                Enabled = entry.enabled,
                Constant = entry.constant,
                Selective = entry.selective,
                CaseSensitive = entry.case_sensitive,
                InsertionOrder = entry.insertion_order,
                Priority = entry.priority,
                Position = entry.position,
            });
        }

        return lorebook;
    }

    public TavernCardV2.CharacterBook ToTavernCharacterBook()
    {
        var book = new TavernCardV2.CharacterBook
        {
            name = Name,
            description = Description,
            scan_depth = ScanDepth,
            token_budget = TokenBudget,
            recursive_scanning = RecursiveScanning,
            entries = new TavernCardV2.CharacterBook.Entry[Entries.Count],
        };

        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            book.entries[i] = new TavernCardV2.CharacterBook.Entry
            {
                id = entry.Id > 0 ? entry.Id : i + 1,
                keys = entry.Keys,
                secondary_keys = entry.SecondaryKeys,
                name = entry.Name,
                comment = entry.Comment,
                content = entry.Content,
                enabled = entry.Enabled,
                constant = entry.Constant,
                selective = entry.Selective,
                case_sensitive = entry.CaseSensitive,
                insertion_order = entry.InsertionOrder,
                priority = entry.Priority,
                position = entry.Position,
            };
        }

        return book;
    }
}
