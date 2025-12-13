using System;
using System.IO;
using SkiaSharp;

namespace Ginger
{
	/// <summary>
	/// Cross-platform image reference using SkiaSharp.
	/// Manages image data and provides lazy bitmap decoding.
	/// </summary>
	public class ImageRef : IDisposable
	{
		private byte[] _data;
		private SKBitmap _bitmap;
		private bool _disposed;
		private string _uid;

		/// <summary>
		/// Raw image data (PNG, JPEG, WebP, GIF, etc.)
		/// </summary>
		public byte[] data
		{
			get => _data;
			set
			{
				if (_data != value)
				{
					_data = value;
					// Invalidate cached bitmap when data changes
					_bitmap?.Dispose();
					_bitmap = null;
				}
			}
		}

		/// <summary>
		/// Original filename if loaded from file.
		/// </summary>
		public string filename { get; set; }

		/// <summary>
		/// Unique identifier for this image reference.
		/// </summary>
		public string uid
		{
			get
			{
				if (string.IsNullOrEmpty(_uid))
					_uid = Guid.NewGuid().ToString();
				return _uid;
			}
			set => _uid = value;
		}

		/// <summary>
		/// Image width in pixels.
		/// </summary>
		public int Width
		{
			get
			{
				EnsureBitmapLoaded();
				return _bitmap?.Width ?? 0;
			}
		}

		/// <summary>
		/// Image height in pixels.
		/// </summary>
		public int Height
		{
			get
			{
				EnsureBitmapLoaded();
				return _bitmap?.Height ?? 0;
			}
		}

		// Lowercase aliases for compatibility
		public int width { get => Width; set { } }
		public int height { get => Height; set { } }

		/// <summary>
		/// Whether this image reference contains no data.
		/// </summary>
		public bool isEmpty => _data == null || _data.Length == 0;

		/// <summary>
		/// Length of the raw image data in bytes.
		/// </summary>
		public int length => _data?.Length ?? 0;

		/// <summary>
		/// Create an empty ImageRef.
		/// </summary>
		public ImageRef()
		{
		}

		/// <summary>
		/// Create an ImageRef from raw image data.
		/// </summary>
		private ImageRef(byte[] imageData)
		{
			_data = imageData;
		}

		/// <summary>
		/// Create an ImageRef from an SKBitmap.
		/// </summary>
		private ImageRef(SKBitmap bitmap, bool disposable = true)
		{
			_bitmap = bitmap;
			// Encode to PNG for storage
			if (bitmap != null)
			{
				using var image = SKImage.FromBitmap(bitmap);
				using var data = image.Encode(SKEncodedImageFormat.Png, 100);
				_data = data.ToArray();
			}
		}

		/// <summary>
		/// Create an ImageRef from raw image data (PNG, JPEG, WebP, etc.)
		/// </summary>
		public static ImageRef FromBytes(byte[] data)
		{
			if (data == null || data.Length == 0)
				return null;
			return new ImageRef(data);
		}

		/// <summary>
		/// Create an ImageRef from raw image data.
		/// Alias for FromBytes for compatibility.
		/// </summary>
		public static ImageRef FromImage(byte[] data)
		{
			return FromBytes(data);
		}

		/// <summary>
		/// Create an ImageRef from an SKBitmap.
		/// </summary>
		public static ImageRef FromBitmap(SKBitmap bitmap, bool disposable = true)
		{
			if (bitmap == null)
				return null;
			return new ImageRef(bitmap, disposable);
		}

		/// <summary>
		/// Create an ImageRef from a file.
		/// </summary>
		public static ImageRef FromFile(string filename)
		{
			if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
				return null;

			try
			{
				var data = File.ReadAllBytes(filename);
				var imageRef = new ImageRef(data)
				{
					filename = filename
				};
				return imageRef;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Get the SKBitmap for this image.
		/// </summary>
		public SKBitmap ToBitmap()
		{
			EnsureBitmapLoaded();
			return _bitmap;
		}

		/// <summary>
		/// Clone this ImageRef.
		/// </summary>
		public ImageRef Clone()
		{
			if (_data == null)
				return null;

			var clone = new ImageRef
			{
				_data = (byte[])_data.Clone(),
				filename = filename,
				_uid = null // New instance gets new UID
			};
			return clone;
		}

		/// <summary>
		/// Save the image to a file.
		/// </summary>
		public bool SaveToFile(string path, SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 100)
		{
			if (_data == null || _data.Length == 0)
				return false;

			try
			{
				// If format matches source, write directly
				if (format == SKEncodedImageFormat.Png && IsPng(_data))
				{
					File.WriteAllBytes(path, _data);
					return true;
				}

				// Otherwise, decode and re-encode
				EnsureBitmapLoaded();
				if (_bitmap == null)
					return false;

				using var image = SKImage.FromBitmap(_bitmap);
				using var data = image.Encode(format, quality);
				using var stream = File.OpenWrite(path);
				data.SaveTo(stream);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Encode the image to a specific format.
		/// </summary>
		public byte[] Encode(SKEncodedImageFormat format, int quality = 100)
		{
			EnsureBitmapLoaded();
			if (_bitmap == null)
				return null;

			try
			{
				using var image = SKImage.FromBitmap(_bitmap);
				using var data = image.Encode(format, quality);
				return data.ToArray();
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Resize the image to fit within specified dimensions while maintaining aspect ratio.
		/// </summary>
		public ImageRef ResizeToFit(int maxWidth, int maxHeight)
		{
			EnsureBitmapLoaded();
			if (_bitmap == null)
				return null;

			float ratio = Math.Min(
				(float)maxWidth / _bitmap.Width,
				(float)maxHeight / _bitmap.Height);

			if (ratio >= 1)
				return Clone(); // No resize needed

			int newWidth = (int)(_bitmap.Width * ratio);
			int newHeight = (int)(_bitmap.Height * ratio);

			var resized = _bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
			return FromBitmap(resized);
		}

		/// <summary>
		/// Crop the image to specified dimensions from center.
		/// </summary>
		public ImageRef CropCenter(int width, int height)
		{
			EnsureBitmapLoaded();
			if (_bitmap == null)
				return null;

			int x = Math.Max(0, (_bitmap.Width - width) / 2);
			int y = Math.Max(0, (_bitmap.Height - height) / 2);
			int cropWidth = Math.Min(width, _bitmap.Width);
			int cropHeight = Math.Min(height, _bitmap.Height);

			var cropped = new SKBitmap(cropWidth, cropHeight);
			using var canvas = new SKCanvas(cropped);
			canvas.DrawBitmap(_bitmap,
				new SKRect(x, y, x + cropWidth, y + cropHeight),
				new SKRect(0, 0, cropWidth, cropHeight));

			return FromBitmap(cropped);
		}

		/// <summary>
		/// Implicit conversion from byte[] to ImageRef.
		/// </summary>
		public static implicit operator ImageRef(byte[] data)
		{
			return FromBytes(data);
		}

		private void EnsureBitmapLoaded()
		{
			if (_bitmap != null || _data == null || _data.Length == 0)
				return;

			try
			{
				_bitmap = SKBitmap.Decode(_data);
			}
			catch
			{
				_bitmap = null;
			}
		}

		private static bool IsPng(byte[] data)
		{
			if (data == null || data.Length < 8)
				return false;
			return data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
				&& data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_bitmap?.Dispose();
					_bitmap = null;
				}
				_disposed = true;
			}
		}

		~ImageRef()
		{
			Dispose(false);
		}
	}
}
