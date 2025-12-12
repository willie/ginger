// Lorebook class for character world information
// Simplified version for Avalonia port - full version includes many card format converters

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Text;

namespace Ginger
{
	public class Lorebook
	{
		public string name = "";
		public string description = "";
		public List<Entry> entries = new List<Entry>();
		public string filename;
		public UnusedProperties unused = null;
		public string[] paths;

		public enum Sorting
		{
			Default		= 0,	// By internal order
			ByIndex,			// By addition order
			ByKey,				// Alphabetical
			ByOrder,			// By sort order
		}

		public bool isEmpty
		{
			get
			{
				return entries.Count == 0
					|| entries.ContainsNoneOf(e => e.isEnabled && e.keys.Length > 0 && string.IsNullOrWhiteSpace(e.value) == false);
			}
		}

		public class UnusedProperties : IXmlLoadable, IXmlSaveable
		{
			public int scan_depth = 50;
			public int token_budget = 500;
			public bool recursive_scanning = false;
			public JsonExtensionData extensions = null;

			public bool LoadFromXml(XmlNode xmlNode)
			{
				scan_depth = xmlNode.GetAttributeInt("scan-depth", scan_depth);
				token_budget = xmlNode.GetAttributeInt("token-budget", token_budget);
				recursive_scanning = xmlNode.GetAttributeBool("recursive-scanning", recursive_scanning);
				return true;
			}

			public void SaveToXml(XmlNode xmlNode)
			{
				if (scan_depth != 50)
					xmlNode.AddAttribute("scan-depth", scan_depth);
				if (token_budget != 500)
					xmlNode.AddAttribute("token-budget", token_budget);
				if (recursive_scanning)
					xmlNode.AddAttribute("recursive-scanning", true);
			}
		}

		public class Entry
		{
			public static readonly int DefaultSortOrder = 50;

			public string key
			{
				get { return _key; }
				set
				{
					_key = (value ?? "").Trim();
					_keys = Utility.ListFromCommaSeparatedString(_key).ToArray();
				}
			}
			public string[] keys
			{
				get { return _keys; }
				set
				{
					_keys = value;
					_key = Utility.ListToCommaSeparatedString(_keys);
				}
			}

			public string value
			{
				get { return _value; }
				set
				{
					_value = value;
				}
			}

			private string _value;
			private string _key;
			private string[] _keys = new string[0];
			public bool isEnabled = true;
			public int addition_index;
			public int sortOrder = DefaultSortOrder;

			public int tokenCount = 0;
			private string _uniqueID = null;
			public UnusedProperties unused = null;

			public bool isEmpty { get { return keys.Length == 0 && string.IsNullOrWhiteSpace(value); } }

			public class UnusedProperties : IXmlLoadable, IXmlSaveable
			{
				public string name = "";
				public string comment = ""; // Alt name
				public string[] secondary_keys = new string[0];
				public int weight = 0;		// Agnai
				public int priority = 0;	// Agnai
				public bool enabled = true;
				public bool case_sensitive = false;
				public bool selective = false;
				public bool constant = false;
				public bool use_regex = false; // v3
				public string placement = "before_char"; // position: 'before_char' | 'after_char'

				// Tavern world book
				public int selectiveLogic = 0;
				public int position = 0; // 0: before_char 1: after_char 2: before_authors_note 3:after_authors note 4:at_depth
				public int depth = 4;
				public string group = "";
				public int probability = 100;
				public bool useProbability = true;
				public bool excludeRecursion = false;
				public bool addMemo = true;

				public JsonExtensionData extensions = null;

				public bool LoadFromXml(XmlNode xmlNode)
				{
					name = xmlNode.GetAttribute("name", name);
					comment = xmlNode.GetAttribute("comment", comment);
					addMemo = xmlNode.GetAttributeBool("add-memo", addMemo);
					case_sensitive = xmlNode.GetAttributeBool("case-sensitive", case_sensitive);
					constant = xmlNode.GetAttributeBool("constant", constant);
					depth = xmlNode.GetAttributeInt("depth", depth);
					excludeRecursion = xmlNode.GetAttributeBool("exclude-recursion", excludeRecursion);
					enabled = xmlNode.GetAttributeBool("enabled", enabled);
					group = xmlNode.GetValueElement("group", group);
					weight = xmlNode.GetAttributeInt("weight", weight);
					placement = xmlNode.GetAttribute("placement", placement);
					position = xmlNode.GetAttributeInt("position", position);
					priority = xmlNode.GetAttributeInt("priority", priority);
					probability = xmlNode.GetAttributeInt("probability", probability);
					selective = xmlNode.GetAttributeBool("selective", selective);
					selectiveLogic = xmlNode.GetAttributeInt("selective-logic", selectiveLogic);
					useProbability = xmlNode.GetAttributeBool("use-probability", useProbability);
					use_regex = xmlNode.GetAttributeBool("use-regex", use_regex);
					string secondaryKeys = xmlNode.GetAttribute("secondary-keys", null);
					if (secondaryKeys != null)
						secondary_keys = Utility.ListFromCommaSeparatedString(secondaryKeys).ToArray();
					return true;
				}

				public void SaveToXml(XmlNode xmlNode)
				{
					if (string.IsNullOrEmpty(name) == false)
						xmlNode.AddAttribute("name", name);
					if (string.IsNullOrEmpty(comment) == false)
						xmlNode.AddAttribute("comment", comment);
					if (string.IsNullOrEmpty(placement) == false)
						xmlNode.AddAttribute("placement", placement);
					if (addMemo)
						xmlNode.AddAttribute("add-memo", true);
					if (case_sensitive)
						xmlNode.AddAttribute("case-sensitive", true);
					if (constant)
						xmlNode.AddAttribute("constant", true);
					if (depth != 4)
						xmlNode.AddAttribute("depth", depth);
					if (excludeRecursion)
						xmlNode.AddAttribute("exclude-recursion", true);
					if (!enabled)
						xmlNode.AddAttribute("enabled", false);
					if (string.IsNullOrEmpty(group) == false)
						xmlNode.AddAttribute("group", group);
					if (weight != 0)
						xmlNode.AddAttribute("weight", weight);
					if (position != 0)
						xmlNode.AddAttribute("position", position);
					if (priority != 0)
						xmlNode.AddAttribute("priority", priority);
					if (probability != 100)
						xmlNode.AddAttribute("probability", probability);
					if (secondary_keys != null && secondary_keys.Length > 0)
						xmlNode.AddAttribute("secondary-keys", Utility.ListToCommaSeparatedString(secondary_keys));
					if (selective)
						xmlNode.AddAttribute("selective", true);
					if (selectiveLogic != 0)
						xmlNode.AddAttribute("selective-logic", selectiveLogic);
					if (!useProbability)
						xmlNode.AddAttribute("use-probability", false);
					if (use_regex)
						xmlNode.AddAttribute("use-regex", true);
				}
			}

			public string GetUID()
			{
				if (_uniqueID == null)
					_uniqueID = Cuid.NewCuid();
				return _uniqueID;
			}

			public Entry Clone()
			{
				return new Entry() {
					key = this.key,
					value = this.value,
					isEnabled = this.isEnabled,
					sortOrder = this.sortOrder,
					addition_index = this.addition_index,
					unused = this.unused,
					_uniqueID = GetUID(),
				};
			}

			public override int GetHashCode()
			{
				return Utility.MakeHashCode(_key, _value);
			}
		}

		public int GetNextIndex()
		{
			if (entries.Count == 0)
				return 0;
			return Math.Max(entries.Max(a => a.addition_index) + 1, 0);
		}

		public Lorebook()
		{
		}

		public Lorebook Clone()
		{
			var clone = (Lorebook)this.MemberwiseClone();
			clone.entries = new List<Entry>(this.entries.Count);
			for (int i = 0; i < this.entries.Count; ++i)
				clone.entries.Add(this.entries[i].Clone());

			return clone;
		}

		public bool LoadFromXml(XmlNode xmlNode, string characterName, string userName)
		{
			name = xmlNode.GetValueElement("Name");
			description = xmlNode.GetValueElement("Description");

			entries.Clear();
			var entriesNode = xmlNode.GetFirstElement("Entries");
			if (entriesNode != null)
			{
				var entryNode = entriesNode.GetFirstElement("Entry");
				while (entryNode != null)
				{
					string key = entryNode.GetValueElement("Name");
					string value = entryNode.GetValueElement("Value");
					bool isEnabled = entryNode.GetAttributeBool("enabled", true);
					int order = entryNode.GetAttributeInt("order", Entry.DefaultSortOrder);
					int addition_index = entryNode.GetAttributeInt("index", -1);

					var entry = new Entry() {
						key = key,
						value = Parameter.FromClipboard(value, characterName, userName),
						sortOrder = order,
						addition_index = addition_index,
						isEnabled = isEnabled,
					};

					// Unused
					var propertiesNode = entryNode.GetFirstElement("Properties");
					if (propertiesNode != null)
					{
						entry.unused = new Entry.UnusedProperties();
						entry.unused.LoadFromXml(propertiesNode);
					}

					entries.Add(entry);

					entryNode = entryNode.GetNextSibling();
				}

				for (int i = 0; i < entries.Count; ++i)
				{
					if (entries[i].addition_index < 0)
						entries[i].addition_index = GetNextIndex();
				}

				if (entries.ContainsNoneOf(e => e.sortOrder != -1))
					SortEntries(Sorting.Default, true); // Assign order indices
			}

			// Unused
			var unusedNode = xmlNode.GetFirstElement("Properties");
			if (unusedNode != null)
			{
				unused = new UnusedProperties();
				unused.LoadFromXml(unusedNode);
			}
			else
				unused = null;
			return true;
		}

		public void SaveToXml(XmlNode xmlNode)
		{
			if (string.IsNullOrEmpty(name) == false)
				xmlNode.AddValueElement("Name", name);
			if (string.IsNullOrEmpty(description) == false)
				xmlNode.AddValueElement("Description", description);

			var entriesNode = xmlNode.AddElement("Entries");
			foreach (var entry in entries)
			{
				var entryNode = entriesNode.AddElement("Entry");
				entryNode.AddValueElement("Name", entry.key);
				entryNode.AddValueElement("Value", Parameter.ToClipboard(entry.value));
				if (entry.isEnabled == false)
					entryNode.AddAttribute("enabled", false);
				entryNode.AddAttribute("order", entry.sortOrder);
				entryNode.AddAttribute("index", entry.addition_index);

				if (entry.unused != null)
				{
					var propertiesNode = entryNode.AddElement("Properties");
					entry.unused.SaveToXml(propertiesNode);
				}
			}

			// Unused
			if (unused != null)
			{
				var unusedNode = xmlNode.AddElement("Properties");
				unused.SaveToXml(unusedNode);
			}
		}

		public void Reindex(bool bSort)
		{
			if (bSort)
				entries = entries.OrderBy(e => e.sortOrder).ToList();
			for (int i = 0; i < entries.Count; ++i)
				entries[i].addition_index = i;
		}

		public override int GetHashCode()
		{
			int hash = 0x2324A697;
			hash ^= Utility.MakeHashCode(name, description);
			hash ^= Utility.MakeHashCode(entries, Utility.HashOption.None);
			return hash;
		}

		public static Lorebook Merge(List<Lorebook> lorebooks)
		{
			if (lorebooks == null || lorebooks.Count == 0)
				return null;

			if (lorebooks.Count == 1)
				return lorebooks[0];

			var lorebook = new Lorebook();
			var usedKeys = new HashSet<string>();

			for (int i = 0; i < lorebooks.Count; ++i)
			{
				foreach (var entry in lorebooks[i].entries)
				{
					if (usedKeys.Contains(entry.key))
						continue; // Duplicate key

					usedKeys.Add(entry.key);
					lorebook.entries.Add(entry.Clone());
				}
			}
			for (int i = 0; i < lorebook.entries.Count; ++i)
				lorebook.entries[i].addition_index = i;

			return lorebook;
		}

		public bool CompareTo(Lorebook other)
		{
			if (string.Compare(name, other.name, StringComparison.Ordinal) != 0)
				return false;
			if (string.Compare(description, other.description, StringComparison.Ordinal) != 0)
				return false;
			if (entries.Count != other.entries.Count)
				return false;
			for (int i = 0; i < entries.Count; ++i)
			{
				if (entries[i].GetUID() != other.entries[i].GetUID())
					return false;
				if (entries[i].keys.Length != other.entries[i].keys.Length)
					return false;
				if (entries[i].keys.Compare(other.entries[i].keys) == false)
					return false;
				if (string.Compare(entries[i].value, other.entries[i].value, StringComparison.Ordinal) != 0)
					return false;
			}

			return true;
		}

		public void StripDecorators()
		{
			foreach (var entry in entries)
			{
				int pos_decorator = entry.value.IndexOf("@@", 0);
				if (pos_decorator == -1)
					continue;

				var sb = new StringBuilder(entry.value);
				while (pos_decorator != -1)
				{
					if (pos_decorator > 0 && sb[pos_decorator - 1] != '\n')
					{
						pos_decorator = sb.IndexOf("@@", pos_decorator + 2);
						continue;
					}
					int pos_end = sb.IndexOf('\n', pos_decorator + 2);
					if (pos_end == -1)
						pos_end = sb.Length - 1;
					sb.RemoveFromTo(pos_decorator, pos_end);
					pos_decorator = sb.IndexOf("@@", pos_decorator);
				}

				entry.value = sb.ToString();
			}
		}

		public void SortEntries(Sorting sorting, bool bResetOrder)
		{
			switch (sorting)
			{
			case Sorting.ByIndex:
				entries = entries.OrderBy(a => a.addition_index).ToList();
				break;
			case Sorting.ByKey:
				entries = entries.OrderBy(a => a.key).ToList();
				break;
			case Sorting.ByOrder:
				entries = entries.OrderBy(a => a.sortOrder).ToList();
				break;
			default:
				break;
			}

			if (bResetOrder)
			{
				for (int i = 0; i < entries.Count; ++i)
					entries[i].sortOrder = i + 1;
			}
		}
	}
}
