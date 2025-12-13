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
    #region Image Format Detection

    /// <summary>
    /// Detected image format.
    /// </summary>
    public enum ImageFormat
    {
        Unknown,
        Png,
        Jpeg,
        Gif,
        WebP,
        Bmp
    }

    /// <summary>
    /// Detect the format of image data from its header bytes.
    /// </summary>
    public static ImageFormat DetectFormat(byte[] data)
    {
        if (data == null || data.Length < 12)
            return ImageFormat.Unknown;

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
            && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return ImageFormat.Png;

        // JPEG: FF D8 FF
        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return ImageFormat.Jpeg;

        // GIF: GIF87a or GIF89a
        if (data[0] == 'G' && data[1] == 'I' && data[2] == 'F' && data[3] == '8'
            && (data[4] == '7' || data[4] == '9') && data[5] == 'a')
            return ImageFormat.Gif;

        // WebP: RIFF....WEBP
        if (data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F'
            && data[8] == 'W' && data[9] == 'E' && data[10] == 'B' && data[11] == 'P')
            return ImageFormat.WebP;

        // BMP: BM
        if (data[0] == 'B' && data[1] == 'M')
            return ImageFormat.Bmp;

        return ImageFormat.Unknown;
    }

    /// <summary>
    /// Check if the image data is a PNG.
    /// </summary>
    public static bool IsPng(byte[] data) => DetectFormat(data) == ImageFormat.Png;

    /// <summary>
    /// Check if the image data is a JPEG.
    /// </summary>
    public static bool IsJpeg(byte[] data) => DetectFormat(data) == ImageFormat.Jpeg;

    /// <summary>
    /// Check if the image data is a GIF.
    /// </summary>
    public static bool IsGif(byte[] data) => DetectFormat(data) == ImageFormat.Gif;

    /// <summary>
    /// Check if the image data is a WebP.
    /// </summary>
    public static bool IsWebP(byte[] data) => DetectFormat(data) == ImageFormat.WebP;

    #endregion

    #region Animation Detection

    /// <summary>
    /// Check if the image data is an animation (animated GIF, APNG, or animated WebP).
    /// </summary>
    public static bool IsAnimation(byte[] data)
    {
        if (data == null || data.Length == 0)
            return false;

        var format = DetectFormat(data);
        return format switch
        {
            ImageFormat.Gif => IsAnimatedGif(data),
            ImageFormat.Png => IsAnimatedPng(data),
            ImageFormat.WebP => IsAnimatedWebP(data),
            _ => false
        };
    }

    /// <summary>
    /// Check if a file contains an animated image.
    /// </summary>
    public static bool IsAnimation(string filename)
    {
        if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
            return false;

        try
        {
            var data = File.ReadAllBytes(filename);
            return IsAnimation(data);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if the GIF data contains multiple frames (animation).
    /// </summary>
    public static bool IsAnimatedGif(byte[] data)
    {
        if (data == null || data.Length < 6)
            return false;

        // Must be a GIF
        if (!IsGif(data))
            return false;

        try
        {
            using var codec = SKCodec.Create(new MemoryStream(data));
            if (codec != null)
            {
                return codec.FrameCount > 1;
            }
        }
        catch
        {
        }

        // Fallback: scan for multiple image blocks
        // GIF image block starts with 0x2C (Image Descriptor)
        int imageBlockCount = 0;
        for (int i = 0; i < data.Length - 1; i++)
        {
            if (data[i] == 0x2C) // Image Descriptor
            {
                imageBlockCount++;
                if (imageBlockCount > 1)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if the PNG data is an APNG (animated PNG) by looking for acTL chunk.
    /// </summary>
    public static bool IsAnimatedPng(byte[] data)
    {
        if (data == null || data.Length < 8)
            return false;

        // Must be a PNG
        if (!IsPng(data))
            return false;

        // Look for acTL chunk (Animation Control Chunk)
        // PNG chunks: 4 bytes length + 4 bytes type + data + 4 bytes CRC
        int offset = 8; // Skip PNG signature

        while (offset + 8 <= data.Length)
        {
            // Read chunk length (big-endian)
            int length = (data[offset] << 24) | (data[offset + 1] << 16)
                       | (data[offset + 2] << 8) | data[offset + 3];

            // Read chunk type
            string chunkType = System.Text.Encoding.ASCII.GetString(data, offset + 4, 4);

            if (chunkType == "acTL")
            {
                // Found Animation Control chunk
                // Check if num_frames > 1
                if (offset + 12 <= data.Length)
                {
                    int numFrames = (data[offset + 8] << 24) | (data[offset + 9] << 16)
                                  | (data[offset + 10] << 8) | data[offset + 11];
                    return numFrames > 1;
                }
                return true;
            }

            if (chunkType == "IEND")
                break;

            // Move to next chunk (length + type + data + CRC)
            offset += 4 + 4 + length + 4;
        }

        return false;
    }

    /// <summary>
    /// Check if the WebP data is animated by looking for ANIM chunk.
    /// </summary>
    public static bool IsAnimatedWebP(byte[] data)
    {
        if (data == null || data.Length < 21)
            return false;

        // Must be a WebP
        if (!IsWebP(data))
            return false;

        // Check for VP8X chunk with animation flag
        // WebP structure: RIFF + size + WEBP + chunks
        int offset = 12; // Skip RIFF header

        while (offset + 8 <= data.Length)
        {
            string chunkId = System.Text.Encoding.ASCII.GetString(data, offset, 4);
            int chunkSize = data[offset + 4] | (data[offset + 5] << 8)
                          | (data[offset + 6] << 16) | (data[offset + 7] << 24);

            if (chunkId == "VP8X" && offset + 12 <= data.Length)
            {
                // VP8X flags byte is at offset + 8
                byte flags = data[offset + 8];
                // Bit 1 (0x02) indicates animation
                return (flags & 0x02) != 0;
            }

            if (chunkId == "ANIM")
                return true;

            // Move to next chunk
            offset += 8 + chunkSize;
            // Chunks are padded to even size
            if (chunkSize % 2 != 0)
                offset++;
        }

        return false;
    }

    #endregion

    #region Image Effects

    /// <summary>
    /// Apply Gaussian blur to an image.
    /// </summary>
    public static SKBitmap? BlurImage(SKBitmap source, float sigma = 10f)
    {
        if (source == null)
            return null;

        try
        {
            var result = new SKBitmap(source.Width, source.Height);
            using var canvas = new SKCanvas(result);
            using var paint = new SKPaint();
            paint.ImageFilter = SKImageFilter.CreateBlur(sigma, sigma);
            canvas.DrawBitmap(source, 0, 0, paint);
            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Apply blur effect to image data.
    /// </summary>
    public static byte[]? BlurImage(byte[] data, float sigma = 10f)
    {
        if (data == null || data.Length == 0)
            return null;

        using var bitmap = LoadImage(data);
        if (bitmap == null)
            return null;

        using var blurred = BlurImage(bitmap, sigma);
        return SaveToPng(blurred);
    }

    /// <summary>
    /// Darken an image by a specified amount (0.0 = no change, 1.0 = black).
    /// </summary>
    public static SKBitmap? DarkenImage(SKBitmap source, float amount = 0.5f)
    {
        if (source == null)
            return null;

        amount = Math.Clamp(amount, 0f, 1f);
        float multiplier = 1f - amount;

        try
        {
            var result = new SKBitmap(source.Width, source.Height);

            // Process pixels directly
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    var pixel = source.GetPixel(x, y);
                    var darkened = new SKColor(
                        (byte)(pixel.Red * multiplier),
                        (byte)(pixel.Green * multiplier),
                        (byte)(pixel.Blue * multiplier),
                        pixel.Alpha);
                    result.SetPixel(x, y, darkened);
                }
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Darken image data by a specified amount.
    /// </summary>
    public static byte[]? DarkenImage(byte[] data, float amount = 0.5f)
    {
        if (data == null || data.Length == 0)
            return null;

        using var bitmap = LoadImage(data);
        if (bitmap == null)
            return null;

        using var darkened = DarkenImage(bitmap, amount);
        return SaveToPng(darkened);
    }

    /// <summary>
    /// Desaturate an image (convert to grayscale) by a specified amount (0.0 = no change, 1.0 = full grayscale).
    /// </summary>
    public static SKBitmap? DesaturateImage(SKBitmap source, float amount = 1f)
    {
        if (source == null)
            return null;

        amount = Math.Clamp(amount, 0f, 1f);

        try
        {
            var result = new SKBitmap(source.Width, source.Height);

            // Process pixels directly
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    var pixel = source.GetPixel(x, y);

                    // Calculate luminance using standard weights
                    float luminance = 0.3f * pixel.Red + 0.59f * pixel.Green + 0.11f * pixel.Blue;

                    // Blend between original and grayscale based on amount
                    byte r = (byte)(pixel.Red + amount * (luminance - pixel.Red));
                    byte g = (byte)(pixel.Green + amount * (luminance - pixel.Green));
                    byte b = (byte)(pixel.Blue + amount * (luminance - pixel.Blue));

                    result.SetPixel(x, y, new SKColor(r, g, b, pixel.Alpha));
                }
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Desaturate image data (convert to grayscale).
    /// </summary>
    public static byte[]? DesaturateImage(byte[] data, float amount = 1f)
    {
        if (data == null || data.Length == 0)
            return null;

        using var bitmap = LoadImage(data);
        if (bitmap == null)
            return null;

        using var desaturated = DesaturateImage(bitmap, amount);
        return SaveToPng(desaturated);
    }

    /// <summary>
    /// Apply a color filter using a color matrix.
    /// </summary>
    public static SKBitmap? ApplyColorMatrix(SKBitmap source, float[] matrix)
    {
        if (source == null || matrix == null || matrix.Length != 20)
            return null;

        try
        {
            var result = new SKBitmap(source.Width, source.Height);
            using var canvas = new SKCanvas(result);
            using var paint = new SKPaint();
            paint.ColorFilter = SKColorFilter.CreateColorMatrix(matrix);
            canvas.DrawBitmap(source, 0, 0, paint);
            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Adjust brightness of an image (-1.0 to 1.0, where 0 = no change).
    /// </summary>
    public static SKBitmap? AdjustBrightness(SKBitmap source, float brightness)
    {
        if (source == null)
            return null;

        brightness = Math.Clamp(brightness, -1f, 1f);
        float adjustment = brightness * 255;

        // Color matrix for brightness adjustment
        float[] matrix = {
            1, 0, 0, 0, adjustment,
            0, 1, 0, 0, adjustment,
            0, 0, 1, 0, adjustment,
            0, 0, 0, 1, 0
        };

        return ApplyColorMatrix(source, matrix);
    }

    /// <summary>
    /// Adjust contrast of an image (-1.0 to 1.0, where 0 = no change).
    /// </summary>
    public static SKBitmap? AdjustContrast(SKBitmap source, float contrast)
    {
        if (source == null)
            return null;

        contrast = Math.Clamp(contrast, -1f, 1f);
        float factor = (1f + contrast) / (1f - contrast * 0.99f);
        float offset = 128 * (1 - factor);

        // Color matrix for contrast adjustment
        float[] matrix = {
            factor, 0, 0, 0, offset,
            0, factor, 0, 0, offset,
            0, 0, factor, 0, offset,
            0, 0, 0, 1, 0
        };

        return ApplyColorMatrix(source, matrix);
    }

    #endregion

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
