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
		public static bool IsNSFW { get; set; } // For Backyard integration

		public static Integration.Backyard.Link Link { get; set; } // Backyard link info

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
			None = 0,       // Card info only
			FlagsOnly = 1,  // + Recipe flags
			Full = 2,       // + Parameters
		}

		private string _uid;
		private string _spokenName;

		public string uid
		{
			get => _uid;
			set => _uid = value;
		}

		public string name
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(_spokenName))
					return _spokenName.Trim();
				else if (isMainCharacter && !string.IsNullOrWhiteSpace(Current.Card.name))
					return Current.Card.name.Trim();
				return Constants.DefaultCharacterName;
			}
			set => _spokenName = value;
		}

		public string spokenName
		{
			get => _spokenName;
			set => _spokenName = value;
		}

		public string gender { get; set; } = "";
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

		public bool isMainCharacter => this == Current.MainCharacter;

		public CharacterData()
		{
			_uid = Utility.CreateGUID();
		}

		public CharacterData Clone()
		{
			var clone = (CharacterData)MemberwiseClone();
			clone.recipes = new List<Recipe>(recipes.Count);
			foreach (var recipe in recipes)
				clone.recipes.Add((Recipe)recipe.Clone());
			clone.greetings = new List<string>(greetings);
			return clone;
		}

		// Overload without options parameter
		public Context GetContext(ContextType type)
		{
			return GetContext(type, Generator.Option.None, false);
		}

		public Context GetContext(ContextType type, Generator.Option options, bool includeInactiveRecipes = false)
		{
			var context = Context.CreateEmpty();

			// Character marker
			int actorIndex = Current.Characters.IndexOf(this);
			context.SetValue("char", GingerString.MakeInternalCharacterMarker(actorIndex));
			int charactersIndex = 0;
			context.SetValue("characters", string.Join(Text.Delimiter,
				Current.Characters.Select(c => GingerString.MakeInternalCharacterMarker(charactersIndex++))));
			context.SetValue("user", GingerString.InternalUserMarker);
			context.SetValue("actor:index", actorIndex);

			// Names
			context.SetValue("card", Utility.FirstNonEmpty(Current.Card.name, Current.Name, Constants.DefaultCharacterName));
			context.SetValue("name", Utility.FirstNonEmpty(name, Current.Card.name, Constants.DefaultCharacterName));
			context.SetValue("#name", GingerString.InternalUserMarker);
			context.SetValue("names",
				string.Join(Text.Delimiter,
					Current.Characters.Select(c => c.name)
					.Where(s => !string.IsNullOrEmpty(s))));
			context.SetValue("actors",
				string.Join(Text.Delimiter,
				Current.Characters
					.Except(new CharacterData[] { Current.MainCharacter })
					.Select(c => c.name)
					.Where(s => !string.IsNullOrEmpty(s))));
			context.SetValue("others",
				string.Join(Text.Delimiter,
				Current.Characters
					.Except(new CharacterData[] { this })
					.Select(c => c.name)
					.Where(s => !string.IsNullOrEmpty(s))));
			context.SetValue("actor:count", Current.Characters.Count);
			for (int i = 0; i < Current.Characters.Count; ++i)
			{
				context.SetValue($"name:{i + 1}", Current.Characters[i].name);
				context.SetValue($"actor:{i + 1}", GingerString.MakeInternalCharacterMarker(i));
			}

			// Gender
			if (!string.IsNullOrWhiteSpace(gender))
			{
				context.SetValue("gender", gender.ToLowerInvariant());
				context.SetFlag(gender);

				bool bFuta = GenderSwap.IsFutanari(gender);
				if (bFuta)
					context.SetFlag("futanari");

				// Custom gender?
				if (!(string.Compare(gender, "male", true) == 0
					|| string.Compare(gender, "female", true) == 0
					|| bFuta))
				{
					context.SetFlag("custom-gender");
				}

				// Transgender
				if (gender.ToLowerInvariant().Contains("trans"))
				{
					if (gender.ToLowerInvariant().Contains("woman") || gender.ToLowerInvariant().Contains("female"))
						context.SetFlag("trans-female");
					else if (gender.ToLowerInvariant().Contains("man") || gender.ToLowerInvariant().Contains("male"))
						context.SetFlag("trans-male");
				}
			}

			// Gender (user)
			if (Current.Card.userGender != null)
			{
				var userGender = Current.Card.userGender.ToLowerInvariant();
				context.SetValue("user-gender", userGender);
				context.SetValue("#gender", userGender);
			}

			// Is actor?
			if (!isMainCharacter)
				context.SetFlag(Constants.Flag.Actor);
			if (Current.IsGroup)
			{
				if (options.Contains(Generator.Option.Group))
				{
					context.SetFlag("group-chat");
					context.SetFlag(Constants.Flag.Group);
				}
				else
				{
					context.SetFlag("multi-character");
					context.SetFlag(Constants.Flag.MultiCharacter);
				}
			}

			// Allow nsfw?
			if (AppSettings.Settings.AllowNSFW)
				context.SetFlag("allow-nsfw");

			// Level of detail
			switch (Current.Card.detailLevel)
			{
				case CardData.DetailLevel.Low:
					context.SetFlag("less-detail");
					break;
				case CardData.DetailLevel.High:
					context.SetFlag("more-detail");
					break;
			}
			context.SetValue("detail", EnumHelper.ToInt(Current.Card.detailLevel));

			// Text style
			context.SetValue("text-style", EnumHelper.ToInt(Current.Card.textStyle));

			// Language
			context.SetValue("__locale", AppSettings.Settings.Locale);

			switch (Current.Card.textStyle)
			{
				case CardData.TextStyle.None:
					break;
				case CardData.TextStyle.Novel:
					context.SetFlag("__style-quotes");
					break;
				case CardData.TextStyle.Chat:
					context.SetFlag("__style-action-asterisks");
					break;
				case CardData.TextStyle.Mixed:
					context.SetFlag("__style-quotes");
					context.SetFlag("__style-action-asterisks");
					break;
				case CardData.TextStyle.Decorative:
					context.SetFlag("__style-quotes-decorative");
					break;
				case CardData.TextStyle.Japanese:
					context.SetFlag("__style-quotes-cjk");
					break;
				case CardData.TextStyle.Parentheses:
					context.SetFlag("__style-action-brackets");
					break;
				case CardData.TextStyle.Bold:
					context.SetFlag("__style-action-bold");
					break;
			}

			// Flags
			if (Current.Card.extraFlags.Contains(CardData.Flag.PruneScenario))
				context.SetFlag(Constants.Flag.PruneScenario);
			if (Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario))
				context.SetFlag(Constants.Flag.UserPersonaInScenario);

			if (type == ContextType.Full)
			{
				Context fullContext;
				ParameterResolver.ResolveParameters(recipes.ToArray(), context, out fullContext);
				return fullContext;
			}
			else if (type == ContextType.FlagsOnly)
			{
				foreach (var recipe in recipes)
				{
					if (!recipe.isEnabled && !includeInactiveRecipes)
						continue;
					context.SetFlags(recipe.flags);
				}
			}

			return context;
		}

		public Context GetContextForRecipe(Recipe targetRecipe, Generator.Option option = Generator.Option.None)
		{
			int index = recipes.IndexOf(targetRecipe);
			if (index == -1)
				return GetContext(ContextType.None, option);

			var localContexts = ParameterResolver.GetLocalContexts(recipes.ToArray(), GetContext(ContextType.None, option));
			return localContexts[index];
		}

		public Recipe AddRecipe(RecipeTemplate recipeTemplate)
		{
			var recipe = recipeTemplate.Instantiate();
			if (AddRecipe(recipe))
				return recipe;
			return null;
		}

		public bool AddRecipe(Recipe recipe)
		{
			if (recipe.allowMultiple == Recipe.AllowMultiple.No && recipes.Any(r => r.uid == recipe.uid))
				return false;
			if (recipe.allowMultiple == Recipe.AllowMultiple.One && Current.AllRecipes.Any(r => r.uid == recipe.uid))
				return false;

			if (recipe.isSnippet)
				return false;

			int index = 0;
			foreach (var existing in recipes.Where(r => r.id == recipe.id))
				index = Math.Max(index, recipe.instanceIndex + 1);
			recipe.instanceIndex = index;

			if (recipe.isBase)
				recipes.Insert(0, recipe);
			else
				recipes.Add(recipe);

			Current.IsDirty = true;
			return true;
		}

		public bool RemoveRecipe(Recipe recipe)
		{
			if (recipes.Remove(recipe))
			{
				Current.IsDirty = true;
				return true;
			}
			return false;
		}

		public Recipe[] AddRecipePreset(RecipePreset preset)
		{
			var instances = new List<Recipe>();

			foreach (var presetRecipe in preset.recipes)
			{
				// Clone the recipe from the preset
				var recipe = (Recipe)presetRecipe.Clone();
				if (AddRecipe(recipe))
					instances.Add(recipe);
			}

			return instances.ToArray();
		}

		public bool IsEmpty()
		{
			if (recipes.Count > 0 || !string.IsNullOrEmpty(_spokenName))
				return false;

			int index = Current.Characters.IndexOf(this);
			if (index == 0)
			{
				return Current.Card.portraitImage == null
					&& Current.Card.assets.assets.Count(a => a.actorIndex <= 0) == 0;
			}
			else if (index > 0)
			{
				return Current.Card.assets.assets.Count(a => a.actorIndex == index) == 0;
			}
			return true;
		}
	}

	/// <summary>
	/// Card-level data (metadata about the character card).
	/// This is separate from CharacterData - one card can contain multiple characters.
	/// </summary>
	public class CardData
	{
		[Flags]
		public enum Flag : int
		{
			None = 0,
			PruneScenario = 1 << 0,
			UserPersonaInScenario = 1 << 1,

			OmitPersonality = 1 << 23,
			OmitUserPersona = 1 << 24,
			OmitSystemPrompt = 1 << 25,
			OmitAttributes = 1 << 26,
			OmitScenario = 1 << 27,
			OmitExample = 1 << 28,
			OmitGreeting = 1 << 29,
			OmitGrammar = 1 << 30,
			OmitLore = 1 << 31,

			Default = None,
		}

		public string uuid { get; set; } = Guid.NewGuid().ToString();
		public string name { get; set; } = "";
		public string creator { get; set; } = "";
		public string comment { get; set; } = "";
		public string versionString { get; set; } = "";
		public string userPlaceholder { get; set; } = "User";
		public string userGender { get; set; } = "";
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
			Default = None,
		}
		public TextStyle textStyle { get; set; } = TextStyle.Default;

		// Extra flags for generation options
		public HashSet<Flag> extraFlags { get; set; } = new HashSet<Flag>();

		// Whether to use style grammar
		public bool useStyleGrammar { get; set; } = false;

		// For Backyard integration
		public AssetCollection assets { get; set; } = new AssetCollection();
		public ImageRef portraitImage { get; set; }

		// Token counts (for optimization)
		public int[] lastTokenCounts { get; set; } = new int[3];

		// Source URLs
		public List<string> sources { get; set; } = new List<string>();

		// Custom variables for macro system
		public List<CustomVariable> customVariables { get; set; } = new List<CustomVariable>();

		public bool TryGetVariable(string key, out string value)
		{
			var variable = customVariables.FirstOrDefault(v => v.Name == key);
			if (!CustomVariableName.IsNullOrEmpty(variable.Name))
			{
				value = variable.Value;
				return true;
			}
			value = null;
			return false;
		}

		public void SetVariable(string key, string value)
		{
			var index = customVariables.FindIndex(v => v.Name == key);
			if (index >= 0)
				customVariables[index] = new CustomVariable(key, value);
			else
				customVariables.Add(new CustomVariable(key, value));
		}

		// Helper to convert extraFlags HashSet to int (for serialization)
		public int GetExtraFlagsAsInt()
		{
			int result = 0;
			foreach (var flag in extraFlags)
				result |= (int)flag;
			return result;
		}
	}

}
