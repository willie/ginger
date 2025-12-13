// Stub implementations for Backyard integration
// These provide minimal interfaces to allow compilation
// Full implementations will be added as needed

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using SkiaSharp;
using Ginger.Services;

namespace Ginger
{
	// ImageRef is now implemented in Utility/ImageRef.cs

	/// <summary>
	/// File utility functions for image operations.
	/// </summary>
	public static partial class FileUtil
	{
		[Flags]
		public enum FileType
		{
			Unknown         = 0,
			Png             = 1 << 0,
			Json            = 1 << 1,
			Csv             = 1 << 2,
			Yaml            = 1 << 3,
			CharX           = 1 << 4,
			Backup          = 1 << 5,
			BackyardArchive = 1 << 6,
			Jpeg            = 1 << 7,
			Gif             = 1 << 8,
			Webp            = 1 << 9,

			Character       = 1 << 10,
			Lorebook        = 1 << 11,
			Group           = 1 << 12,

			Ginger          = 1 << 20,
			Faraday         = 1 << 21,
			TavernV2        = 1 << 22,
			TavernV3        = 1 << 23,
			Agnaistic       = 1 << 24,
			Pygmalion       = 1 << 25,
			TextGenWebUI    = 1 << 26,
		}

		public enum Error
		{
			NoError,
			InvalidData,
			InvalidJson,
			UnrecognizedFormat,
			NoDataFound,
			FileNotFound,
			FileReadError,
			FileWriteError,
			DiskFullError,
			FallbackError,
			UnknownError,
			InvalidFormat,
			IOError,
		}

		/// <summary>
		/// Crop a portrait image from file to specified dimensions.
		/// </summary>
		public static byte[] CropPortrait(string filename, int width, int height)
		{
			if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
				return null;

			try
			{
				var data = File.ReadAllBytes(filename);
				return CropPortrait(data, width, height);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Crop a portrait image from data to specified dimensions.
		/// Centers the crop if the image is larger than the target.
		/// Scales up if the image is smaller.
		/// </summary>
		public static byte[] CropPortrait(byte[] data, int width, int height)
		{
			if (data == null || data.Length == 0)
				return null;

			try
			{
				using var bitmap = SKBitmap.Decode(data);
				if (bitmap == null)
					return data;

				// Calculate aspect ratios
				float sourceRatio = (float)bitmap.Width / bitmap.Height;
				float targetRatio = (float)width / height;

				int srcX, srcY, srcWidth, srcHeight;

				if (sourceRatio > targetRatio)
				{
					// Source is wider - crop horizontally
					srcHeight = bitmap.Height;
					srcWidth = (int)(bitmap.Height * targetRatio);
					srcX = (bitmap.Width - srcWidth) / 2;
					srcY = 0;
				}
				else
				{
					// Source is taller - crop vertically
					srcWidth = bitmap.Width;
					srcHeight = (int)(bitmap.Width / targetRatio);
					srcX = 0;
					srcY = (bitmap.Height - srcHeight) / 2;
				}

				// Create result bitmap
				using var result = new SKBitmap(width, height);
				using var canvas = new SKCanvas(result);
				using var paint = new SKPaint
				{
					FilterQuality = SKFilterQuality.High,
					IsAntialias = true
				};

				var srcRect = new SKRect(srcX, srcY, srcX + srcWidth, srcY + srcHeight);
				var dstRect = new SKRect(0, 0, width, height);

				canvas.DrawBitmap(bitmap, srcRect, dstRect, paint);

				// Encode to PNG
				using var image = SKImage.FromBitmap(result);
				using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
				return encoded.ToArray();
			}
			catch
			{
				return data;
			}
		}

		/// <summary>
		/// Write data to a file with error handling.
		/// </summary>
		public static Error WriteToFile(string filename, byte[] data)
		{
			if (string.IsNullOrEmpty(filename))
				return Error.InvalidData;

			if (data == null || data.Length == 0)
				return Error.InvalidData;

			try
			{
				// Write to temp file first, then move
				var tempFilename = Path.GetTempFileName();
				File.WriteAllBytes(tempFilename, data);

				// Delete existing file if it exists
				if (File.Exists(filename))
					File.Delete(filename);

				File.Move(tempFilename, filename);
				return Error.NoError;
			}
			catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x70) // ERROR_DISK_FULL
			{
				return Error.DiskFullError;
			}
			catch (IOException)
			{
				return Error.FileWriteError;
			}
			catch
			{
				return Error.UnknownError;
			}
		}

		/// <summary>
		/// Read file contents.
		/// </summary>
		public static Error ReadFile(string filename, out byte[] data)
		{
			if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
			{
				data = null;
				return Error.FileNotFound;
			}

			try
			{
				data = File.ReadAllBytes(filename);
				return Error.NoError;
			}
			catch
			{
				data = null;
				return Error.FileReadError;
			}
		}

		/// <summary>
		/// Check if a FileType contains a specific flag.
		/// </summary>
		public static bool Contains(this FileType fileType, FileType flag)
		{
			return (fileType & flag) == flag;
		}

		/// <summary>
		/// Embedded data from a PNG file.
		/// </summary>
		public class EmbeddedData
		{
			public string faraday;
			public string chara;
			public string ccv3;
		}

		/// <summary>
		/// Extract JSON metadata from a PNG file.
		/// </summary>
		public static EmbeddedData ExtractJsonFromPNG(byte[] pngData)
		{
			if (pngData == null || pngData.Length < 8)
				return null;

			// Check PNG signature
			if (pngData[0] != 0x89 || pngData[1] != 0x50 || pngData[2] != 0x4E || pngData[3] != 0x47)
				return null;

			var result = new EmbeddedData();
			int offset = 8; // Skip PNG signature

			try
			{
				while (offset + 8 <= pngData.Length)
				{
					int length = (pngData[offset] << 24) | (pngData[offset + 1] << 16)
							   | (pngData[offset + 2] << 8) | pngData[offset + 3];

					string chunkType = System.Text.Encoding.ASCII.GetString(pngData, offset + 4, 4);

					if (chunkType == "tEXt" && offset + 8 + length <= pngData.Length)
					{
						// Find null separator between keyword and text
						int dataStart = offset + 8;
						int dataEnd = dataStart + length;
						int nullPos = dataStart;
						while (nullPos < dataEnd && pngData[nullPos] != 0)
							nullPos++;

						string keyword = System.Text.Encoding.Latin1.GetString(pngData, dataStart, nullPos - dataStart);
						string text = System.Text.Encoding.Latin1.GetString(pngData, nullPos + 1, dataEnd - nullPos - 1);

						// Try base64 decode for chara keyword
						if (keyword.Equals("chara", StringComparison.OrdinalIgnoreCase))
						{
							try
							{
								var decoded = Convert.FromBase64String(text);
								result.chara = System.Text.Encoding.UTF8.GetString(decoded);
							}
							catch
							{
								result.chara = text;
							}
						}
						else if (keyword.Equals("faraday", StringComparison.OrdinalIgnoreCase))
						{
							try
							{
								var decoded = Convert.FromBase64String(text);
								result.faraday = System.Text.Encoding.UTF8.GetString(decoded);
							}
							catch
							{
								result.faraday = text;
							}
						}
						else if (keyword.Equals("ccv3", StringComparison.OrdinalIgnoreCase))
						{
							try
							{
								var decoded = Convert.FromBase64String(text);
								result.ccv3 = System.Text.Encoding.UTF8.GetString(decoded);
							}
							catch
							{
								result.ccv3 = text;
							}
						}
					}

					if (chunkType == "IEND")
						break;

					offset += 4 + 4 + length + 4; // length + type + data + CRC
				}
			}
			catch
			{
				return null;
			}

			return result;
		}

		/// <summary>
		/// Write EXIF metadata to PNG bytes.
		/// </summary>
		public static byte[] WriteExifMetaDataToBytes(byte[] imageData, string keyword, string json)
		{
			if (imageData == null || imageData.Length == 0)
				return imageData;

			// Check PNG signature
			if (imageData[0] != 0x89 || imageData[1] != 0x50 || imageData[2] != 0x4E || imageData[3] != 0x47)
				return imageData;

			try
			{
				// Encode the JSON to base64
				string base64Json = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

				// Create tEXt chunk
				byte[] keywordBytes = System.Text.Encoding.Latin1.GetBytes(keyword);
				byte[] textBytes = System.Text.Encoding.Latin1.GetBytes(base64Json);
				int chunkDataLength = keywordBytes.Length + 1 + textBytes.Length; // keyword + null + text

				using var ms = new MemoryStream();

				// Copy PNG signature (first 8 bytes)
				ms.Write(imageData, 0, 8);

				// Find IEND chunk
				int offset = 8;
				while (offset + 8 <= imageData.Length)
				{
					int length = (imageData[offset] << 24) | (imageData[offset + 1] << 16)
							   | (imageData[offset + 2] << 8) | imageData[offset + 3];

					string chunkType = System.Text.Encoding.ASCII.GetString(imageData, offset + 4, 4);

					if (chunkType == "IEND")
					{
						// Write our tEXt chunk before IEND
						WritePngChunk(ms, "tEXt", keywordBytes, new byte[] { 0 }, textBytes);

						// Write IEND
						ms.Write(imageData, offset, 4 + 4 + length + 4);
						break;
					}

					// Copy this chunk
					int chunkTotalLength = 4 + 4 + length + 4;
					ms.Write(imageData, offset, chunkTotalLength);
					offset += chunkTotalLength;
				}

				return ms.ToArray();
			}
			catch
			{
				return imageData;
			}
		}

		private static void WritePngChunk(MemoryStream ms, string chunkType, params byte[][] dataArrays)
		{
			// Calculate total data length
			int totalLength = 0;
			foreach (var arr in dataArrays)
				totalLength += arr.Length;

			// Write length (big-endian)
			ms.WriteByte((byte)((totalLength >> 24) & 0xFF));
			ms.WriteByte((byte)((totalLength >> 16) & 0xFF));
			ms.WriteByte((byte)((totalLength >> 8) & 0xFF));
			ms.WriteByte((byte)(totalLength & 0xFF));

			// Write chunk type
			byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(chunkType);
			ms.Write(typeBytes, 0, 4);

			// Write data
			foreach (var arr in dataArrays)
				ms.Write(arr, 0, arr.Length);

			// Calculate and write CRC
			uint crc = CalculateCrc32(typeBytes, dataArrays);
			ms.WriteByte((byte)((crc >> 24) & 0xFF));
			ms.WriteByte((byte)((crc >> 16) & 0xFF));
			ms.WriteByte((byte)((crc >> 8) & 0xFF));
			ms.WriteByte((byte)(crc & 0xFF));
		}

		private static uint CalculateCrc32(byte[] chunkType, byte[][] dataArrays)
		{
			// PNG uses CRC-32 with initial value of all 1s and final XOR with all 1s
			uint crc = 0xFFFFFFFF;

			foreach (byte b in chunkType)
				crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);

			foreach (var arr in dataArrays)
			{
				foreach (byte b in arr)
					crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
			}

			return crc ^ 0xFFFFFFFF;
		}

		private static readonly uint[] Crc32Table = CreateCrc32Table();

		private static uint[] CreateCrc32Table()
		{
			var table = new uint[256];
			for (uint i = 0; i < 256; i++)
			{
				uint c = i;
				for (int k = 0; k < 8; k++)
				{
					if ((c & 1) != 0)
						c = 0xEDB88320 ^ (c >> 1);
					else
						c = c >> 1;
				}
				table[i] = c;
			}
			return table;
		}
	}

	/// <summary>
	/// Stub for AssetCollection
	/// </summary>
	public class AssetCollection
	{
		public List<AssetFile> assets { get; set; } = new List<AssetFile>();
		public static AssetCollection FromBytes(byte[] data) => new AssetCollection();

		// Required methods
		public AssetFile GetPortraitOverride() => assets.FirstOrDefault(a => a.type == AssetFile.AssetType.Portrait);
		public AssetFile GetPortrait() => assets.FirstOrDefault(a => a.type == AssetFile.AssetType.Portrait);
		public void Remove(AssetFile asset) => assets.Remove(asset);
		public void Add(AssetFile asset) => assets.Add(asset);

		// Overload taking params array
		public bool ContainsNoneOf(params AssetFile.AssetType[] types)
			=> !assets.Any(a => types.Contains(a.type));

		// Overload taking predicate (for lambda expressions)
		public bool ContainsNoneOf(Func<AssetFile, bool> predicate)
			=> !assets.Any(predicate);

		public AssetCollection Clone() => new AssetCollection { assets = new List<AssetFile>(assets) };
		public List<AssetFile> ToList() => new List<AssetFile>(assets);

		// Overload without return
		public void AddBackgroundFromPortrait(ImageRef portrait) { }

		// Overload with out parameter
		public bool AddBackgroundFromPortrait(out AssetFile background)
		{
			background = null;
			return false;
		}

		// LINQ support
		public IEnumerable<AssetFile> Where(Func<AssetFile, bool> predicate) => assets.Where(predicate);
		public AssetFile FirstOrDefault(Func<AssetFile, bool> predicate) => assets.FirstOrDefault(predicate);
		public IEnumerable<T> Select<T>(Func<AssetFile, T> selector) => assets.Select(selector);
	}

	/// <summary>
	/// Stub for AssetData
	/// </summary>
	public struct AssetData
	{
		public byte[] data { get; set; }
		public byte[] bytes { get => data; set => data = value; }
		public long length => data?.Length ?? 0;
		public bool isEmpty => data == null || data.Length == 0;

		public static AssetData FromBytes(byte[] data) => new AssetData { data = data };
		public static AssetData FromFile(string filename)
		{
			try
			{
				return new AssetData { data = System.IO.File.ReadAllBytes(filename) };
			}
			catch
			{
				return new AssetData();
			}
		}

		// Implicit conversion from byte[] to AssetData
		public static implicit operator AssetData(byte[] data) => new AssetData { data = data };
	}

	/// <summary>
	/// Stub for AssetFile
	/// </summary>
	public class AssetFile : IXmlLoadable, IXmlSaveable
	{
		public enum AssetType { Undefined, Unknown, Portrait, Background, Emotion, Other, Icon, Expression, UserIcon }

		public string id { get; set; }
		public string uid { get; set; } = Guid.NewGuid().ToString();
		public string name { get; set; }
		public AssetType type { get; set; }
		public AssetType assetType { get => type; set => type = value; }
		public AssetData data { get; set; }
		public string ext { get; set; }
		public string uri { get; set; }
		public bool isEmbeddedAsset { get; set; }

		// Additional properties for compatibility
		public bool isMainPortraitOverride { get; set; }
		public int actorIndex { get; set; } = -1;
		public int knownWidth { get; set; }
		public int knownHeight { get; set; }

		public bool LoadFromXml(System.Xml.XmlNode xmlNode)
		{
			uid = xmlNode.GetAttribute("uid", uid);
			name = xmlNode.GetAttribute("name", null);
			type = xmlNode.GetAttributeEnum("type", AssetType.Undefined);
			ext = xmlNode.GetAttribute("ext", null);
			uri = xmlNode.GetValueElement("URI", null);
			isEmbeddedAsset = xmlNode.GetAttributeBool("embedded", false);
			actorIndex = xmlNode.GetAttributeInt("actor", -1);
			knownWidth = xmlNode.GetAttributeInt("width", 0);
			knownHeight = xmlNode.GetAttributeInt("height", 0);
			isMainPortraitOverride = xmlNode.GetAttributeBool("main-portrait", false);
			return true;
		}

		public void SaveToXml(System.Xml.XmlNode xmlNode)
		{
			xmlNode.AddAttribute("uid", uid);
			if (!string.IsNullOrEmpty(name))
				xmlNode.AddAttribute("name", name);
			if (type != AssetType.Undefined)
				xmlNode.AddAttribute("type", EnumHelper.ToString(type));
			if (!string.IsNullOrEmpty(ext))
				xmlNode.AddAttribute("ext", ext);
			if (!string.IsNullOrEmpty(uri))
				xmlNode.AddValueElement("URI", uri);
			if (isEmbeddedAsset)
				xmlNode.AddAttribute("embedded", true);
			if (actorIndex >= 0)
				xmlNode.AddAttribute("actor", actorIndex);
			if (knownWidth > 0)
				xmlNode.AddAttribute("width", knownWidth);
			if (knownHeight > 0)
				xmlNode.AddAttribute("height", knownHeight);
			if (isMainPortraitOverride)
				xmlNode.AddAttribute("main-portrait", true);
		}
	}

	/// <summary>
	/// Stub for UserData
	/// </summary>
	public class UserData
	{
		public string name { get; set; }
		public string persona { get; set; }

		public string ToJson()
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(this);
		}

		public static bool Validate(string json)
		{
			if (string.IsNullOrEmpty(json))
				return false;
			try
			{
				var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
				return obj["name"] != null || obj["persona"] != null;
			}
			catch { return false; }
		}

		public static UserData FromJson(string json)
		{
			try
			{
				return Newtonsoft.Json.JsonConvert.DeserializeObject<UserData>(json);
			}
			catch { return null; }
		}
	}

	/// <summary>
	/// Stub for ChatHistory
	/// </summary>
	public class ChatHistory
	{
		public DateTime createdAt { get; set; }
		public DateTime lastMessageTime { get; set; }
		public List<Integration.Backyard.CharacterMessage> messages { get; set; } = new List<Integration.Backyard.CharacterMessage>();
		public int numSpeakers { get; set; }
	}

	/// <summary>
	/// Stub for BackupData
	/// </summary>
	public class BackupData
	{
		public class Chat
		{
			public string id { get; set; }
			public string name { get; set; }
			public string backgroundName { get; set; }
			public DateTime creationDate { get; set; }
			public DateTime updateDate { get; set; }
			public Integration.Backyard.ChatParameters parameters { get; set; }
			public Integration.ChatHistory history { get; set; }
			public Integration.Backyard.ChatStaging staging { get; set; }
		}
	}

	/// <summary>
	/// Stub for FaradayCardV1
	/// </summary>
	public class FaradayCardV1
	{
		public Data data = new Data();
		public int version = 1;

		public class Data
		{
			public string id { get; set; }
			public string displayName { get; set; }
			public string name { get; set; }
			public string persona { get; set; }
			public string scenario { get; set; }
			public string greeting { get; set; }
			public string example { get; set; }
			public string system { get; set; }
			public string grammar { get; set; }
			public bool isNSFW { get; set; }
			public string creationDate { get; set; }
			public string updateDate { get; set; }
			public LoreBookEntry[] loreItems { get; set; } = new LoreBookEntry[0];
		}

		public class LoreBookEntry
		{
			public string id { get; set; } = Guid.NewGuid().ToString();
			public string key { get; set; }
			public string value { get; set; }
		}

		public class Chat
		{
			public string id { get; set; }
		}
	}

	/// <summary>
	/// Stub for FaradayCardV2
	/// </summary>
	public class FaradayCardV2
	{
		public FaradayCardV1.Data data = new FaradayCardV1.Data();
		public int version = 2;
	}

	/// <summary>
	/// Stub for FaradayCardV3
	/// </summary>
	public class FaradayCardV3
	{
		public FaradayCardV1.Data data = new FaradayCardV1.Data();
		public int version = 3;
	}

	/// <summary>
	/// Stub for FaradayCardV4
	/// </summary>
	public class FaradayCardV4
	{
		public Data data = new Data();
		public int version = 4;

		// Transient values
		public string creator { get; set; }
		public string hubCharacterId { get; set; }
		public string hubAuthorUsername { get; set; }
		public string authorNote { get; set; }
		public string userPersona { get; set; }

		public static readonly string[] OriginalModelInstructionsByFormat = new string[8]
		{
			// None
			"Text transcript of a never-ending conversation between {user} and {character}. In the transcript, gestures and other non-verbal actions are written between asterisks (for example, *waves hello* or *moves closer*).",
			// Asterisks
			"Text transcript of a never-ending conversation between {user} and {character}. In the transcript, gestures and other non-verbal actions are written between asterisks (for example, *waves hello* or *moves closer*).",
			// Quotes
			"Text transcript of a never-ending conversation between {user} and {character}.",
			// Quotes + Asterisks
			"Text transcript of a never-ending conversation between {user} and {character}. In the transcript, gestures and other non-verbal actions are written between asterisks (for example, *waves hello* or *moves closer*).",
			// Decorative quotes
			"Text transcript of a never-ending conversation between {user} and {character}.",
			// Bold
			"Text transcript of a never-ending conversation between {user} and {character}. In the transcript, gestures and other non-verbal actions are written between asterisks (for example, **waves hello** or **moves closer**).",
			// Parentheses
			"Text transcript of a never-ending conversation between {user} and {character}. In the transcript, gestures and other non-verbal actions are written between parentheses, for example (waves hello) or (moves closer).",
			// Japanese
			"Text transcript of a never-ending conversation between {user} and {character}.",
		};

		public class Data
		{
			public string id { get; set; }
			public string displayName { get; set; }
			public string name { get; set; }
			public string persona { get; set; }
			public string scenario { get; set; }
			public string greeting { get; set; }
			public string example { get; set; }
			public string system { get; set; }
			public string grammar { get; set; }
			public bool isNSFW { get; set; }
			public string creationDate { get; set; }
			public string updateDate { get; set; }
			public FaradayCardV1.LoreBookEntry[] loreItems { get; set; } = new FaradayCardV1.LoreBookEntry[0];
			public string promptTemplate { get; set; }
		}

		public string ToJson()
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
		}

		public static FaradayCardV4 FromJson(string json)
		{
			if (string.IsNullOrEmpty(json))
				return null;
			try
			{
				return Newtonsoft.Json.JsonConvert.DeserializeObject<FaradayCardV4>(json);
			}
			catch { return null; }
		}

		public static bool Validate(string json)
		{
			if (string.IsNullOrEmpty(json))
				return false;
			try
			{
				var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
				return obj["data"] != null && obj["version"] != null;
			}
			catch { return false; }
		}
	}

	// TavernCardV1, V2, V3 are defined in Models/TavernCardV2.cs
	// Do NOT duplicate them here - they're different from FaradayCardV1-V4

	// BackyardLinkCard is defined in BackyardLinkCard.cs - do NOT duplicate here
	/// <summary>
	/// Alias for JsonExtensionData (was GingerJsonExtensionData in original)
	/// </summary>
	public class GingerJsonExtensionData : System.Collections.Generic.Dictionary<string, object> { }

	/// <summary>
	/// Stub for DefaultPortrait
	/// </summary>
	public static class DefaultPortrait
	{
		private static byte[] _defaultBytes;

		public static byte[] Image => null;

		public static byte[] GetBytes()
		{
			// Return empty 1x1 transparent PNG if no default portrait
			if (_defaultBytes != null)
				return _defaultBytes;

			// Create a minimal 1x1 transparent PNG
			using var bitmap = new SkiaSharp.SKBitmap(1, 1);
			bitmap.Erase(SkiaSharp.SKColors.Transparent);
			using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
			using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
			_defaultBytes = data.ToArray();
			return _defaultBytes;
		}
	}
}
