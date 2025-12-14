using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ginger.Services;

public class TokenizerService : IDisposable
{
    private Generator.Output _prompt;
    private int _promptID = 0;
    private System.Timers.Timer _timer;
    private CancellationTokenSource? _cts;
    private bool _isProcessing;
    private readonly object _lock = new();

    public struct Result
    {
        public int hash;
        public int tokens_total;
        public int tokens_permanent_faraday;
        public int tokens_permanent_silly;
        public Dictionary<string, int>? loreTokens;
    }

    public delegate void OnTokenCount(Result result);
    public event OnTokenCount? TokenCountCompleted;

    public TokenizerService()
    {
        _timer = new System.Timers.Timer(300);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = false;
    }

    public void Dispose()
    {
        _timer.Dispose();
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public bool Schedule(Generator.Output prompt, int promptID)
    {
        if (prompt.isEmpty)
        {
            _timer.Stop();
            _cts?.Cancel();

            _prompt = prompt;
            _promptID = promptID;
            TokenCountCompleted?.Invoke(new Result { hash = promptID });
            return false;
        }

        if (promptID == _promptID)
            return false;

        _prompt = prompt;
        _promptID = promptID;

        _timer.Stop();
        _timer.Interval = 300;
        _timer.Start();
        return true;
    }

    private async void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        lock (_lock)
        {
            if (_isProcessing)
            {
                // Restart the timer
                _timer.Start();
                return;
            }
            _isProcessing = true;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        var prompt = _prompt;
        var promptHash = _promptID;
        var characterPlaceholder = Current.Character?.name ?? "{{char}}";
        var userPlaceholder = Current.Card?.userPlaceholder ?? "{{user}}";
        var format = AppSettings.Settings.PreviewFormat;
        var token = _cts.Token;

        try
        {
            var result = await Task.Run(() => CalculateTokens(
                prompt, characterPlaceholder, userPlaceholder, promptHash, format, token), token);

            if (!token.IsCancellationRequested)
            {
                TokenCountCompleted?.Invoke(result);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }
        finally
        {
            lock (_lock)
            {
                _isProcessing = false;
            }
        }
    }

    private static Result CalculateTokens(
        Generator.Output prompt,
        string characterPlaceholder,
        string userPlaceholder,
        int promptHash,
        AppSettings.Settings.OutputPreviewFormat format,
        CancellationToken cancellationToken)
    {
        int total = 0;
        int permanent_silly = 0;
        int permanent_faraday = 0;
        int numChannels = EnumHelper.ToInt(Recipe.Component.Count);
        Dictionary<string, int>? loreTokens = null;

        for (int i = 0; i < numChannels; ++i)
        {
            if (cancellationToken.IsCancellationRequested)
                return default;

            var channel = EnumHelper.FromInt(i, Recipe.Component.Invalid);
            var text = prompt.GetText(channel).ToTavern();
            if (string.IsNullOrEmpty(text))
                continue;

            var sb = new StringBuilder(text);
            sb.Replace("{{char}}", characterPlaceholder);
            sb.Replace("{{user}}", userPlaceholder);

            var tokens = LlamaTokenizer.LlamaTokenizer.encode(sb.ToString(), true, true);
            total += tokens.Length;

            if (channel == Recipe.Component.System || channel == Recipe.Component.System_PostHistory)
            {
                permanent_faraday += tokens.Length;
            }
            else if (channel == Recipe.Component.Persona
                || channel == Recipe.Component.UserPersona
                || channel == Recipe.Component.Scenario)
            {
                permanent_silly += tokens.Length;
                permanent_faraday += tokens.Length;
            }
        }

        if (prompt.hasLore && prompt.lorebook?.entries != null)
        {
            loreTokens = new Dictionary<string, int>();
            for (int i = 0; i < prompt.lorebook.entries.Count; ++i)
            {
                if (cancellationToken.IsCancellationRequested)
                    return default;

                var entry = prompt.lorebook.entries[i];
                string key = entry.GetUID();
                string content = string.Concat(entry.key, "=", GingerString.FromString(entry.value).ToTavern());
                var sb = new StringBuilder(content);
                sb.Replace("{{char}}", characterPlaceholder);
                sb.Replace("{{user}}", userPlaceholder);

                var tokens = LlamaTokenizer.LlamaTokenizer.encode(sb.ToString(), false, false);
                loreTokens.TryAdd(key, tokens.Length);
            }
        }

        return new Result
        {
            hash = promptHash,
            tokens_total = total,
            tokens_permanent_faraday = permanent_faraday,
            tokens_permanent_silly = permanent_silly,
            loreTokens = loreTokens,
        };
    }
}
