// Stub implementations for Backyard integration
// These provide minimal interfaces to allow compilation
// Full implementations will be added as needed

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Ginger
{
	/// <summary>
	/// Stub for ImageRef - originally uses System.Drawing which isn't cross-platform.
	/// </summary>
	public class ImageRef
	{
		public byte[] data { get; set; }
		public int width { get; set; }
		public int height { get; set; }
		public string filename { get; set; }
		public string uid { get; set; } = Guid.NewGuid().ToString();

		// Capitalized versions for compatibility with original code
		public int Width { get => width; set => width = value; }
		public int Height { get => height; set => height = value; }

		public static ImageRef FromImage(byte[] data) => new ImageRef { data = data };
		public static ImageRef FromBytes(byte[] data) => new ImageRef { data = data };

		// Implicit conversion from byte[] to ImageRef
		public static implicit operator ImageRef(byte[] data) => new ImageRef { data = data };

		public bool isEmpty => data == null || data.Length == 0;
		public int length => data?.Length ?? 0;
	}

	/// <summary>
	/// Stub for FileUtil
	/// </summary>
	public static partial class FileUtil
	{
		public enum FileType { Unknown, Png, Jpeg, Gif, Webp }
		public enum Error { NoError, FileNotFound, InvalidFormat, IOError }

		public static byte[] CropPortrait(string filename, int width, int height) => null;
		public static byte[] CropPortrait(byte[] data, int width, int height) => data;
		public static Error WriteToFile(string filename, byte[] data) => Error.NoError;
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
		public static byte[] Image => null;
	}
}
