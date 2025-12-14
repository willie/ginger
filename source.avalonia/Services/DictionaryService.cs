using System;
using System.Collections.Generic;
using System.IO;

namespace Ginger.Services;

public static class DictionaryService
{
    public static IEnumerable<KeyValuePair<string, string>> Available => _dictionaries;
    private static Dictionary<string, string> _dictionaries = new();

    public static bool IsOk => _dictionaries.Count > 0;

    public static bool Load()
    {
        try
        {
            string? dictionaryPath = FindDictionaryPath();
            if (string.IsNullOrEmpty(dictionaryPath))
                return false;

            var dicFiles = Directory.GetFiles(dictionaryPath, "*.dic");
            foreach (var dicFile in dicFiles)
            {
                string fileName = Path.GetFileName(dicFile);
                if (fileName.Equals("user.dic", StringComparison.OrdinalIgnoreCase))
                    continue; // Ignore user dictionary

                string affFile = Path.Combine(
                    Path.GetDirectoryName(dicFile) ?? "",
                    Path.GetFileNameWithoutExtension(dicFile) + ".aff");

                if (!File.Exists(affFile))
                    continue; // No aff-file

                string locale = Path.GetFileNameWithoutExtension(dicFile);
                if (LocaleService.AllLocales.TryGetValue(locale.ToLowerInvariant(), out var localeDisplayName))
                {
                    _dictionaries.TryAdd(locale, localeDisplayName);
                }
            }
        }
        catch
        {
            return false;
        }

        return _dictionaries.Count > 0;
    }

    private static string? FindDictionaryPath()
    {
        var appDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(appDir, "Dictionaries"),
            Path.Combine(appDir, "..", "Dictionaries"),
            Path.Combine(appDir, "..", "..", "Dictionaries"),
            "Dictionaries"
        };

        foreach (var path in candidates)
        {
            if (Directory.Exists(path))
                return path;
        }
        return null;
    }

    public static string? GetDictionaryPath(string locale)
    {
        string? dictionaryPath = FindDictionaryPath();
        if (string.IsNullOrEmpty(dictionaryPath))
            return null;

        string dicFile = Path.Combine(dictionaryPath, locale + ".dic");
        if (File.Exists(dicFile))
            return dictionaryPath;

        return null;
    }
}
