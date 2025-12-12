using System;
using System.Collections.Generic;

namespace Ginger.Services;

/// <summary>
/// Service for managing undo/redo operations.
/// </summary>
public class UndoService
{
    private readonly Stack<UndoAction> _undoStack = new();
    private readonly Stack<UndoAction> _redoStack = new();
    private const int MaxUndoLevels = 100;
    private bool _isPerformingUndoRedo;

    public event EventHandler? StateChanged;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    /// <summary>
    /// Record an action that can be undone.
    /// </summary>
    public void RecordAction(string description, Action undoAction, Action redoAction)
    {
        if (_isPerformingUndoRedo)
            return;

        _undoStack.Push(new UndoAction(description, undoAction, redoAction));
        _redoStack.Clear(); // Clear redo stack when new action is recorded

        // Limit undo stack size
        if (_undoStack.Count > MaxUndoLevels)
        {
            var temp = new Stack<UndoAction>();
            for (int i = 0; i < MaxUndoLevels; i++)
                temp.Push(_undoStack.Pop());
            _undoStack.Clear();
            while (temp.Count > 0)
                _undoStack.Push(temp.Pop());
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Record a property change that can be undone.
    /// </summary>
    public void RecordPropertyChange<T>(string description, Action<T> setter, T oldValue, T newValue)
    {
        RecordAction(description,
            () => setter(oldValue),
            () => setter(newValue));
    }

    /// <summary>
    /// Undo the last action.
    /// </summary>
    public bool Undo()
    {
        if (!CanUndo)
            return false;

        _isPerformingUndoRedo = true;
        try
        {
            var action = _undoStack.Pop();
            action.PerformUndo();
            _redoStack.Push(action);
            StateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        finally
        {
            _isPerformingUndoRedo = false;
        }
    }

    /// <summary>
    /// Redo the last undone action.
    /// </summary>
    public bool Redo()
    {
        if (!CanRedo)
            return false;

        _isPerformingUndoRedo = true;
        try
        {
            var action = _redoStack.Pop();
            action.PerformRedo();
            _undoStack.Push(action);
            StateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        finally
        {
            _isPerformingUndoRedo = false;
        }
    }

    /// <summary>
    /// Clear all undo/redo history.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Get description of next undo action.
    /// </summary>
    public string? GetUndoDescription()
    {
        return CanUndo ? _undoStack.Peek().Description : null;
    }

    /// <summary>
    /// Get description of next redo action.
    /// </summary>
    public string? GetRedoDescription()
    {
        return CanRedo ? _redoStack.Peek().Description : null;
    }

    /// <summary>
    /// Begin a batch of operations that will be undone as a single action.
    /// </summary>
    public BatchScope BeginBatch(string description)
    {
        return new BatchScope(this, description);
    }

    private class UndoAction
    {
        public string Description { get; }
        public Action PerformUndo { get; }
        public Action PerformRedo { get; }

        public UndoAction(string description, Action undoAction, Action redoAction)
        {
            Description = description;
            PerformUndo = undoAction;
            PerformRedo = redoAction;
        }
    }

    /// <summary>
    /// Scope for batching multiple operations into a single undo action.
    /// </summary>
    public class BatchScope : IDisposable
    {
        private readonly UndoService _service;
        private readonly string _description;
        private readonly List<Action> _undoActions = new();
        private readonly List<Action> _redoActions = new();
        private bool _disposed;

        internal BatchScope(UndoService service, string description)
        {
            _service = service;
            _description = description;
        }

        public void AddAction(Action undoAction, Action redoAction)
        {
            _undoActions.Add(undoAction);
            _redoActions.Add(redoAction);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            if (_undoActions.Count > 0)
            {
                _service.RecordAction(_description,
                    () =>
                    {
                        // Execute undo actions in reverse order
                        for (int i = _undoActions.Count - 1; i >= 0; i--)
                            _undoActions[i]();
                    },
                    () =>
                    {
                        // Execute redo actions in order
                        foreach (var action in _redoActions)
                            action();
                    });
            }
        }
    }
}
