using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ginger.Models;

namespace Ginger.Services
{
	/// <summary>
	/// Service for lorebook import/export operations.
	/// </summary>
	public class LorebookService
	{
		private static readonly Encoding UTF8WithoutBOM = new UTF8Encoding(false);

		/// <summary>
		/// Export lorebook to Tavern V2 (World Book) format.
		/// </summary>
		public static bool ExportTavernV2Lorebook(Lorebook lorebook, string filename)
		{
			var tavernBook = new TavernWorldBook();
			if (string.IsNullOrWhiteSpace(lorebook.name) == false)
				tavernBook.name = lorebook.name.Trim();
			else if (string.IsNullOrWhiteSpace(Current.Card.name) == false)
				tavernBook.name = Current.Card.name;
			else if (string.IsNullOrWhiteSpace(Current.Character.name) == false)
				tavernBook.name = Current.Character.name;
			else
				tavernBook.name = Path.GetFileNameWithoutExtension(filename);

			tavernBook.description = lorebook.description;
			if (lorebook.unused != null)
			{
				tavernBook.recursive_scanning = lorebook.unused.recursive_scanning;
				tavernBook.scan_depth = lorebook.unused.scan_depth;
				tavernBook.token_budget = lorebook.unused.token_budget;
				if (lorebook.unused.extensions != null)
					tavernBook.extensions = lorebook.unused.extensions.WithGingerVersion();
				else
					tavernBook.extensions = new JsonExtensionData();
			}

			int index = 0;
			foreach (var loreEntry in lorebook.entries)
			{
				if (loreEntry.keys == null || loreEntry.keys.Length == 0)
					continue;

				int entryId = index + 1;
				var tavernBookEntry = new TavernWorldBook.Entry()
				{
					uid = entryId,
					displayIndex = index,
					comment = loreEntry.keys[0] ?? "",
					key = loreEntry.keys,
					content = GingerString.FromString(loreEntry.value).ToTavern(),
					order = loreEntry.sortOrder,
				};

				if (loreEntry.unused != null)
				{
					tavernBookEntry.comment = loreEntry.unused.comment;
					tavernBookEntry.constant = loreEntry.unused.constant;
					tavernBookEntry.disable = !loreEntry.unused.enabled;
					tavernBookEntry.comment = loreEntry.unused.name;
					tavernBookEntry.position = loreEntry.unused.position;
					tavernBookEntry.secondary_keys = loreEntry.unused.secondary_keys;
					tavernBookEntry.addMemo = loreEntry.unused.addMemo;
					tavernBookEntry.depth = loreEntry.unused.depth;
					tavernBookEntry.excludeRecursion = loreEntry.unused.excludeRecursion;
					tavernBookEntry.selective = loreEntry.unused.selective;
					tavernBookEntry.selectiveLogic = loreEntry.unused.selectiveLogic;
					tavernBookEntry.useProbability = loreEntry.unused.useProbability;
					tavernBookEntry.probability = loreEntry.unused.probability;
					tavernBookEntry.group = loreEntry.unused.group;
					tavernBookEntry.extensions = loreEntry.unused.extensions ?? new JsonExtensionData();
				}

				tavernBook.entries.TryAdd(entryId.ToString(), tavernBookEntry);

				index++;
			}

			try
			{
				string json = Newtonsoft.Json.JsonConvert.SerializeObject(tavernBook, Newtonsoft.Json.Formatting.Indented);
				if (json != null)
				{
					File.WriteAllText(filename, json, UTF8WithoutBOM);
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		/// <summary>
		/// Export lorebook to Tavern V3 (lorebook_v3) format.
		/// </summary>
		public static bool ExportTavernV3Lorebook(Lorebook lorebook, string filename)
		{
			var lorebookV3 = new TavernLorebookV3()
			{
				spec = "lorebook_v3",
				data = new TavernCardV3.CharacterBook(),
			};
			var data = lorebookV3.data;

			if (string.IsNullOrWhiteSpace(lorebook.name) == false)
				data.name = lorebook.name.Trim();
			else if (string.IsNullOrWhiteSpace(Current.Card.name) == false)
				data.name = Current.Card.name;
			else if (string.IsNullOrWhiteSpace(Current.Character.name) == false)
				data.name = Current.Character.name;
			else
				data.name = Path.GetFileNameWithoutExtension(filename);
			data.description = lorebook.description;

			if (lorebook.unused != null)
			{
				data.recursive_scanning = lorebook.unused.recursive_scanning;
				data.scan_depth = lorebook.unused.scan_depth;
				data.token_budget = lorebook.unused.token_budget;
				if (lorebook.unused.extensions != null)
					data.extensions = lorebook.unused.extensions.WithGingerVersion();
				else
					data.extensions = new JsonExtensionData();
			}
			else
				data.extensions = new JsonExtensionData();

			int index = 0;
			var entries = new List<TavernCardV3.CharacterBook.Entry>();
			foreach (var loreEntry in lorebook.entries)
			{
				if (loreEntry.keys == null || loreEntry.keys.Length == 0)
					continue;

				int entryId = index + 1;
				var copy = new TavernCardV3.CharacterBook.Entry()
				{
					id = entryId,
					keys = loreEntry.keys,
					name = loreEntry.key,
					content = GingerString.FromString(loreEntry.value).ToTavern(),
					insertion_order = loreEntry.sortOrder,
					use_regex = false,
				};

				if (loreEntry.unused != null)
				{
					copy.name = loreEntry.unused.name;
					copy.comment = loreEntry.unused.comment;
					copy.constant = loreEntry.unused.constant;
					copy.enabled = loreEntry.unused.enabled;
					copy.position = loreEntry.unused.placement;
					copy.secondary_keys = loreEntry.unused.secondary_keys;
					copy.priority = loreEntry.unused.priority;
					copy.selective = loreEntry.unused.selective;
					copy.case_sensitive = loreEntry.unused.case_sensitive;
					copy.extensions = loreEntry.unused.extensions ?? new JsonExtensionData();
				}

				entries.Add(copy);
				index++;
			}
			data.entries = entries.ToArray();

			try
			{
				string json = Newtonsoft.Json.JsonConvert.SerializeObject(lorebookV3, Newtonsoft.Json.Formatting.Indented);
				if (json != null)
				{
					File.WriteAllText(filename, json, UTF8WithoutBOM);
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		/// <summary>
		/// Export lorebook to Agnaistic format.
		/// </summary>
		public static bool ExportAgnaisticLorebook(Lorebook lorebook, string filename)
		{
			var agnaiBook = new AgnaisticCard.CharacterBook();
			if (string.IsNullOrWhiteSpace(lorebook.name) == false)
				agnaiBook.name = lorebook.name.Trim();
			else if (string.IsNullOrWhiteSpace(Current.Card.name) == false)
				agnaiBook.name = Current.Card.name;
			else if (string.IsNullOrWhiteSpace(Current.Character.name) == false)
				agnaiBook.name = Current.Character.name;
			else
				agnaiBook.name = Path.GetFileNameWithoutExtension(filename);
			agnaiBook.description = lorebook.description;
			if (lorebook.unused != null)
			{
				agnaiBook.recursiveScanning = lorebook.unused.recursive_scanning;
				agnaiBook.scanDepth = lorebook.unused.scan_depth;
				agnaiBook.tokenBudget = lorebook.unused.token_budget;
			}

			var entries = new List<AgnaisticCard.CharacterBook.Entry>(lorebook.entries.Count);
			int id = 1;
			int minPriority = int.MaxValue;
			int maxPriority = int.MinValue;
			foreach (var loreEntry in lorebook.entries)
			{
				var agnaiBookEntry = new AgnaisticCard.CharacterBook.Entry()
				{
					id = id++,
					name = loreEntry.keys[0],
					keywords = loreEntry.keys,
					entry = loreEntry.value,
					priority = loreEntry.sortOrder, // Inverse
				};

				if (loreEntry.unused != null)
				{
					agnaiBookEntry.comment = loreEntry.unused.comment;
					agnaiBookEntry.constant = loreEntry.unused.constant;
					agnaiBookEntry.enabled = loreEntry.unused.enabled;
					agnaiBookEntry.weight = loreEntry.unused.weight;
					agnaiBookEntry.position = loreEntry.unused.placement;
					agnaiBookEntry.secondaryKeys = loreEntry.unused.secondary_keys;
					agnaiBookEntry.selective = loreEntry.unused.selective;
				}
				minPriority = Math.Min(loreEntry.sortOrder, minPriority);
				maxPriority = Math.Max(loreEntry.sortOrder, maxPriority);

				entries.Add(agnaiBookEntry);
			}

			// Sort order -> Priority (inverse)
			if (minPriority != maxPriority)
			{
				for (int i = 0; i < entries.Count; ++i)
				{
					var entry = entries[i];
					entries[i].priority = maxPriority - (entry.priority - minPriority) - minPriority;
				}
			}

			agnaiBook.entries = entries.ToArray();

			try
			{
				string json = Newtonsoft.Json.JsonConvert.SerializeObject(agnaiBook, Newtonsoft.Json.Formatting.Indented);
				if (json != null)
				{
					File.WriteAllText(filename, json, UTF8WithoutBOM);
					return true;
				}
			}
			catch
			{
			}

			return false;
		}

		/// <summary>
		/// Export lorebook to CSV format.
		/// </summary>
		public static bool ExportLorebookCsv(Lorebook lorebook, string filename)
		{
			if (lorebook == null || lorebook.entries.Count == 0)
				return false;

			try
			{
				var intermediateFilename = Path.GetTempFileName();

				// Write text file
				using (StreamWriter outputFile = new StreamWriter(new FileStream(intermediateFilename, FileMode.Open, FileAccess.Write), UTF8WithoutBOM))
				{
					outputFile.NewLine = "\r\n";

					StringBuilder sb = new StringBuilder();
					foreach (var entry in lorebook.entries)
					{
						if (entry.keys == null || entry.keys.Length == 0)
							continue;

						sb.Append("\"");
						for (int i = 0; i < entry.keys.Length; ++i)
						{
							if (i > 0)
								sb.Append(", ");
							sb.Append(entry.keys[i].Replace("\"", "\"\""));
						}
						sb.Append("\",\"");
						sb.Append(entry.value.Replace("\"", "\"\""));
						sb.AppendLine("\"");
					}
					sb.ConvertLinebreaks(Linebreak.CRLF);
					outputFile.Write(sb.ToString());
				}

				// Rename Temporary file to Target file
				if (File.Exists(filename))
					File.Delete(filename);
				File.Move(intermediateFilename, filename);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Import lorebook from file (auto-detects format).
		/// </summary>
		public static Lorebook ImportLorebook(string filename, out Lorebook.LoadError error, out int errors)
		{
			var lorebook = new Lorebook();
			string ext = Path.GetExtension(filename).ToLowerInvariant();

			if (ext == ".csv")
			{
				if (lorebook.LoadFromCsv(filename))
				{
					error = Lorebook.LoadError.NoError;
					errors = 0;
					return lorebook;
				}
				else
				{
					error = Lorebook.LoadError.FileError;
					errors = 0;
					return null;
				}
			}
			else // JSON or other
			{
				error = lorebook.LoadFromJson(filename, out errors);
				if (error == Lorebook.LoadError.NoError)
					return lorebook;
				return null;
			}
		}
	}
}
