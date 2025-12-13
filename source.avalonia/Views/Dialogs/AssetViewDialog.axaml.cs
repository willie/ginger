using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Ginger.Views.Dialogs;

public partial class AssetViewDialog : Window
{
    public bool DialogResult { get; private set; }
    public AssetCollection Assets { get; private set; } = new();
    public bool Changed { get; private set; }

    private ObservableCollection<AssetItem> _assetItems = new();
    private ListBox? _assetsList;

    public AssetViewDialog()
    {
        InitializeComponent();
        _assetsList = this.FindControl<ListBox>("AssetsList");
        if (_assetsList != null)
            _assetsList.ItemsSource = _assetItems;
    }

    public void LoadAssets(AssetCollection assets)
    {
        Assets = assets.Clone();
        RefreshAssetList();
    }

    private void RefreshAssetList()
    {
        _assetItems.Clear();
        foreach (var asset in Assets.assets.Where(a => a.isEmbeddedAsset))
        {
            _assetItems.Add(AssetItem.FromAsset(asset));
        }
    }

    private void UpdateButtonStates()
    {
        var viewButton = this.FindControl<Button>("ViewButton");
        var exportButton = this.FindControl<Button>("ExportButton");
        var removeButton = this.FindControl<Button>("RemoveButton");

        bool hasSelection = _assetsList?.SelectedItem != null;
        if (viewButton != null) viewButton.IsEnabled = hasSelection;
        if (exportButton != null) exportButton.IsEnabled = hasSelection;
        if (removeButton != null) removeButton.IsEnabled = hasSelection;
    }

    private void AssetsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateButtonStates();
    }

    private AssetFile? GetSelectedAsset()
    {
        if (_assetsList?.SelectedItem is not AssetItem item)
            return null;

        int index = _assetItems.IndexOf(item);
        if (index < 0)
            return null;

        // Map back to Assets collection
        var embeddedAssets = Assets.assets.Where(a => a.isEmbeddedAsset).ToList();
        if (index < embeddedAssets.Count)
            return embeddedAssets[index];

        return null;
    }

    private async Task AddAssetOfType(AssetFile.AssetType assetType, string title)
    {
        var window = GetWindow();
        if (window?.StorageProvider == null)
            return;

        var imageFilter = new FilePickerFileType("Image files")
        {
            Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp" }
        };

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = true,
            FileTypeFilter = new[] { imageFilter, FilePickerFileTypes.All }
        };

        var files = await window.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
            return;

        foreach (var file in files)
        {
            try
            {
                var path = file.Path.LocalPath;
                var bytes = await File.ReadAllBytesAsync(path);
                var name = Path.GetFileNameWithoutExtension(path);
                var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
                if (ext == "jpg") ext = "jpeg";

                // Check for duplicates by hash
                var data = AssetData.FromBytes(bytes);
                if (Assets.assets.Any(a => a.data.length == data.length && a.ext == ext))
                    continue; // Already exists

                var asset = new AssetFile
                {
                    name = name,
                    ext = ext,
                    type = assetType,
                    data = data,
                    isEmbeddedAsset = true
                };

                Assets.Add(asset);
                _assetItems.Add(AssetItem.FromAsset(asset));
                Changed = true;
            }
            catch
            {
                // Skip files that can't be read
            }
        }
    }

    private async void AddPortraitButton_Click(object? sender, RoutedEventArgs e)
    {
        await AddAssetOfType(AssetFile.AssetType.Portrait, "Add Portrait");
    }

    private async void AddUserPortraitButton_Click(object? sender, RoutedEventArgs e)
    {
        await AddAssetOfType(AssetFile.AssetType.UserIcon, "Add User Portrait");
    }

    private async void AddBackgroundButton_Click(object? sender, RoutedEventArgs e)
    {
        await AddAssetOfType(AssetFile.AssetType.Background, "Add Background");
    }

    private async void AddFileButton_Click(object? sender, RoutedEventArgs e)
    {
        var window = GetWindow();
        if (window?.StorageProvider == null)
            return;

        var options = new FilePickerOpenOptions
        {
            Title = "Add File",
            AllowMultiple = true,
            FileTypeFilter = new[] { FilePickerFileTypes.All }
        };

        var files = await window.StorageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
            return;

        foreach (var file in files)
        {
            try
            {
                var path = file.Path.LocalPath;
                var bytes = await File.ReadAllBytesAsync(path);
                var name = Path.GetFileNameWithoutExtension(path);
                var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();

                var data = AssetData.FromBytes(bytes);
                if (Assets.assets.Any(a => a.data.length == data.length && a.ext == ext))
                    continue;

                var assetType = GetAssetTypeFromExtension(ext);

                var asset = new AssetFile
                {
                    name = name,
                    ext = ext,
                    type = assetType,
                    data = data,
                    isEmbeddedAsset = true
                };

                Assets.Add(asset);
                _assetItems.Add(AssetItem.FromAsset(asset));
                Changed = true;
            }
            catch
            {
                // Skip files that can't be read
            }
        }
    }

    private static AssetFile.AssetType GetAssetTypeFromExtension(string ext)
    {
        var imageTypes = new[] { "jpg", "jpeg", "png", "gif", "webp", "apng", "avif" };
        if (imageTypes.Contains(ext.ToLowerInvariant()))
            return AssetFile.AssetType.Portrait;
        return AssetFile.AssetType.Other;
    }

    private void ViewButton_Click(object? sender, RoutedEventArgs e)
    {
        var asset = GetSelectedAsset();
        if (asset?.data.bytes == null || asset.data.length == 0)
            return;

        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "Ginger");
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);

            var filename = Path.Combine(tempPath, $"{asset.uid}.{asset.ext ?? "bin"}");

            if (!File.Exists(filename))
            {
                File.WriteAllBytes(filename, asset.data.bytes);
            }

            // Open with default application
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filename,
                UseShellExecute = true
            });
        }
        catch
        {
            // Failed to open file
        }
    }

    private async void ExportButton_Click(object? sender, RoutedEventArgs e)
    {
        var asset = GetSelectedAsset();
        if (asset?.data.bytes == null || asset.data.length == 0)
            return;

        var window = GetWindow();
        if (window?.StorageProvider == null)
            return;

        var filter = new FilePickerFileType($"{asset.ext?.ToUpperInvariant() ?? "All"} files")
        {
            Patterns = new[] { $"*.{asset.ext ?? "*"}" }
        };

        var options = new FilePickerSaveOptions
        {
            Title = "Export Asset",
            SuggestedFileName = $"{asset.name}.{asset.ext ?? "bin"}",
            FileTypeChoices = new[] { filter, FilePickerFileTypes.All }
        };

        var file = await window.StorageProvider.SaveFilePickerAsync(options);
        if (file == null)
            return;

        try
        {
            await File.WriteAllBytesAsync(file.Path.LocalPath, asset.data.bytes);
        }
        catch
        {
            // Failed to save
        }
    }

    private void RemoveButton_Click(object? sender, RoutedEventArgs e)
    {
        var asset = GetSelectedAsset();
        if (asset == null)
            return;

        int index = _assetsList?.SelectedIndex ?? -1;

        Assets.Remove(asset);
        if (index >= 0 && index < _assetItems.Count)
            _assetItems.RemoveAt(index);

        Changed = true;
        UpdateButtonStates();
    }

    private void ApplyButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private Window? GetWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    public class AssetItem
    {
        public string DisplayName { get; set; } = "";
        public string Extension { get; set; } = "";
        public string TypeName { get; set; } = "";
        public string SizeText { get; set; } = "";
        public string Uid { get; set; } = "";

        public static AssetItem FromAsset(AssetFile asset)
        {
            string typeName = asset.type switch
            {
                AssetFile.AssetType.Icon => "Portrait",
                AssetFile.AssetType.UserIcon => "User Portrait",
                AssetFile.AssetType.Background => "Background",
                AssetFile.AssetType.Expression => "Expression",
                _ => "Other"
            };

            string sizeText = "N/A";
            if (asset.data.length > 0)
            {
                decimal size = (decimal)asset.data.length / 1_000_000m;
                if (size >= 1.0m)
                    sizeText = string.Format(CultureInfo.InvariantCulture, "{0:0.0} MB", size);
                else
                    sizeText = string.Format(CultureInfo.InvariantCulture, "{0:0.0} KB", size * 1000);
            }

            string ext = (asset.ext ?? "").ToUpperInvariant();
            if (ext == "JPG") ext = "JPEG";

            return new AssetItem
            {
                DisplayName = asset.name ?? "Untitled",
                Extension = ext,
                TypeName = typeName,
                SizeText = sizeText,
                Uid = asset.uid
            };
        }
    }
}
