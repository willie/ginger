using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WeCantSpell.Hunspell;

namespace Ginger.Services;

/// <summary>
/// Service for spell checking using Hunspell dictionaries.
/// </summary>
public class SpellCheckService : IDisposable
{
    private WordList? _dictionary;
    private bool _isLoaded;
    private readonly HashSet<string> _customWords = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _ignoredWords = new(StringComparer.OrdinalIgnoreCase);

    public bool IsLoaded => _isLoaded;
    public string? CurrentLanguage { get; private set; }

    /// <summary>
    /// Load a Hunspell dictionary from .aff and .dic files.
    /// </summary>
    public async Task<bool> LoadDictionaryAsync(string affPath, string dicPath)
    {
        try
        {
            if (!File.Exists(affPath) || !File.Exists(dicPath))
                return false;

            _dictionary = await Task.Run(() => WordList.CreateFromFiles(dicPath, affPath));
            _isLoaded = _dictionary != null;
            CurrentLanguage = Path.GetFileNameWithoutExtension(dicPath);
            return _isLoaded;
        }
        catch
        {
            _isLoaded = false;
            return false;
        }
    }

    /// <summary>
    /// Load a dictionary from a directory containing .aff and .dic files.
    /// </summary>
    public async Task<bool> LoadDictionaryFromDirectoryAsync(string directory, string language = "en_US")
    {
        string affPath = Path.Combine(directory, $"{language}.aff");
        string dicPath = Path.Combine(directory, $"{language}.dic");
        return await LoadDictionaryAsync(affPath, dicPath);
    }

    /// <summary>
    /// Check if a word is spelled correctly.
    /// </summary>
    public bool CheckWord(string word)
    {
        if (!_isLoaded || _dictionary == null)
            return true; // Assume correct if no dictionary loaded

        if (string.IsNullOrWhiteSpace(word))
            return true;

        // Check custom words first
        if (_customWords.Contains(word) || _ignoredWords.Contains(word))
            return true;

        // Check the dictionary
        return _dictionary.Check(word);
    }

    /// <summary>
    /// Get spelling suggestions for a misspelled word.
    /// </summary>
    public IEnumerable<string> GetSuggestions(string word)
    {
        if (!_isLoaded || _dictionary == null)
            return Array.Empty<string>();

        return _dictionary.Suggest(word).Take(10);
    }

    /// <summary>
    /// Add a word to the custom dictionary.
    /// </summary>
    public void AddToCustomDictionary(string word)
    {
        if (!string.IsNullOrWhiteSpace(word))
            _customWords.Add(word);
    }

    /// <summary>
    /// Ignore a word for the current session.
    /// </summary>
    public void IgnoreWord(string word)
    {
        if (!string.IsNullOrWhiteSpace(word))
            _ignoredWords.Add(word);
    }

    /// <summary>
    /// Clear session ignored words.
    /// </summary>
    public void ClearIgnoredWords()
    {
        _ignoredWords.Clear();
    }

    /// <summary>
    /// Get all misspelled words in a text.
    /// </summary>
    public IEnumerable<SpellingError> CheckText(string text)
    {
        if (!_isLoaded || _dictionary == null || string.IsNullOrEmpty(text))
            yield break;

        var words = ExtractWords(text);
        foreach (var (word, start, length) in words)
        {
            if (!CheckWord(word))
            {
                yield return new SpellingError
                {
                    Word = word,
                    Start = start,
                    Length = length,
                    Suggestions = GetSuggestions(word).ToArray()
                };
            }
        }
    }

    /// <summary>
    /// Extract words and their positions from text.
    /// </summary>
    private static IEnumerable<(string word, int start, int length)> ExtractWords(string text)
    {
        int start = -1;
        int i = 0;

        while (i < text.Length)
        {
            if (char.IsLetter(text[i]))
            {
                if (start < 0)
                    start = i;
            }
            else
            {
                if (start >= 0)
                {
                    int length = i - start;
                    string word = text.Substring(start, length);
                    yield return (word, start, length);
                    start = -1;
                }
            }
            i++;
        }

        // Handle word at end of text
        if (start >= 0)
        {
            int length = i - start;
            string word = text.Substring(start, length);
            yield return (word, start, length);
        }
    }

    /// <summary>
    /// Load custom words from a file.
    /// </summary>
    public async Task LoadCustomDictionaryAsync(string path)
    {
        if (!File.Exists(path))
            return;

        var lines = await File.ReadAllLinesAsync(path);
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
                _customWords.Add(line.Trim());
        }
    }

    /// <summary>
    /// Save custom words to a file.
    /// </summary>
    public async Task SaveCustomDictionaryAsync(string path)
    {
        await File.WriteAllLinesAsync(path, _customWords.OrderBy(w => w));
    }

    public void Dispose()
    {
        _dictionary = null;
        _isLoaded = false;
    }
}

/// <summary>
/// Represents a spelling error with position and suggestions.
/// </summary>
public class SpellingError
{
    public string Word { get; set; } = "";
    public int Start { get; set; }
    public int Length { get; set; }
    public string[] Suggestions { get; set; } = Array.Empty<string>();
}
