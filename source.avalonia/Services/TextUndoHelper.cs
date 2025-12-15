using System;
using System.Collections.Generic;
using System.Timers;

namespace Ginger.Services;

/// <summary>
/// Helper class for debounced text undo recording.
/// Groups rapid text changes into a single undo action.
/// </summary>
public class TextUndoHelper : IDisposable
{
    private readonly UndoService _undoService;
    private readonly Dictionary<string, PendingChange> _pendingChanges = new();
    private readonly Timer _debounceTimer;
    private readonly object _lock = new();
    private const int DebounceDelayMs = 800;
    private bool _disposed;

    public TextUndoHelper(UndoService undoService)
    {
        _undoService = undoService;
        _debounceTimer = new Timer(DebounceDelayMs);
        _debounceTimer.AutoReset = false;
        _debounceTimer.Elapsed += OnDebounceTimerElapsed;
    }

    /// <summary>
    /// Record a text change with debouncing.
    /// Rapid changes to the same property are grouped into one undo action.
    /// </summary>
    public void RecordTextChange(string propertyName, string description, string oldValue, string newValue, Action<string> setter)
    {
        if (oldValue == newValue)
            return;

        lock (_lock)
        {
            if (_pendingChanges.TryGetValue(propertyName, out var existing))
            {
                // Update the new value but keep the original old value
                existing.NewValue = newValue;
                existing.Description = description;
            }
            else
            {
                // Create new pending change
                _pendingChanges[propertyName] = new PendingChange
                {
                    PropertyName = propertyName,
                    Description = description,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Setter = setter
                };
            }

            // Reset debounce timer
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
    }

    /// <summary>
    /// Force immediate commit of all pending changes.
    /// Call this before operations that need a clean undo state.
    /// </summary>
    public void FlushPendingChanges()
    {
        lock (_lock)
        {
            _debounceTimer.Stop();
            CommitPendingChanges();
        }
    }

    private void OnDebounceTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            CommitPendingChanges();
        }
    }

    private void CommitPendingChanges()
    {
        if (_pendingChanges.Count == 0)
            return;

        if (_pendingChanges.Count == 1)
        {
            // Single field change
            foreach (var change in _pendingChanges.Values)
            {
                var oldVal = change.OldValue;
                var newVal = change.NewValue;
                var setter = change.Setter;
                _undoService.RecordAction(
                    change.Description,
                    () => setter(oldVal),
                    () => setter(newVal));
            }
        }
        else
        {
            // Multiple field changes - batch them
            using var batch = _undoService.BeginBatch("Edit multiple fields");
            foreach (var change in _pendingChanges.Values)
            {
                var oldVal = change.OldValue;
                var newVal = change.NewValue;
                var setter = change.Setter;
                batch.AddAction(
                    () => setter(oldVal),
                    () => setter(newVal));
            }
        }

        _pendingChanges.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _debounceTimer.Stop();
        _debounceTimer.Dispose();
        _pendingChanges.Clear();
    }

    private class PendingChange
    {
        public string PropertyName { get; set; } = "";
        public string Description { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public Action<string> Setter { get; set; } = _ => { };
    }
}
