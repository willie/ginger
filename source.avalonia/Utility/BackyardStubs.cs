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

		public static ImageRef FromImage(byte[] data) => new ImageRef { data = data };
		public static ImageRef FromBytes(byte[] data) => new ImageRef { data = data };

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
		public bool ContainsNoneOf(params AssetFile.AssetType[] types)
			=> !assets.Any(a => types.Contains(a.type));
		public AssetCollection Clone() => new AssetCollection { assets = new List<AssetFile>(assets) };
		public void AddBackgroundFromPortrait(ImageRef portrait) { }

		// LINQ support
		public IEnumerable<AssetFile> Where(Func<AssetFile, bool> predicate) => assets.Where(predicate);
		public AssetFile FirstOrDefault(Func<AssetFile, bool> predicate) => assets.FirstOrDefault(predicate);
		public IEnumerable<T> Select<T>(Func<AssetFile, T> selector) => assets.Select(selector);
	}

	/// <summary>
	/// Stub for AssetData
	/// </summary>
	public class AssetData
	{
		public string id { get; set; }
		public byte[] data { get; set; }
		public byte[] bytes { get => data; set => data = value; }
		public bool isEmpty => data == null || data.Length == 0;

		public static AssetData FromBytes(byte[] data) => new AssetData { data = data };
	}

	/// <summary>
	/// Stub for AssetFile
	/// </summary>
	public class AssetFile
	{
		public enum AssetType { Undefined, Unknown, Portrait, Background, Emotion, Other, Icon, Expression, UserIcon }

		public string id { get; set; }
		public string uid { get; set; } = Guid.NewGuid().ToString();
		public string name { get; set; }
		public AssetType type { get; set; }
		public AssetType assetType { get => type; set => type = value; }
		public byte[] data { get; set; }
		public string ext { get; set; }
		public string uri { get; set; }
		public bool isEmbeddedAsset { get; set; }
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
	/// Stub for CharacterMessage
	/// </summary>
	public class CharacterMessage
	{
		public string id { get; set; }
		public string characterId { get; set; }
		public string text { get; set; }
		public DateTime timestamp { get; set; }
	}

	/// <summary>
	/// Stub for ChatStaging
	/// </summary>
	public class ChatStaging
	{
		public string id { get; set; }
		public string text { get; set; }
	}

	/// <summary>
	/// Stub for ChatHistory
	/// </summary>
	public class ChatHistory
	{
		public DateTime createdAt { get; set; }
		public DateTime lastMessageTime { get; set; }
		public List<CharacterMessage> messages { get; set; } = new List<CharacterMessage>();
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
			public ChatHistory history { get; set; }
			public ChatStaging staging { get; set; }
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
}

namespace Ginger.Integration
{
	/// <summary>
	/// Stub for BackyardLinkCard - used by Backyard integration (excluded from build for now)
	/// </summary>
	public class BackyardLinkCard
	{
		public Data data = new Data();
		public string creator { get; set; }
		public string hubCharacterId { get; set; }
		public string hubAuthorUsername { get; set; }
		public string authorNote { get; set; }
		public string userPersona { get; set; }

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
		}

		public static BackyardLinkCard FromFaradayCard(FaradayCardV4 card) => new BackyardLinkCard();
		public FaradayCardV4 ToFaradayCard() => new FaradayCardV4();
	}
}

namespace Ginger
{
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
