// Global state for Avalonia port
// Provides interface for generation and macro system

using System;
using System.Collections.Generic;
using System.Linq;

namespace Ginger
{
	/// <summary>
	/// Global application state.
	/// </summary>
	public static class Current
	{
		private static GingerCharacter _instance = new GingerCharacter();

		public static GingerCharacter Instance
		{
			get => _instance;
			set => _instance = value ?? new GingerCharacter();
		}

		public static CardData Card
		{
			get => Instance.Card;
			set => Instance.Card = value;
		}

		public static List<CharacterData> Characters
		{
			get => Instance.Characters;
			set => Instance.Characters = value;
		}

		public static CharacterData MainCharacter => Characters.Count > 0 ? Characters[0] : new CharacterData();

		public static int SelectedCharacter { get; set; }

		public static CharacterData Character =>
			SelectedCharacter >= 0 && SelectedCharacter < Characters.Count
				? Characters[SelectedCharacter]
				: MainCharacter;

		public static string Name =>
			Utility.FirstNonEmpty(MainCharacter.name, Card.name, Constants.DefaultCharacterName);

		public static string CardName
		{
			get
			{
				if (Characters.Count == 1)
					return Utility.FirstNonEmpty(Card.name, MainCharacter.spokenName, Constants.DefaultCharacterName);
				else
					return Utility.FirstNonEmpty(Card.name,
						Utility.CommaSeparatedList(Characters.Select(c =>
							Utility.FirstNonEmpty(c.spokenName, Constants.DefaultCharacterName)), "and", false),
						Constants.DefaultCharacterName);
			}
		}

		public static StringBank Strings { get; private set; } = new StringBank();
		public static string Filename { get; set; }

		public static bool IsGroup => Characters.Count > 1;
		public static bool IsDirty { get; set; }
		public static bool IsFileDirty { get; set; }
		public static bool IsLoading { get; set; }

		public static IEnumerable<Recipe> AllRecipes => Characters.SelectMany(c => c.recipes);
		public static IRuleSupplier[] RuleSuppliers => new IRuleSupplier[] { Strings };

		public static int seed => string.Concat(MainCharacter.name, MainCharacter.gender ?? "Gender").GetHashCode();

		public static event EventHandler OnLoadCharacter;

		public static void Reset()
		{
			Instance.Reset();
			SelectedCharacter = 0;
			Filename = null;
			IsDirty = false;
			IsFileDirty = false;
		}

		public static void LoadMacros()
		{
			Strings.Clear();
			// Load internal macros - would need to load from embedded resources
			// For now, just initialize empty
		}

		public static void NewCharacter()
		{
			Reset();
			Instance.Characters = new List<CharacterData> { new CharacterData() };
			OnLoadCharacter?.Invoke(null, EventArgs.Empty);
			IsDirty = false;
			IsFileDirty = false;
		}

		public static void AddCharacter()
		{
			var character = new CharacterData
			{
				spokenName = Constants.DefaultCharacterName,
			};
			Characters.Add(character);
			SelectedCharacter = Characters.Count - 1;
			IsDirty = true;
		}
	}

	/// <summary>
	/// Represents the full character data including card metadata.
	/// </summary>
	public class GingerCharacter
	{
		public CardData Card { get; set; } = new CardData();
		public List<CharacterData> Characters { get; set; } = new List<CharacterData> { new CharacterData() };

		public void Reset()
		{
			Card = new CardData();
			Characters = new List<CharacterData> { new CharacterData() };
		}
	}

	/// <summary>
	/// Represents individual character data.
	/// </summary>
	public class CharacterData
	{
		public enum ContextType
		{
			None = 0,
			WithRecipes = 1,
			Full = 2,
		}

		public string uid { get; set; } = Guid.NewGuid().ToString();
		public string name { get; set; } = "";
		public string spokenName { get; set; } = "";
		public string gender { get; set; }
		public string persona { get; set; } = "";
		public string personality { get; set; } = "";
		public string scenario { get; set; } = "";
		public string example { get; set; } = "";
		public string greeting { get; set; } = "";
		public string system { get; set; } = "";
		public string userPersona { get; set; } = "";
		public string grammar { get; set; } = "";

		// Greetings (multiple)
		public List<string> greetings { get; set; } = new List<string>();

		// Recipes for this character
		public List<Recipe> recipes { get; set; } = new List<Recipe>();

		public Context GetContext(ContextType type, Generator.Option options, bool includeCard = true)
		{
			var context = new Context();

			// Set character name
			if (!string.IsNullOrEmpty(name))
				context.SetValue("char", name);
			if (!string.IsNullOrEmpty(spokenName))
				context.SetValue("name", spokenName);

			// Set gender
			if (!string.IsNullOrEmpty(gender))
			{
				context.SetValue("gender", gender);
				context.SetFlag(gender.ToLowerInvariant());
			}

			// Set user name
			if (includeCard && Current.Card != null)
			{
				context.SetValue("user", Current.Card.userPlaceholder ?? "User");
			}

			return context;
		}

		public Context GetContextForRecipe(Recipe recipe, Generator.Option options)
		{
			return GetContext(ContextType.WithRecipes, options, true);
		}
	}

	/// <summary>
	/// Card-level data (metadata about the character card).
	/// This is separate from CharacterData - one card can contain multiple characters.
	/// </summary>
	public class CardData
	{
		[Flags]
		public enum Flag
		{
			None = 0,
			PruneScenario = 1 << 0,
			OmitAttributes = 1 << 1,
			OmitPersonality = 1 << 2,
			OmitSystemPrompt = 1 << 3,
			OmitUserPersona = 1 << 4,
			OmitScenario = 1 << 5,
			OmitExample = 1 << 6,
			OmitGrammar = 1 << 7,
			OmitGreeting = 1 << 8,
			OmitLore = 1 << 9,
		}

		public string uuid { get; set; } = Guid.NewGuid().ToString();
		public string name { get; set; } = "";
		public string creator { get; set; } = "";
		public string comment { get; set; } = "";
		public string userPlaceholder { get; set; } = "User";
		public HashSet<string> tags { get; set; } = new HashSet<string>();
		public DateTime? creationDate { get; set; }

		public enum DetailLevel { Low = -1, Normal = 0, High = 1 }
		public DetailLevel detailLevel { get; set; } = DetailLevel.Normal;

		public enum TextStyle
		{
			None = 0,
			Chat,       // Asterisks
			Novel,      // Quotes
			Mixed,      // Quotes + Asterisks
			Decorative, // Decorative quotes
			Bold,       // Double asterisks
			Parentheses,// Parentheses instead of asterisks
			Japanese,   // Japanese quotes
		}
		public TextStyle textStyle { get; set; } = TextStyle.None;

		// Extra flags for generation options
		public HashSet<Flag> extraFlags { get; set; } = new HashSet<Flag>();

		// Whether to use style grammar
		public bool useStyleGrammar { get; set; } = false;

		// Custom variables dictionary for macro system
		private Dictionary<string, string> _variables = new Dictionary<string, string>();

		public bool TryGetVariable(string key, out string value)
		{
			return _variables.TryGetValue(key, out value);
		}

		public void SetVariable(string key, string value)
		{
			_variables[key] = value;
		}
	}

}
