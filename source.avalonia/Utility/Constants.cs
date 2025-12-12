using System.Collections.Generic;
using Avalonia.Media;

namespace Ginger
{
	public static class Constants
	{
		public static readonly string WebsiteURL = "https://github.com/DominaeDev/ginger";
		public static readonly string GitHubURL = "https://github.com/DominaeDev/ginger";
		public static readonly string DictionariesURL = "https://github.com/LibreOffice/dictionaries";

		public static readonly int GingerExtensionVersion = 1;
		public static readonly Color DefaultColor = Colors.Gainsboro;

		// Helper to convert HTML color strings
		private static Color FromHtml(string html)
		{
			if (Color.TryParse(html, out var color))
				return color;
			return Colors.Gainsboro;
		}

		public static Dictionary<Recipe.Drawer, Color> RecipeColorByDrawer = new Dictionary<Recipe.Drawer, Color>
		{
			{ Recipe.Drawer.Undefined,			Colors.Gainsboro },
			{ Recipe.Drawer.Components,			Colors.Gainsboro },
			{ Recipe.Drawer.Snippets,			FromHtml("#f0f0f0") },
			{ Recipe.Drawer.Lore,				FromHtml("#f2e6f2") },
			{ Recipe.Drawer.Model,				FromHtml("#bfd0db") },
			{ Recipe.Drawer.Character,			Colors.Honeydew },
			{ Recipe.Drawer.Traits,				FromHtml("#fffef0") },
			{ Recipe.Drawer.Mind,				Colors.Azure },
			{ Recipe.Drawer.Story,				Colors.Linen  },
		};

		public static Dictionary<Recipe.Category, Color> RecipeColorByCategory = new Dictionary<Recipe.Category, Color>
		{
			{ Recipe.Category.Undefined,		Colors.Gainsboro },
			{ Recipe.Category.Base,			    FromHtml("#98acb9") },
			{ Recipe.Category.Model,			FromHtml("#bfd0db") },
			{ Recipe.Category.Modifier,			FromHtml("#bfd0db") },

			{ Recipe.Category.Archetype,		Colors.Honeydew },
			{ Recipe.Category.Character,		Colors.Honeydew },
			{ Recipe.Category.Special,		    Colors.Honeydew },

			{ Recipe.Category.Appearance,	    FromHtml("#fffef0") },
			{ Recipe.Category.Body,				FromHtml("#fffef0") },
			{ Recipe.Category.Trait,		    FromHtml("#fffef0") },
			{ Recipe.Category.Feature,			FromHtml("#fffef0") },
			{ Recipe.Category.Speech,		    FromHtml("#fffef0") },

			{ Recipe.Category.Relationship,	    FromHtml("#fffef0") },
			{ Recipe.Category.Job,				FromHtml("#fffef0") },
			{ Recipe.Category.Role,				FromHtml("#fffef0") },

			{ Recipe.Category.Personality,	    FromHtml("#d2f0f0") },
			{ Recipe.Category.Mind,				Colors.Azure },
			{ Recipe.Category.Behavior,			Colors.Azure },
			{ Recipe.Category.Quirk,			Colors.Azure },
			{ Recipe.Category.Emotion,          Colors.Azure },
			{ Recipe.Category.Sexual,           FromHtml("#fff0f8") },

			{ Recipe.Category.User,			    FromHtml("#ddf5ef") },

			{ Recipe.Category.Story,			Colors.Linen },
			{ Recipe.Category.World,			Colors.Linen },
			{ Recipe.Category.Location,			Colors.Linen },
			{ Recipe.Category.Scenario,			Colors.Linen },
			{ Recipe.Category.Cast,				Colors.Linen },
			{ Recipe.Category.Concept,			Colors.Linen },

			{ Recipe.Category.Chat,			    Colors.FloralWhite },
			{ Recipe.Category.Lore,			    FromHtml("#f2e6f2") },
			{ Recipe.Category.Custom,		    Colors.WhiteSmoke },
		};

		public static Dictionary<Recipe.Category, Recipe.Drawer> DrawerFromCategory = new Dictionary<Recipe.Category, Recipe.Drawer>
		{
			{ Recipe.Category.Undefined,		Recipe.Drawer.Traits },
			{ Recipe.Category.Base,				Recipe.Drawer.Model },
			{ Recipe.Category.Model,			Recipe.Drawer.Model },
			{ Recipe.Category.Modifier,			Recipe.Drawer.Model },

			{ Recipe.Category.Archetype,		Recipe.Drawer.Character },
			{ Recipe.Category.Character,		Recipe.Drawer.Character },
			{ Recipe.Category.Body,				Recipe.Drawer.Character },
			{ Recipe.Category.Appearance,		Recipe.Drawer.Character },

			{ Recipe.Category.Trait,			Recipe.Drawer.Traits },
			{ Recipe.Category.Feature,			Recipe.Drawer.Traits },
			{ Recipe.Category.Speech,			Recipe.Drawer.Traits },
			{ Recipe.Category.Special,			Recipe.Drawer.Traits },
			{ Recipe.Category.Custom,			Recipe.Drawer.Traits },

			{ Recipe.Category.Personality,		Recipe.Drawer.Mind },
			{ Recipe.Category.Mind,				Recipe.Drawer.Mind },
			{ Recipe.Category.Behavior,			Recipe.Drawer.Mind },
			{ Recipe.Category.Quirk,			Recipe.Drawer.Mind },
			{ Recipe.Category.Emotion,			Recipe.Drawer.Mind },
			{ Recipe.Category.Sexual,			Recipe.Drawer.Mind },
			{ Recipe.Category.Job,				Recipe.Drawer.Mind },
			{ Recipe.Category.Role,             Recipe.Drawer.Mind },
			{ Recipe.Category.Relationship,		Recipe.Drawer.Mind },

			{ Recipe.Category.Chat,				Recipe.Drawer.Story },
			{ Recipe.Category.Story,			Recipe.Drawer.Story },
			{ Recipe.Category.World,			Recipe.Drawer.Story },
			{ Recipe.Category.User,				Recipe.Drawer.Story },
			{ Recipe.Category.Location,			Recipe.Drawer.Story },
			{ Recipe.Category.Scenario,			Recipe.Drawer.Story },
			{ Recipe.Category.Cast,				Recipe.Drawer.Story },
			{ Recipe.Category.Concept,			Recipe.Drawer.Story },
			{ Recipe.Category.Lore,				Recipe.Drawer.Story },
		};

		public static class Flag
		{
			public static readonly string Base = "base";
			public static readonly string NSFW = "nsfw";
			public static readonly string Actor = "__actor";
			public static readonly string Internal = "__internal";
			public static readonly string External = "__external";
			public static readonly string Component = "__component";
			public static readonly string System = "__system";
			public static readonly string Lorebook = "__lorebook";
			public static readonly string Greeting = "__greeting";
			public static readonly string Grammar = "__grammar";
			public static readonly string DontBake = "__nobake";
			public static readonly string Hidden = "__hidden";
			public static readonly string Group = "__group";
			public static readonly string MultiCharacter = "__multi";
			public static readonly string PruneScenario = "__prune-scenario";
			public static readonly string UserPersonaInScenario = "__user-persona-in-scenario";
			public static readonly string ToggleFormatting = "__formatting";
			public static readonly string NSFWOptional = "__nsfw-optional";
			public static readonly string LevelOfDetail = "__detail-optional";
		}

		public static class Variables
		{
			public static readonly string Adjectives	= "summary:adjectives";
			public static readonly string Noun			= "summary:noun";
			public static readonly string Addendum		= "summary:addendum";
			public static readonly string NoAffix		= "summary:noun:noaffix";
			public static readonly string Prefix		= "summary:noun:prefix";
			public static readonly string Suffix		= "summary:noun:suffix";

			public static readonly string CardName = "card";
			public static readonly string CharacterName = "name";
			public static readonly string CharacterGender = "gender";
			public static readonly string UserName = "#name";
			public static readonly string UserGender = "#gender";
		}

		public static readonly string DefaultCharacterName = "Unnamed";
		public static readonly string DefaultGroupName = "Untitled party";
		public static readonly string UnknownCharacter = "{unknown}";
		public static readonly string DefaultUserName = "User";
		public static readonly float AutoWrapWidth = 54;
		public static readonly int DefaultPortraitWidth = 256;
		public static readonly int DefaultPortraitHeight = 256;

		public static readonly string DefaultFontFace = "Segoe UI";
		public static readonly float DefaultFontSize = 9.75f;
		public static readonly float ReferenceFontSize = 8.25f;
		public static readonly float LineHeight = 1.15f;

		public static readonly int StatusBarMessageInterval = 3500;

		public static readonly int MaxImageDimension = 1800;
		public static readonly int MaxActorCount = 8;

		public static class ParameterPanel
		{
			public static readonly int TopMargin = 2;
			public static readonly int Spacing = 4;
			public static readonly int LabelWidth = 140;
			public static readonly int CheckboxWidth = 30;
		}

		public static class RecipePanel
		{
			public static readonly int BottomMargin = 8;
		}

		public static class InternalFiles
		{
			public static readonly string GlobalMacros = "global_macros.xml";
			public static readonly string GlobalRecipe = "global_recipe.xml";
		}

		public static class Drawer
		{
			public static readonly int SplitMenuAfter = 28;
			public static readonly int RecipesPerSplit = 20;
		}
	}
}
