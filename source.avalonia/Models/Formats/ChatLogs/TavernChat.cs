using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ginger.Models.Formats.ChatLogs
{
	public class TavernChat
	{
		public class Header
		{
			[JsonProperty("user_name", Required = Required.Always)]
			public string userName;
			[JsonProperty("character_name", Required = Required.Always)]
			public string characterName;
			[JsonProperty("create_date", Required = Required.Default)]
			public string creationDate;
		}

		public class Entry
		{
			[JsonProperty("name", Required = Required.Always)]
			public string name;
			[JsonProperty("is_user", Required = Required.Always)]
			public bool isUser;
			[JsonProperty("send_date", Required = Required.Default)]
			public string creationDate;
			[JsonProperty("mes", Required = Required.Default)]
			public string text;
			[JsonProperty("swipes", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
			public string[] swipes;
			[JsonProperty("swipe_id", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
			public int? swipeIndex;
		}

		public Header header = new Header();
		public Entry[] entries = Array.Empty<Entry>();

		public static TavernChat FromJson(string json)
		{
			try
			{
				string[] lines = json.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

				if (lines.Length == 0)
					return null;

				// Parse starter
				JObject jHeader = JObject.Parse(lines[0]);
				if (jHeader["user_name"] == null || jHeader["character_name"] == null)
					return null;

				DateTime createdAt;
				var header = JsonConvert.DeserializeObject<Header>(lines[0]);
				createdAt = DateTimeExtensions.FromTavernDate(header.creationDate);

				// Replace default "You" user name (causes problems during anonymization)
				if (string.Compare(header.userName, "You", StringComparison.OrdinalIgnoreCase) == 0)
					header.userName = Constants.DefaultUserName;

				List<Entry> lsEntries = new List<Entry>();
				for (int i = 1; i < lines.Length; ++i)
				{
					try
					{
						JObject jObject = JObject.Parse(lines[i]);
						if (jObject["name"] != null && jObject["is_user"] != null)
						{
							var entry = JsonConvert.DeserializeObject<Entry>(lines[i]);
							lsEntries.Add(entry);
						}
					}
					catch
					{
						// Skip invalid entries
					}
				}

				return new TavernChat() {
					header = header,
					entries = lsEntries.ToArray(),
				};
			}
			catch
			{
			}
			return null;
		}

		public string ToJson()
		{
			try
			{
				var sbJson = new StringBuilder();
				// Starter
				sbJson.AppendLine(JsonConvert.SerializeObject(header, new JsonSerializerSettings() {
						StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
					}));

				foreach (var entry in entries)
				{
					sbJson.AppendLine(JsonConvert.SerializeObject(entry, new JsonSerializerSettings() {
						StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
					}));
				}

				return sbJson.ToString().Replace("\r\n", "\n");
			}
			catch
			{
				return null;
			}
		}

		public static TavernChat FromChat(ChatHistory chatHistory, string[] names)
		{
			string userName = names != null && names.Length > 0 ? names[0] : Constants.DefaultUserName;
			string characterName = names != null && names.Length > 1 ? names[1] : Constants.DefaultCharacterName;

			if (chatHistory == null || chatHistory.count == 0)
			{
				return new TavernChat() {
					header = new Header() {
						userName = userName,
						characterName = characterName,
						creationDate = DateTimeExtensions.ToTavernDate(DateTime.UtcNow),
					},
					entries = Array.Empty<Entry>(),
				};
			}

			List<Entry> lsEntries = new List<Entry>();
			foreach (var message in chatHistory.messages) // Include greeting
			{
				var entry = new Entry() {
					isUser = message.speaker == 0,
					name = message.speaker == 0 ? userName : characterName,
					creationDate = DateTimeExtensions.ToTavernDate(message.creationDate),
					text = message.text,
				};
				if (message.swipes.Length > 1)
				{
					entry.swipes = message.swipes;
					entry.swipeIndex = message.activeSwipe;
				}
				lsEntries.Add(entry);
			}
			return new TavernChat() {
				header = new Header() {
					userName = userName,
					characterName = characterName,
					creationDate = DateTimeExtensions.ToTavernDate(DateTime.UtcNow),
				},
				entries = lsEntries.ToArray(),
			};
		}

		public ChatHistory ToChat()
		{
			var messages = new List<ChatHistory.Message>();
			foreach (var entry in entries)
			{
				DateTime messageTime = DateTimeExtensions.FromTavernDate(entry.creationDate);

				if (string.IsNullOrEmpty(entry.text) == false)
				{
					string text = entry.text;
					if (string.IsNullOrEmpty(header.characterName) == false)
						text = Utility.ReplaceWholeWord(text, header.characterName, GingerString.CharacterMarker, StringComparison.Ordinal);
					if (string.IsNullOrEmpty(header.userName) == false)
						text = Utility.ReplaceWholeWord(text, header.userName, GingerString.UserMarker, StringComparison.Ordinal);
					text = text.Replace("<START>", "");
					text = GingerString.FromTavern(text).ToString();

					Anonymize(ref text);

					ChatHistory.Message message;

					if (entry.swipes != null
						&& entry.swipes.Length > 1
						&& entry.swipeIndex != null)
					{
						message = new ChatHistory.Message() {
							speaker = entry.isUser ? 0 : 1,
							creationDate = messageTime,
							updateDate = messageTime,
							activeSwipe = Math.Min(Math.Max(entry.swipeIndex.Value, 0), entry.swipes.Length),
							swipes = entry.swipes,
						};
					}
					else
					{
						message = new ChatHistory.Message() {
							speaker = entry.isUser ? 0 : 1,
							creationDate = messageTime,
							updateDate = messageTime,
							activeSwipe = 0,
							swipes = new string[1] { text },
						};
					}

					messages.Add(message);
				}
			}

			return new ChatHistory() {
				messages = messages.ToArray(),
			};
		}

		private void Anonymize(ref string text)
		{
			if (header == null)
				return;

			StringBuilder sb = new StringBuilder(text);
			if (string.IsNullOrEmpty(header.userName) == false)
				Utility.ReplaceWholeWord(sb, header.userName, GingerString.UserMarker, StringComparison.Ordinal);
			if (string.IsNullOrEmpty(header.characterName) == false)
				Utility.ReplaceWholeWord(sb, header.characterName, GingerString.CharacterMarker, StringComparison.Ordinal);
			text = sb.ToString();
		}

		public static bool Validate(string jsonData)
		{
			try
			{
				string[] lines = jsonData.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

				if (lines.Length == 0)
					return false;

				// Parse starter
				JObject jObject = JObject.Parse(lines[0]);
				return jObject["user_name"] != null && jObject["character_name"] != null;
			}
			catch
			{
				return false;
			}
		}
	}
}
