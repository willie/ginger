#nullable enable
#pragma warning disable CS8600, CS8601, CS8604, CS8619 // Nullable reference type warnings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ginger.Services;

public static class LocaleService
{
    public static readonly string DefaultLocale = "en";

    public static IEnumerable<KeyValuePair<string, string>> AppLocales
    {
        get { return _locales.Select(l => new KeyValuePair<string, string>(l, AllLocales.GetValueOrDefault(l.ToLowerInvariant(), l))); }
    }
    private static List<string> _locales = new();

    public static void Init()
    {
        try
        {
            string contentPath = FindContentPath();
            if (string.IsNullOrEmpty(contentPath))
                return;

            var directories = Directory.EnumerateDirectories(contentPath);
            foreach (var directory in directories)
            {
                string localeDir = Path.GetFileName(directory);
                if (AllLocales.ContainsKey(localeDir.ToLowerInvariant()))
                    _locales.Add(localeDir);
            }
        }
        catch
        {
        }

        if (!_locales.Any(s => string.Compare(s, DefaultLocale, StringComparison.OrdinalIgnoreCase) == 0))
            _locales.Add(DefaultLocale);
    }

    private static string? FindContentPath()
    {
        var appDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(appDir, "Content"),
            Path.Combine(appDir, "..", "Content"),
            Path.Combine(appDir, "..", "..", "Content"),
            Path.Combine(appDir, "..", "..", "..", "source", "Content"),
            Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "..", "source", "Content")),
        };

        foreach (var path in candidates)
        {
            if (Directory.Exists(path))
                return path;
        }
        return null;
    }

    public static bool IsValidLocale(string locale)
    {
        return _locales.FindIndex(s => string.Compare(s, locale, StringComparison.OrdinalIgnoreCase) == 0) != -1;
    }

    public static readonly Dictionary<string, string> AllLocales = new()
    {
        { "af", "Afrikaans" },
        { "ar", "Arabic" },
        { "be", "Belarusian" },
        { "bg", "Bulgarian" },
        { "bn", "Bengali" },
        { "ca", "Catalan" },
        { "cs", "Czech" },
        { "cy", "Welsh" },
        { "da", "Danish" },
        { "de", "German" },
        { "el", "Greek" },
        { "en", "English" },
        { "en_au", "English (Australia)" },
        { "en_ca", "English (Canada)" },
        { "en_gb", "English (United Kingdom)" },
        { "en_us", "English (United States)" },
        { "eo", "Esperanto" },
        { "es", "Spanish" },
        { "et", "Estonian" },
        { "eu", "Basque" },
        { "fa", "Persian" },
        { "fi", "Finnish" },
        { "fo", "Faroese" },
        { "fr", "French" },
        { "ga", "Irish" },
        { "gd", "Scottish Gaelic" },
        { "gl", "Galician" },
        { "gu", "Gujarati" },
        { "he", "Hebrew" },
        { "hi", "Hindi" },
        { "hr", "Croatian" },
        { "hu", "Hungarian" },
        { "hy", "Armenian" },
        { "id", "Indonesian" },
        { "is", "Icelandic" },
        { "it", "Italian" },
        { "ja", "Japanese" },
        { "ka", "Georgian" },
        { "kk", "Kazakh" },
        { "km", "Khmer" },
        { "kn", "Kannada" },
        { "ko", "Korean" },
        { "ku", "Kurdish" },
        { "ky", "Kyrgyz" },
        { "la", "Latin" },
        { "lb", "Luxembourgish" },
        { "lt", "Lithuanian" },
        { "lv", "Latvian" },
        { "mk", "Macedonian" },
        { "ml", "Malayalam" },
        { "mn", "Mongolian" },
        { "mr", "Marathi" },
        { "ms", "Malay" },
        { "mt", "Maltese" },
        { "my", "Burmese" },
        { "nb", "Norwegian Bokmal" },
        { "ne", "Nepali" },
        { "nl", "Dutch" },
        { "nn", "Norwegian Nynorsk" },
        { "no", "Norwegian" },
        { "or", "Oriya" },
        { "pa", "Punjabi" },
        { "pl", "Polish" },
        { "ps", "Pashto" },
        { "pt", "Portuguese" },
        { "pt_br", "Portuguese (Brazil)" },
        { "ro", "Romanian" },
        { "ru", "Russian" },
        { "si", "Sinhala" },
        { "sk", "Slovak" },
        { "sl", "Slovenian" },
        { "sq", "Albanian" },
        { "sr", "Serbian" },
        { "sv", "Swedish" },
        { "sw", "Swahili" },
        { "ta", "Tamil" },
        { "te", "Telugu" },
        { "th", "Thai" },
        { "tl", "Tagalog" },
        { "tr", "Turkish" },
        { "uk", "Ukrainian" },
        { "ur", "Urdu" },
        { "uz", "Uzbek" },
        { "vi", "Vietnamese" },
        { "yi", "Yiddish" },
        { "zh", "Chinese" },
        { "zh_cn", "Chinese (China)" },
        { "zh_tw", "Chinese (Taiwan)" },
        { "zu", "Zulu" },
    };
}
