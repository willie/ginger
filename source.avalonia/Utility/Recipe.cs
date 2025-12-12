// Recipe class - main implementation
// This is a partial class - enums are in RecipeEnums.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Avalonia.Media;

namespace Ginger
{
	public partial class Recipe : IXmlLoadable, IXmlSaveable, ICloneable
	{
		private static readonly string[] ComponentNames = new string[]
		{
			"System",
			"Persona",
			"User",
			"Scenario",
			"Example",
			"Grammar",
			"Greeting",
		};

		public class Template
		{
			[Flags]
			public enum Flags
			{
				None		= 0,
				Detached	= 1 << 0,
				Raw			= 1 << 1,
				Important	= 1 << 2,
				GroupOnly	= 1 << 3,
				PerActor	= 1 << 4,
			}

			public Component channel;
			public ICondition condition;
			public string text;
			public Flags flags;

			public bool isRaw { get { return flags.Contains(Flags.Raw); } }
			public bool isDetached { get { return flags.Contains(Flags.Detached); } }
			public bool isImportant { get { return flags.Contains(Flags.Important); } }
			public bool isGroupOnly { get { return flags.Contains(Flags.GroupOnly); } }
			public bool isPerActor { get { return flags.Contains(Flags.PerActor); } }

			public override int GetHashCode()
			{
				return Utility.MakeHashCode(
					channel,
					text,
					condition,
					flags);
			}
		}

		public class LoreItem
		{
			public string key;
			public string text;
			public ICondition condition;
			public int order = 50; // Lorebook.Entry.DefaultSortOrder

			public override int GetHashCode()
			{
				return Utility.MakeHashCode(
					key,
					text,
					condition,
					order);
			}
		}

		public Type type = Type.Recipe;

		public StringHandle id;		// Recipe name
		public int uid;				// Hash
		public int instanceIndex;	// Undo
		public StringHandle instanceID { get { return string.Format("{0}-{1:D4}", id.ToString(), instanceIndex); } }

		public VersionNumber version;
		public string filename;
		public string name;
		public string origName;
		private string _title;
		public string description;
		public string author;
		public string[] path;

		public Drawer drawer = Drawer.Default;
		public Category category = Category.Undefined;
		public string categoryTag = null;
		public List<IParameter> parameters = new List<IParameter>();
		public List<Block> blocks = new List<Block>();
		public List<Template> templates = new List<Template>();
		public List<LoreItem> loreItems = new List<LoreItem>();
		public List<CharacterAdjective> adjectives = new List<CharacterAdjective>();
		public List<CharacterNoun> nouns = new List<CharacterNoun>();
		public ICondition requires = null;
		public HashSet<StringHandle> flags = new HashSet<StringHandle>();
		public List<StringHandle> includes = new List<StringHandle>();
		public Color color = Constants.DefaultColor;
		public bool hasCustomColor;
		public StringBank strings = new StringBank();
		public AllowMultiple allowMultiple = AllowMultiple.No;
		public bool isBase { get { return flags.Contains(Constants.Flag.Base) || category == Category.Base; } }
		public bool isInternal { get { return flags.Contains(Constants.Flag.Internal); } }
		public bool isExternal { get { return filename == Constants.Flag.External; } }
		public bool isSnippet { get { return type == Type.Snippet; } }
		public bool isComponent { get { return flags.Contains(Constants.Flag.Component); } }
		public bool isLorebook { get { return isComponent && id == Constants.Flag.Lorebook; } }
		public bool isGreeting { get { return isComponent && id == Constants.Flag.Greeting; } }
		public bool isGrammar { get { return isComponent && id == Constants.Flag.Grammar; } }
		public bool isNSFW { get { return flags.Contains(Constants.Flag.NSFW); } }
		public bool canBake { get { return !isComponent && flags.Contains(Constants.Flag.DontBake) == false; } }
		public bool isHidden { get { return flags.Contains(Constants.Flag.Hidden); } }
		public bool canToggleTextFormatting { get { return flags.Contains(Constants.Flag.ToggleFormatting); } }
		public int? order = null;

		// State variables (Saved in instance)
		public bool isEnabled = true;
		public bool isCollapsed = false;
		public bool enableTextFormatting { get; private set; } // Components only
		public DetailLevel levelOfDetail = DetailLevel.Default;
		public bool enableNSFWContent = true;

		private static ICondition BaseExclusivityRule = Rule.Parse("not base");

		public string title
		{
			get { return string.IsNullOrWhiteSpace(_title) ? name : _title; }
			set { _title = value; }
		}

		public Recipe()
		{
			this.enableTextFormatting = true;
		}

		public Recipe(string filename)
		{
			this.filename = filename;
			this.enableTextFormatting = true;
		}

		public bool LoadFromXml(XmlNode xmlNode)
		{
			// Version
			int formatVersion = xmlNode.GetAttributeInt("format", 1);
			if (formatVersion > FormatVersion)
				return false; // Unsupported version

			// Path / Name / ID
			string strPath = origName = xmlNode.GetValueElement("Name", null).SingleLine();
			if (strPath == null)
				return false;
			strPath = strPath.Trim();
			if (strPath.Length == 0)
				return false;
			if (strPath.Length > 256)
				strPath = strPath.Substring(0, 256);

			// Path
			strPath = strPath.Replace("//", "%%SLASH%%");
			var lsPath = strPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.Select(s => s.Replace("%%SLASH%%", "/"))
				.Select(s => s.Length > MaxNameLength ? s.Substring(0, MaxNameLength) : s)
				.ToList();

			name = strPath = lsPath[lsPath.Count - 1];
			path = lsPath.Take(lsPath.Count - 1).ToArray();
			id = xmlNode.GetAttribute("id", name);

			// Tags
			string sFlags = xmlNode.GetValueElement("Flags");
			flags = new HashSet<StringHandle>(Utility.ListFromCommaSeparatedString(sFlags).Select(s => new StringHandle(s)));

			// Category
			var sCategory = xmlNode.GetValueElement("Category", null).SingleLine();
			Category eCategory = EnumHelper.FromString(sCategory, Category.Undefined);
			if (eCategory != Category.Undefined)
			{
				category = eCategory;
				categoryTag = EnumHelper.ToString(eCategory);
			}
			else
			{
				category = Category.Undefined;
				categoryTag = null;
			}

			if (isBase)
			{
				category = Category.Base;
				categoryTag = EnumHelper.ToString(Category.Base);
				flags.Add(Constants.Flag.Base);
			}
			else
			{
				categoryTag = sCategory;
			}

			// Drawer
			drawer = Constants.DrawerFromCategory.GetValueOrDefault(category, Drawer.Traits);
			var sDrawer = xmlNode.GetValueElement("Drawer", null);
			if (sDrawer != null) // Override
			{
				switch (sDrawer.ToLowerInvariant())
				{
				case "appearance":
					drawer = Drawer.Traits;
					break;
				default:
					drawer = EnumHelper.FromString(sDrawer, drawer);
					break;
				}
			}

			// Version
			version = VersionNumber.Parse(xmlNode.GetAttribute("version", null));

			// Label
			_title = xmlNode.GetValueElement("Title", null).SingleLine();
			if (string.IsNullOrWhiteSpace(_title))
				_title = strPath;

			// Description
			description = xmlNode.GetValueElement("Description");

			// Author
			author = xmlNode.GetValueElement("Author").SingleLine();

			// Multiple
			string multiple = xmlNode.GetValueElement("Multiple", "No");
			if (EnumInfo<AllowMultiple>.Convert(multiple, out this.allowMultiple) == false)
				allowMultiple = Utility.StringToBool(multiple) ? AllowMultiple.Yes : AllowMultiple.No;

			// Order?
			var orderNode = xmlNode.GetFirstElement("Order");
			if (orderNode != null)
			{
				order = orderNode.GetTextValueInt(int.MinValue);
				if (order.Value == int.MinValue)
					order = null;
			}

			// Parameters - simplified loading (full implementation needs IParameter factory)
			parameters.Clear();

			// Blocks
			blocks.Clear();
			var anyNode = xmlNode.GetFirstElementAny();
			while (anyNode != null)
			{
				if (anyNode.Name == "Node")
				{
					var block = new Block();
					if (block.LoadFromXml(anyNode))
						blocks.Add(block);
				}
				else if (anyNode.Name == "Attribute")
				{
					var attribute = new AttributeBlock();
					if (attribute.LoadFromXml(anyNode))
						blocks.Add(attribute);
				}

				anyNode = anyNode.GetNextSiblingAny();
			}

			// Components (Templates)
			for (int i = 0; i < ComponentNames.Length; ++i)
			{
				var componentNode = xmlNode.GetFirstElement(ComponentNames[i]);
				while (componentNode != null)
				{
					var text = componentNode.GetTextValue();
					bool detached = componentNode.GetAttributeBool("detached", false);
					bool raw = componentNode.GetAttributeBool("raw", false);

					bool important = false;
					if (componentNode.Name == "System")
						important = componentNode.GetAttributeBool("important", false);
					bool groupOnly = false;
					if (componentNode.Name == "Greeting")
						groupOnly = componentNode.GetAttributeBool("group", false);
					bool perActor = false;
					if (componentNode.Name != "Grammar")
						perActor = componentNode.GetAttributeBool("per-actor", false);

					ICondition condition = null;
					if (componentNode.HasAttribute("rule"))
						condition = Rule.Parse(componentNode.GetAttribute("rule"));

					templates.Add(new Template() {
						channel = EnumHelper.FromInt(i, Component.Invalid),
						condition = condition,
						text = text.ConvertLinebreaks(Linebreak.CRLF),
						flags = (detached ? Template.Flags.Detached : 0)
							| (raw ? Template.Flags.Raw : 0)
							| (important ? Template.Flags.Important : 0)
							| (groupOnly ? Template.Flags.GroupOnly : 0)
							| (perActor ? Template.Flags.PerActor : 0),
					});
					componentNode = componentNode.GetNextSibling();
				}
			}

			// Lore items
			var loreNode = xmlNode.GetFirstElement("Lore");
			while (loreNode != null)
			{
				var key = loreNode.GetValueElement("Name").Trim();
				var text = loreNode.GetValueElement("Value").Trim();
				var loreOrder = loreNode.GetAttributeInt("order", 50);

				if (!(string.IsNullOrEmpty(key) || string.IsNullOrEmpty(text)))
				{
					ICondition condition = null;
					if (loreNode.HasAttribute("rule"))
						condition = Rule.Parse(loreNode.GetAttribute("rule"));

					loreItems.Add(new LoreItem() {
						key = key,
						text = text,
						order = loreOrder,
						condition = condition,
					});
				}

				loreNode = loreNode.GetNextSibling();
			}

			// Requires
			string requiresStr = xmlNode.GetValueElement("Requires");
			if (string.IsNullOrEmpty(requiresStr) == false)
				this.requires = Rule.Parse(requiresStr);

			if (isBase)
			{
				// Base exclusivity requirement
				if (this.requires != null)
					this.requires = Condition.And(BaseExclusivityRule, this.requires);
				else
					this.requires = BaseExclusivityRule;
			}

			// Includes
			var includeNode = xmlNode.GetFirstElement("Include");
			while (includeNode != null)
			{
				StringHandle include = includeNode.GetTextValue(null);
				if (StringHandle.IsNullOrEmpty(include) == false)
					includes.Add(include);
				includeNode = includeNode.GetNextSibling();
			}

			// Color
			string sColor = xmlNode.GetValueElement("Color", null);
			hasCustomColor = string.IsNullOrWhiteSpace(sColor) == false;
			if (hasCustomColor)
			{
				if (Color.TryParse(sColor, out var parsedColor))
					color = parsedColor;
				else
					hasCustomColor = false;
			}
			if (!hasCustomColor)
			{
				if (category != Category.Undefined && Constants.RecipeColorByCategory.ContainsKey(category))
					color = Constants.RecipeColorByCategory[category];
				else if (drawer != Drawer.Undefined && Constants.RecipeColorByDrawer.ContainsKey(drawer))
					color = Constants.RecipeColorByDrawer[drawer];
				else
					color = Constants.DefaultColor;
			}

			// Strings (and rules)
			strings.LoadFromXml(xmlNode);

			// Adjectives
			adjectives.Clear();
			var adjectiveNode = xmlNode.GetFirstElement("Adjective");
			while (adjectiveNode != null)
			{
				var adjective = new CharacterAdjective();
				if (adjective.LoadFromXml(adjectiveNode))
					adjectives.Add(adjective);
				adjectiveNode = adjectiveNode.GetNextSibling();
			}

			// Nouns
			nouns.Clear();
			var nounNode = xmlNode.GetFirstElement("Noun");
			while (nounNode != null)
			{
				var noun = new CharacterNoun();
				if (noun.LoadFromXml(nounNode))
					nouns.Add(noun);
				nounNode = nounNode.GetNextSibling();
			}

			var addendumNode = xmlNode.GetFirstElement("Addendum");
			while (addendumNode != null)
			{
				var addendum = new CharacterNoun();
				if (addendum.LoadFromXml(addendumNode))
				{
					addendum.affix = CharacterNoun.Affix.Addendum;
					nouns.Add(addendum);
				}
				addendumNode = addendumNode.GetNextSibling();
			}

			uid = GetHashCode();
			return true;
		}

		public void SaveToXml(XmlNode xmlNode)
		{
			xmlNode.AddAttribute("id", id.ToString());
			xmlNode.AddAttribute("format", FormatVersion);
			xmlNode.AddAttribute("version", version.ToString());

			xmlNode.AddValueElement("Name", origName);
			xmlNode.AddValueElement("Title", _title);
			if (string.IsNullOrWhiteSpace(categoryTag) == false)
				xmlNode.AddValueElement("Category", categoryTag);

			// Requires
			if (requires != null)
				xmlNode.AddValueElement("Requires", requires.ToString());

			// Description
			if (string.IsNullOrEmpty(description) == false)
				xmlNode.AddValueElement("Description", description);

			// Author
			if (string.IsNullOrEmpty(author) == false)
				xmlNode.AddValueElement("Author", author);

			// Multiple
			if (allowMultiple != AllowMultiple.No)
				xmlNode.AddValueElement("Multiple", EnumHelper.ToString(allowMultiple));

			// Order?
			if (order.HasValue)
				xmlNode.AddValueElement("Order", order.Value);

			// Tags
			if (flags.Count > 0)
				xmlNode.AddValueElement("Flags", Utility.ListToCommaSeparatedString(flags));

			// Parameters
			if (parameters.Count > 0)
			{
				foreach (var parameter in parameters)
					parameter.SaveToXml(xmlNode);
			}

			// Blocks
			foreach (var block in blocks)
			{
				if (block is AttributeBlock)
				{
					var attributeNode = xmlNode.AddElement("Attribute");
					(block as AttributeBlock).SaveToXml(attributeNode);
				}
				else
				{
					var blockNode = xmlNode.AddElement("Node");
					block.SaveToXml(blockNode);
				}
			}

			// Templates
			foreach (var template in templates)
			{
				XmlElement templateNode;
				switch (template.channel)
				{
				default:
					continue;
				case Component.System:		templateNode = xmlNode.AddElement("System"); break;
				case Component.Persona:		templateNode = xmlNode.AddElement("Persona"); break;
				case Component.Scenario:	templateNode = xmlNode.AddElement("Scenario"); break;
				case Component.Greeting:	templateNode = xmlNode.AddElement("Greeting"); break;
				case Component.Example:		templateNode = xmlNode.AddElement("Example"); break;
				case Component.Grammar:		templateNode = xmlNode.AddElement("Grammar"); break;
				case Component.UserPersona:	templateNode = xmlNode.AddElement("User"); break;
				}

				if (template.condition != null)
					templateNode.AddAttribute("rule", template.condition.ToString());
				if (template.isDetached)
					templateNode.AddAttribute("detached", true);
				if (template.isRaw)
					templateNode.AddAttribute("raw", true);
				if (template.isImportant)
					templateNode.AddAttribute("important", true);
				if (template.isGroupOnly)
					templateNode.AddAttribute("group", true);
				templateNode.AddTextValue(template.text);
			}

			// Lore
			foreach (var lore in loreItems)
			{
				XmlElement loreNode = xmlNode.AddElement("Lore");

				loreNode.AddValueElement("Name", lore.key);
				loreNode.AddValueElement("Value", lore.text);

				loreNode.AddAttribute("order", lore.order);
				if (lore.condition != null)
					loreNode.AddAttribute("rule", lore.condition.ToString());
				loreNode.AddTextValue(lore.text);
			}

			// Strings (and macros, rules)
			strings.SaveToXml(xmlNode);
		}

		public object Clone()
		{
			var clone = new Recipe(this.filename);
			clone.id = this.id;
			clone.uid = this.uid;
			clone.origName = this.origName;
			clone.name = this.name;
			clone._title = this._title;
			clone.path = (string[])this.path?.Clone();
			clone.drawer = this.drawer;
			clone.category = this.category;
			clone.categoryTag = this.categoryTag;
			clone.type = this.type;
			clone.instanceIndex = this.instanceIndex;
			clone.isEnabled = this.isEnabled;
			clone.isCollapsed = this.isCollapsed;
			clone.version = this.version;
			clone.description = this.description;
			clone.author = this.author;
			clone.allowMultiple = this.allowMultiple;
			clone.requires = this.requires;
			clone.order = this.order;
			clone.flags = new HashSet<StringHandle>(this.flags);
			clone.color = this.color;
			clone.hasCustomColor = this.hasCustomColor;
			clone.strings = this.strings;
			clone.includes = new List<StringHandle>(this.includes);
			clone.enableTextFormatting = this.enableTextFormatting;
			clone.enableNSFWContent = this.enableNSFWContent;
			clone.levelOfDetail = this.levelOfDetail;

			clone.blocks = new List<Block>(this.blocks.Count);
			for (int i = 0; i < this.blocks.Count; ++i)
			{
				var other = this.blocks[i];
				if (other is AttributeBlock)
					clone.blocks.Add((AttributeBlock)other.Clone());
				else
					clone.blocks.Add((Block)other.Clone());
			}

			clone.templates = new List<Template>(this.templates.Count);
			for (int i = 0; i < this.templates.Count; ++i)
			{
				var other = this.templates[i];
				clone.templates.Add(new Template() {
					channel = other.channel,
					text = other.text,
					condition = other.condition,
					flags = other.flags,
				});
			}
			clone.parameters = new List<IParameter>(this.parameters.Count);
			for (int i = 0; i < this.parameters.Count; ++i)
			{
				var other = (IParameter)this.parameters[i].Clone();
				other.recipe = clone;
				clone.parameters.Add(other);
			}
			clone.loreItems = new List<LoreItem>(this.loreItems.Count);
			for (int i = 0; i < this.loreItems.Count; ++i)
			{
				var other = this.loreItems[i];
				clone.loreItems.Add(new LoreItem() {
					key = other.key,
					text = other.text,
					condition = other.condition,
					order = other.order,
				});
			}
			clone.adjectives = new List<CharacterAdjective>(this.adjectives.Count);
			for (int i = 0; i < this.adjectives.Count; ++i)
			{
				var other = this.adjectives[i];
				clone.adjectives.Add(new CharacterAdjective() {
					value = other.value,
					priority = other.priority,
					condition = other.condition,
					order = other.order,
				});
			}
			clone.nouns = new List<CharacterNoun>(this.nouns.Count);
			for (int i = 0; i < this.nouns.Count; ++i)
			{
				var other = this.nouns[i];
				clone.nouns.Add(new CharacterNoun() {
					value = other.value,
					affix = other.affix,
					priority = other.priority,
					condition = other.condition
				});
			}

			return clone;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 0x56C9982B;
				hash ^= Utility.MakeHashCode(
					id,
					requires,
					strings,
					allowMultiple);
				hash ^= Utility.MakeHashCode(flags, Utility.HashOption.None);
				hash ^= Utility.MakeHashCode(templates, Utility.HashOption.None);
				hash ^= Utility.MakeHashCode(blocks, Utility.HashOption.None);
				hash ^= Utility.MakeHashCode(loreItems, Utility.HashOption.None);
				hash ^= Utility.MakeHashCode(parameters, Utility.HashOption.None);
				hash ^= Utility.MakeHashCode(adjectives, Utility.HashOption.None);
				hash ^= Utility.MakeHashCode(nouns, Utility.HashOption.None);
				return hash;
			}
		}

		public string GetMenuLabel()
		{
			return Utility.EscapeMenu(name);
		}

		public string GetTitle()
		{
			StringBuilder sb = new StringBuilder(Utility.EscapeMenu(title));
			if (isNSFW)
				sb.Append(" (NSFW)");
			return sb.ToString();
		}

		public string GetTooltip()
		{
			StringBuilder sbTooltip = new StringBuilder();
			sbTooltip.Append(title);

			if (isBase || isNSFW)
			{
				List<string> tags = new List<string>();
				if (isBase)
					tags.Add("Base recipe");
				if (isNSFW)
					tags.Add("NSFW");
				sbTooltip.Append(" (");
				sbTooltip.Append(string.Join("; ", tags));
				sbTooltip.Append(")");
			}
			if (string.IsNullOrEmpty(author) == false)
			{
				sbTooltip.NewLine();
				sbTooltip.Append("By ");
				sbTooltip.AppendLine(author);
			}
			if (string.IsNullOrEmpty(description) == false)
			{
				sbTooltip.NewParagraph();
				sbTooltip.AppendLine(description);
			}

			return sbTooltip.ToString();
		}

		public void ResetParameters()
		{
			if (parameters.ContainsAny(p => p is IResettableParameter) == false)
				return;

			Context evalContext = Current.Character.GetContext(CharacterData.ContextType.Full, Generator.Option.None);
			var evalConfig = new ContextString.EvaluationConfig() {
				macroSuppliers = new IMacroSupplier[] { Current.Strings },
				referenceSuppliers = new IStringReferenceSupplier[] { Current.Strings },
				ruleSuppliers = new IRuleSupplier[] { Current.Strings },
			};

			char[] brackets = new char[] { '{', '[' };
			var characterNames = new string[] { Current.Name };
			var userName = Current.Card.userPlaceholder;

			foreach (var parameter in parameters.OfType<IResettableParameter>())
			{
				string defaultValue = parameter.defaultValue;
				if (parameter.raw)
				{
					parameter.ResetValue(defaultValue);
					continue;
				}

				// Evaluate default value
				if (string.IsNullOrEmpty(defaultValue) == false && defaultValue.IndexOfAny(brackets, 0) != -1)
				{
					defaultValue = GingerString.FromString(Text.Eval(defaultValue, evalContext, evalConfig, Text.EvalOption.Default))
						.ToParameter();
					if (AppSettings.Settings.AutoConvertNames)
						defaultValue = GingerString.WithNames(defaultValue, characterNames, userName);
				}

				parameter.ResetValue(defaultValue);
			}
		}

		public bool LoadFromXml(string filename, string rootName)
		{
			try
			{
				var xmlDoc = Utility.LoadXmlDocument(filename);
				if (xmlDoc == null)
					return false;

				var root = xmlDoc.DocumentElement;
				if (root == null || root.Name != rootName)
					return false;

				return LoadFromXml(root);
			}
			catch
			{
				return false;
			}
		}

		// Static factory method for loading from file (matches what RecipeService expects)
		public static Recipe LoadFromFile(string filePath)
		{
			try
			{
				var xmlDoc = Utility.LoadXmlDocument(filePath);
				if (xmlDoc == null)
					return null;

				var root = xmlDoc.DocumentElement;
				if (root == null || root.Name != "Ginger")
					return null;

				var recipe = new Recipe(filePath);
				if (recipe.LoadFromXml(root))
					return recipe;
				return null;
			}
			catch
			{
				return null;
			}
		}
	}

}
