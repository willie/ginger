using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Ginger.Views.Dialogs;

public partial class ProgressDialog : Window
{
    private CancellationTokenSource? _cts;
    private bool _canCancel = true;

    public ProgressDialog()
    {
        InitializeComponent();
    }

    public string Message
    {
        get => MessageText.Text ?? "";
        set => MessageText.Text = value;
    }

    public bool IsIndeterminate
    {
        get => ProgressBar.IsIndeterminate;
        set => ProgressBar.IsIndeterminate = value;
    }

    public double Progress
    {
        get => ProgressBar.Value;
        set
        {
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = value;
        }
    }

    public double MaxProgress
    {
        get => ProgressBar.Maximum;
        set => ProgressBar.Maximum = value;
    }

    public bool CanCancel
    {
        get => _canCancel;
        set
        {
            _canCancel = value;
            CancelButton.IsEnabled = value;
            CancelButton.IsVisible = value;
        }
    }

    public CancellationToken CancellationToken => _cts?.Token ?? CancellationToken.None;

    /// <summary>
    /// Run an async task while showing the progress dialog.
    /// </summary>
    public async Task<T?> RunAsync<T>(Func<CancellationToken, IProgress<(string message, double progress)>, Task<T>> task, bool canCancel = true)
    {
        _cts = new CancellationTokenSource();
        CanCancel = canCancel;

        var progress = new Progress<(string message, double progress)>(report =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                Message = report.message;
                if (report.progress >= 0)
                {
                    Progress = report.progress;
                }
            });
        });

        try
        {
            var result = await task(_cts.Token, progress);
            Close(true);
            return result;
        }
        catch (OperationCanceledException)
        {
            Close(false);
            return default;
        }
        catch (Exception)
        {
            Close(false);
            throw;
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    /// <summary>
    /// Run an async task (no return value) while showing the progress dialog.
    /// </summary>
    public async Task<bool> RunAsync(Func<CancellationToken, IProgress<(string message, double progress)>, Task> task, bool canCancel = true)
    {
        _cts = new CancellationTokenSource();
        CanCancel = canCancel;

        var progress = new Progress<(string message, double progress)>(report =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                Message = report.message;
                if (report.progress >= 0)
                {
                    Progress = report.progress;
                }
            });
        });

        try
        {
            await task(_cts.Token, progress);
            Close(true);
            return true;
        }
        catch (OperationCanceledException)
        {
            Close(false);
            return false;
        }
        catch (Exception)
        {
            Close(false);
            throw;
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        CancelButton.IsEnabled = false;
        Message = "Cancelling...";
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Prevent closing while task is running unless cancelled
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            e.Cancel = true;
        }
        base.OnClosing(e);
    }
}
