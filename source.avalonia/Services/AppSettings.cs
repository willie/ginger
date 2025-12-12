using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ginger;

/// <summary>
/// Application settings with JSON persistence for cross-platform use.
/// </summary>
public static class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Ginger",
        "settings.json");

    public static class Settings
    {
        public static bool AutoConvertNames { get; set; } = true;
        public static string UserPlaceholder { get; set; } = "User";
        public static bool AllowNSFW { get; set; } = false;
        public static bool ConfirmNSFW { get; set; } = true;
        public static int UndoSteps { get; set; } = 80;
        public static int TokenBudget { get; set; } = 0;
        public static bool AutoBreakLine { get; set; } = true;
        public static string Locale { get; set; } = "en";
        public static bool DarkTheme { get; set; } = false;

        public enum OutputPreviewFormat
        {
            Default,
            SillyTavern,
            Faraday,
            FaradayParty,
            PlainText,
        }
        public static OutputPreviewFormat PreviewFormat { get; set; } = OutputPreviewFormat.Default;

        public static bool SpellChecking { get; set; } = true;
        public static string Dictionary { get; set; } = "en_US";
        public static bool ShowRecipeCategory { get; set; } = false;
        public static int LoreEntriesPerPage { get; set; } = 10;
        public static bool EnableRearrangeLoreMode { get; set; } = false;
        public static string? FontFamily { get; set; }
        public static double FontSize { get; set; } = 14;
    }

    public enum CharacterSortOrder
    {
        ByName,
        ByCreation,
        ByLastMessage,
        ByCustom,
        Default = ByCustom,
    }

    public static class User
    {
        public static int LastImportCharacterFilter { get; set; } = 0;
        public static int LastImportLorebookFilter { get; set; } = 0;
        public static int LastImportChatFilter { get; set; } = 0;
        public static int LastExportCharacterFilter { get; set; } = 0;
        public static int LastExportLorebookFilter { get; set; } = 0;
        public static int LastExportChatFilter { get; set; } = 0;
        public static int LastBulkImportCharacterFilter { get; set; } = 0;
        public static int LastBulkExportCharacterFilter { get; set; } = 0;
        public static int LastBulkExportGroupFilter { get; set; } = 0;

        public static bool LaunchTextEditor { get; set; } = true;

        public static string FindMatch { get; set; } = "";
        public static bool FindMatchCase { get; set; } = false;
        public static bool FindWholeWords { get; set; } = false;

        public static string ReplaceLastFind { get; set; } = "";
        public static string ReplaceLastReplace { get; set; } = "";
        public static bool ReplaceMatchCase { get; set; } = false;
        public static bool ReplaceWholeWords { get; set; } = false;
        public static bool ReplaceLorebooks { get; set; } = true;

        public static bool SnippetSwapPronouns { get; set; } = false;

        public static CharacterSortOrder SortCharacters { get; set; } = CharacterSortOrder.Default;
        public static CharacterSortOrder SortGroups { get; set; } = CharacterSortOrder.Default;

        public static bool ShowCardInfo { get; set; } = true;
        public static bool ShowUserInfo { get; set; } = true;
        public static bool ShowOutputSettings { get; set; } = false;
        public static bool ShowOutputComponents { get; set; } = false;
        public static bool ShowBackground { get; set; } = false;
        public static bool ShowStats { get; set; } = false;

        public static double WindowX { get; set; } = 0;
        public static double WindowY { get; set; } = 0;
        public static double WindowWidth { get; set; } = 1200;
        public static double WindowHeight { get; set; } = 800;
        public static bool WindowMaximized { get; set; } = false;
    }

    public static class Paths
    {
        public static string? LastCharacterPath { get; set; }
        public static string? LastImagePath { get; set; }
        public static string? LastImportExportPath { get; set; }
    }

    public static class BackyardLink
    {
        public static bool Enabled { get; set; } = false;
        public static bool Strict { get; set; } = true;
        public static string? Location { get; set; }
        public static bool Autosave { get; set; } = true;
        public static bool AlwaysLinkOnImport { get; set; } = true;
        public static string BulkImportFolderName { get; set; } = "Imported from Ginger";

        public enum ActiveChatSetting { First, Last, All }
        public static ActiveChatSetting ApplyChatSettings { get; set; } = ActiveChatSetting.Last;
        public static bool UsePortraitAsBackground { get; set; } = false;
        public static bool ImportAlternateGreetings { get; set; } = false;

        public static bool PruneExampleChat { get; set; } = true;
        public static bool MarkNSFW { get; set; } = true;
        public static bool WriteAuthorNote { get; set; } = true;
        public static bool WriteUserPersona { get; set; } = false;
        public static bool BackupModelSettings { get; set; } = true;
        public static bool BackupUserPersona { get; set; } = true;
    }

    public static class BackyardSettings
    {
        public static bool AutoArrangePortraits { get; set; } = true;
        public static Integration.Backyard.ChatParameters UserSettings { get; set; } = new();
    }

    public static List<MRUEntry> MRUList { get; set; } = new();

    public class MRUEntry
    {
        public string? Filename { get; set; }
        public string? CharacterName { get; set; }
    }

    /// <summary>
    /// Load settings from JSON file.
    /// </summary>
    public static bool Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return false;

            var json = File.ReadAllText(SettingsPath);
            var data = JsonSerializer.Deserialize<SettingsData>(json);
            if (data == null)
                return false;

            // Settings
            Settings.AutoConvertNames = data.AutoConvertNames ?? Settings.AutoConvertNames;
            Settings.UserPlaceholder = data.UserPlaceholder ?? Settings.UserPlaceholder;
            Settings.AllowNSFW = data.AllowNSFW ?? Settings.AllowNSFW;
            Settings.ConfirmNSFW = data.ConfirmNSFW ?? Settings.ConfirmNSFW;
            Settings.UndoSteps = data.UndoSteps ?? Settings.UndoSteps;
            Settings.TokenBudget = data.TokenBudget ?? Settings.TokenBudget;
            Settings.AutoBreakLine = data.AutoBreakLine ?? Settings.AutoBreakLine;
            Settings.Locale = data.Locale ?? Settings.Locale;
            Settings.DarkTheme = data.DarkTheme ?? Settings.DarkTheme;
            Settings.PreviewFormat = data.PreviewFormat ?? Settings.PreviewFormat;
            Settings.SpellChecking = data.SpellChecking ?? Settings.SpellChecking;
            Settings.Dictionary = data.Dictionary ?? Settings.Dictionary;
            Settings.ShowRecipeCategory = data.ShowRecipeCategory ?? Settings.ShowRecipeCategory;
            Settings.LoreEntriesPerPage = data.LoreEntriesPerPage ?? Settings.LoreEntriesPerPage;
            Settings.EnableRearrangeLoreMode = data.EnableRearrangeLoreMode ?? Settings.EnableRearrangeLoreMode;
            Settings.FontFamily = data.FontFamily ?? Settings.FontFamily;
            Settings.FontSize = data.FontSize ?? Settings.FontSize;

            // User
            User.LastImportCharacterFilter = data.LastImportCharacterFilter ?? User.LastImportCharacterFilter;
            User.LastImportLorebookFilter = data.LastImportLorebookFilter ?? User.LastImportLorebookFilter;
            User.LastImportChatFilter = data.LastImportChatFilter ?? User.LastImportChatFilter;
            User.LastExportCharacterFilter = data.LastExportCharacterFilter ?? User.LastExportCharacterFilter;
            User.LastExportLorebookFilter = data.LastExportLorebookFilter ?? User.LastExportLorebookFilter;
            User.LastExportChatFilter = data.LastExportChatFilter ?? User.LastExportChatFilter;
            User.LastBulkImportCharacterFilter = data.LastBulkImportCharacterFilter ?? User.LastBulkImportCharacterFilter;
            User.LastBulkExportCharacterFilter = data.LastBulkExportCharacterFilter ?? User.LastBulkExportCharacterFilter;
            User.LastBulkExportGroupFilter = data.LastBulkExportGroupFilter ?? User.LastBulkExportGroupFilter;
            User.LaunchTextEditor = data.LaunchTextEditor ?? User.LaunchTextEditor;
            User.FindMatch = data.FindMatch ?? User.FindMatch;
            User.FindMatchCase = data.FindMatchCase ?? User.FindMatchCase;
            User.FindWholeWords = data.FindWholeWords ?? User.FindWholeWords;
            User.ReplaceLastFind = data.ReplaceLastFind ?? User.ReplaceLastFind;
            User.ReplaceLastReplace = data.ReplaceLastReplace ?? User.ReplaceLastReplace;
            User.ReplaceMatchCase = data.ReplaceMatchCase ?? User.ReplaceMatchCase;
            User.ReplaceWholeWords = data.ReplaceWholeWords ?? User.ReplaceWholeWords;
            User.ReplaceLorebooks = data.ReplaceLorebooks ?? User.ReplaceLorebooks;
            User.SnippetSwapPronouns = data.SnippetSwapPronouns ?? User.SnippetSwapPronouns;
            User.SortCharacters = data.SortCharacters ?? User.SortCharacters;
            User.SortGroups = data.SortGroups ?? User.SortGroups;
            User.ShowCardInfo = data.ShowCardInfo ?? User.ShowCardInfo;
            User.ShowUserInfo = data.ShowUserInfo ?? User.ShowUserInfo;
            User.ShowOutputSettings = data.ShowOutputSettings ?? User.ShowOutputSettings;
            User.ShowOutputComponents = data.ShowOutputComponents ?? User.ShowOutputComponents;
            User.ShowBackground = data.ShowBackground ?? User.ShowBackground;
            User.ShowStats = data.ShowStats ?? User.ShowStats;
            User.WindowX = data.WindowX ?? User.WindowX;
            User.WindowY = data.WindowY ?? User.WindowY;
            User.WindowWidth = data.WindowWidth ?? User.WindowWidth;
            User.WindowHeight = data.WindowHeight ?? User.WindowHeight;
            User.WindowMaximized = data.WindowMaximized ?? User.WindowMaximized;

            // Paths
            Paths.LastCharacterPath = data.LastCharacterPath;
            Paths.LastImagePath = data.LastImagePath;
            Paths.LastImportExportPath = data.LastImportExportPath;

            // Backyard
            BackyardLink.Enabled = data.BackyardEnabled ?? BackyardLink.Enabled;
            BackyardLink.Strict = data.BackyardStrict ?? BackyardLink.Strict;
            BackyardLink.Location = data.BackyardLocation;
            BackyardLink.Autosave = data.BackyardAutosave ?? BackyardLink.Autosave;
            BackyardLink.AlwaysLinkOnImport = data.BackyardAlwaysLinkOnImport ?? BackyardLink.AlwaysLinkOnImport;
            BackyardLink.BulkImportFolderName = data.BackyardBulkImportFolderName ?? BackyardLink.BulkImportFolderName;
            BackyardLink.ApplyChatSettings = data.BackyardApplyChatSettings ?? BackyardLink.ApplyChatSettings;
            BackyardLink.UsePortraitAsBackground = data.BackyardUsePortraitAsBackground ?? BackyardLink.UsePortraitAsBackground;
            BackyardLink.ImportAlternateGreetings = data.BackyardImportAlternateGreetings ?? BackyardLink.ImportAlternateGreetings;
            BackyardLink.PruneExampleChat = data.BackyardPruneExampleChat ?? BackyardLink.PruneExampleChat;
            BackyardLink.MarkNSFW = data.BackyardMarkNSFW ?? BackyardLink.MarkNSFW;
            BackyardLink.WriteAuthorNote = data.BackyardWriteAuthorNote ?? BackyardLink.WriteAuthorNote;
            BackyardLink.WriteUserPersona = data.BackyardWriteUserPersona ?? BackyardLink.WriteUserPersona;
            BackyardLink.BackupModelSettings = data.BackyardBackupModelSettings ?? BackyardLink.BackupModelSettings;
            BackyardLink.BackupUserPersona = data.BackyardBackupUserPersona ?? BackyardLink.BackupUserPersona;

            // MRU
            MRUList = data.MRU ?? new List<MRUEntry>();

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Save settings to JSON file.
    /// </summary>
    public static bool Save()
    {
        try
        {
            // Ensure directory exists
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var data = new SettingsData
            {
                // Settings
                AutoConvertNames = Settings.AutoConvertNames,
                UserPlaceholder = Settings.UserPlaceholder,
                AllowNSFW = Settings.AllowNSFW,
                ConfirmNSFW = Settings.ConfirmNSFW,
                UndoSteps = Settings.UndoSteps,
                TokenBudget = Settings.TokenBudget,
                AutoBreakLine = Settings.AutoBreakLine,
                Locale = Settings.Locale,
                DarkTheme = Settings.DarkTheme,
                PreviewFormat = Settings.PreviewFormat,
                SpellChecking = Settings.SpellChecking,
                Dictionary = Settings.Dictionary,
                ShowRecipeCategory = Settings.ShowRecipeCategory,
                LoreEntriesPerPage = Settings.LoreEntriesPerPage,
                EnableRearrangeLoreMode = Settings.EnableRearrangeLoreMode,
                FontFamily = Settings.FontFamily,
                FontSize = Settings.FontSize,

                // User
                LastImportCharacterFilter = User.LastImportCharacterFilter,
                LastImportLorebookFilter = User.LastImportLorebookFilter,
                LastImportChatFilter = User.LastImportChatFilter,
                LastExportCharacterFilter = User.LastExportCharacterFilter,
                LastExportLorebookFilter = User.LastExportLorebookFilter,
                LastExportChatFilter = User.LastExportChatFilter,
                LastBulkImportCharacterFilter = User.LastBulkImportCharacterFilter,
                LastBulkExportCharacterFilter = User.LastBulkExportCharacterFilter,
                LastBulkExportGroupFilter = User.LastBulkExportGroupFilter,
                LaunchTextEditor = User.LaunchTextEditor,
                FindMatch = User.FindMatch,
                FindMatchCase = User.FindMatchCase,
                FindWholeWords = User.FindWholeWords,
                ReplaceLastFind = User.ReplaceLastFind,
                ReplaceLastReplace = User.ReplaceLastReplace,
                ReplaceMatchCase = User.ReplaceMatchCase,
                ReplaceWholeWords = User.ReplaceWholeWords,
                ReplaceLorebooks = User.ReplaceLorebooks,
                SnippetSwapPronouns = User.SnippetSwapPronouns,
                SortCharacters = User.SortCharacters,
                SortGroups = User.SortGroups,
                ShowCardInfo = User.ShowCardInfo,
                ShowUserInfo = User.ShowUserInfo,
                ShowOutputSettings = User.ShowOutputSettings,
                ShowOutputComponents = User.ShowOutputComponents,
                ShowBackground = User.ShowBackground,
                ShowStats = User.ShowStats,
                WindowX = User.WindowX,
                WindowY = User.WindowY,
                WindowWidth = User.WindowWidth,
                WindowHeight = User.WindowHeight,
                WindowMaximized = User.WindowMaximized,

                // Paths
                LastCharacterPath = Paths.LastCharacterPath,
                LastImagePath = Paths.LastImagePath,
                LastImportExportPath = Paths.LastImportExportPath,

                // Backyard
                BackyardEnabled = BackyardLink.Enabled,
                BackyardStrict = BackyardLink.Strict,
                BackyardLocation = BackyardLink.Location,
                BackyardAutosave = BackyardLink.Autosave,
                BackyardAlwaysLinkOnImport = BackyardLink.AlwaysLinkOnImport,
                BackyardBulkImportFolderName = BackyardLink.BulkImportFolderName,
                BackyardApplyChatSettings = BackyardLink.ApplyChatSettings,
                BackyardUsePortraitAsBackground = BackyardLink.UsePortraitAsBackground,
                BackyardImportAlternateGreetings = BackyardLink.ImportAlternateGreetings,
                BackyardPruneExampleChat = BackyardLink.PruneExampleChat,
                BackyardMarkNSFW = BackyardLink.MarkNSFW,
                BackyardWriteAuthorNote = BackyardLink.WriteAuthorNote,
                BackyardWriteUserPersona = BackyardLink.WriteUserPersona,
                BackyardBackupModelSettings = BackyardLink.BackupModelSettings,
                BackyardBackupUserPersona = BackyardLink.BackupUserPersona,

                // MRU
                MRU = MRUList,
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(SettingsPath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Add a file to the MRU list.
    /// </summary>
    public static void AddToMRU(string filename, string characterName)
    {
        const int MaxMRU = 10;

        // Remove existing entry with same filename
        MRUList.RemoveAll(e => string.Equals(e.Filename, filename, StringComparison.OrdinalIgnoreCase));

        // Add at beginning
        MRUList.Insert(0, new MRUEntry { Filename = filename, CharacterName = characterName });

        // Trim to max size
        while (MRUList.Count > MaxMRU)
            MRUList.RemoveAt(MRUList.Count - 1);
    }

    /// <summary>
    /// Internal class for JSON serialization.
    /// </summary>
    private class SettingsData
    {
        // Settings
        public bool? AutoConvertNames { get; set; }
        public string? UserPlaceholder { get; set; }
        public bool? AllowNSFW { get; set; }
        public bool? ConfirmNSFW { get; set; }
        public int? UndoSteps { get; set; }
        public int? TokenBudget { get; set; }
        public bool? AutoBreakLine { get; set; }
        public string? Locale { get; set; }
        public bool? DarkTheme { get; set; }
        public Settings.OutputPreviewFormat? PreviewFormat { get; set; }
        public bool? SpellChecking { get; set; }
        public string? Dictionary { get; set; }
        public bool? ShowRecipeCategory { get; set; }
        public int? LoreEntriesPerPage { get; set; }
        public bool? EnableRearrangeLoreMode { get; set; }
        public string? FontFamily { get; set; }
        public double? FontSize { get; set; }

        // User
        public int? LastImportCharacterFilter { get; set; }
        public int? LastImportLorebookFilter { get; set; }
        public int? LastImportChatFilter { get; set; }
        public int? LastExportCharacterFilter { get; set; }
        public int? LastExportLorebookFilter { get; set; }
        public int? LastExportChatFilter { get; set; }
        public int? LastBulkImportCharacterFilter { get; set; }
        public int? LastBulkExportCharacterFilter { get; set; }
        public int? LastBulkExportGroupFilter { get; set; }
        public bool? LaunchTextEditor { get; set; }
        public string? FindMatch { get; set; }
        public bool? FindMatchCase { get; set; }
        public bool? FindWholeWords { get; set; }
        public string? ReplaceLastFind { get; set; }
        public string? ReplaceLastReplace { get; set; }
        public bool? ReplaceMatchCase { get; set; }
        public bool? ReplaceWholeWords { get; set; }
        public bool? ReplaceLorebooks { get; set; }
        public bool? SnippetSwapPronouns { get; set; }
        public CharacterSortOrder? SortCharacters { get; set; }
        public CharacterSortOrder? SortGroups { get; set; }
        public bool? ShowCardInfo { get; set; }
        public bool? ShowUserInfo { get; set; }
        public bool? ShowOutputSettings { get; set; }
        public bool? ShowOutputComponents { get; set; }
        public bool? ShowBackground { get; set; }
        public bool? ShowStats { get; set; }
        public double? WindowX { get; set; }
        public double? WindowY { get; set; }
        public double? WindowWidth { get; set; }
        public double? WindowHeight { get; set; }
        public bool? WindowMaximized { get; set; }

        // Paths
        public string? LastCharacterPath { get; set; }
        public string? LastImagePath { get; set; }
        public string? LastImportExportPath { get; set; }

        // Backyard
        public bool? BackyardEnabled { get; set; }
        public bool? BackyardStrict { get; set; }
        public string? BackyardLocation { get; set; }
        public bool? BackyardAutosave { get; set; }
        public bool? BackyardAlwaysLinkOnImport { get; set; }
        public string? BackyardBulkImportFolderName { get; set; }
        public BackyardLink.ActiveChatSetting? BackyardApplyChatSettings { get; set; }
        public bool? BackyardUsePortraitAsBackground { get; set; }
        public bool? BackyardImportAlternateGreetings { get; set; }
        public bool? BackyardPruneExampleChat { get; set; }
        public bool? BackyardMarkNSFW { get; set; }
        public bool? BackyardWriteAuthorNote { get; set; }
        public bool? BackyardWriteUserPersona { get; set; }
        public bool? BackyardBackupModelSettings { get; set; }
        public bool? BackyardBackupUserPersona { get; set; }

        // MRU
        public List<MRUEntry>? MRU { get; set; }
    }
}
