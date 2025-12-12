using System;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;

namespace Ginger.Services;

/// <summary>
/// Cross-platform image processing service using SkiaSharp.
/// </summary>
public class ImageService
{
    /// <summary>
    /// Load an image from a file path.
    /// </summary>
    public static SKBitmap? LoadImage(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            using var stream = File.OpenRead(filePath);
            return SKBitmap.Decode(stream);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Load an image from byte data.
    /// </summary>
    public static SKBitmap? LoadImage(byte[] data)
    {
        if (data == null || data.Length == 0)
            return null;

        try
        {
            return SKBitmap.Decode(data);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Save an image to PNG format.
    /// </summary>
    public static byte[]? SaveToPng(SKBitmap bitmap, int quality = 100)
    {
        if (bitmap == null)
            return null;

        try
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, quality);
            return data.ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Save an image to JPEG format.
    /// </summary>
    public static byte[]? SaveToJpeg(SKBitmap bitmap, int quality = 90)
    {
        if (bitmap == null)
            return null;

        try
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
            return data.ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resize an image to fit within specified dimensions while maintaining aspect ratio.
    /// </summary>
    public static SKBitmap? ResizeToFit(SKBitmap source, int maxWidth, int maxHeight)
    {
        if (source == null)
            return null;

        try
        {
            float ratio = Math.Min(
                (float)maxWidth / source.Width,
                (float)maxHeight / source.Height);

            if (ratio >= 1)
                return source.Copy(); // No resize needed

            int newWidth = (int)(source.Width * ratio);
            int newHeight = (int)(source.Height * ratio);

            return source.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resize an image to exact dimensions (may distort).
    /// </summary>
    public static SKBitmap? ResizeExact(SKBitmap source, int width, int height)
    {
        if (source == null)
            return null;

        try
        {
            return source.Resize(new SKImageInfo(width, height), SKFilterQuality.High);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Crop an image to a rectangle.
    /// </summary>
    public static SKBitmap? Crop(SKBitmap source, int x, int y, int width, int height)
    {
        if (source == null)
            return null;

        try
        {
            // Clamp values
            x = Math.Max(0, Math.Min(x, source.Width - 1));
            y = Math.Max(0, Math.Min(y, source.Height - 1));
            width = Math.Min(width, source.Width - x);
            height = Math.Min(height, source.Height - y);

            var rect = new SKRectI(x, y, x + width, y + height);
            var cropped = new SKBitmap(width, height);

            using var canvas = new SKCanvas(cropped);
            canvas.DrawBitmap(source, rect, new SKRect(0, 0, width, height));

            return cropped;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Create a thumbnail from an image.
    /// </summary>
    public static byte[]? CreateThumbnail(byte[] imageData, int maxSize = 256, SKEncodedImageFormat format = SKEncodedImageFormat.Png)
    {
        var bitmap = LoadImage(imageData);
        if (bitmap == null)
            return null;

        try
        {
            var resized = ResizeToFit(bitmap, maxSize, maxSize);
            if (resized == null)
                return null;

            using var image = SKImage.FromBitmap(resized);
            using var data = image.Encode(format, 90);

            resized.Dispose();
            bitmap.Dispose();

            return data.ToArray();
        }
        catch
        {
            bitmap.Dispose();
            return null;
        }
    }

    /// <summary>
    /// Get image dimensions without fully loading the image.
    /// </summary>
    public static (int width, int height)? GetImageDimensions(byte[] imageData)
    {
        if (imageData == null || imageData.Length == 0)
            return null;

        try
        {
            using var codec = SKCodec.Create(new MemoryStream(imageData));
            if (codec != null)
                return (codec.Info.Width, codec.Info.Height);
        }
        catch
        {
        }

        return null;
    }

    /// <summary>
    /// Convert image to Avalonia-compatible format.
    /// </summary>
    public static async Task<Avalonia.Media.Imaging.Bitmap?> ToAvaloniaBitmapAsync(byte[] imageData)
    {
        if (imageData == null || imageData.Length == 0)
            return null;

        try
        {
            return await Task.Run(() =>
            {
                using var stream = new MemoryStream(imageData);
                return new Avalonia.Media.Imaging.Bitmap(stream);
            });
        }
        catch
        {
            return null;
        }
    }
}
