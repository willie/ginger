using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Ginger
{
	public class RecipeTemplate
	{
		public int uid { get { return _template != null ? _template.uid : -1; } }
		public string label { get { return _template != null ? _template.GetMenuLabel() : string.Empty; } }
		public VersionNumber version { get { return _template != null ? _template.version : default(VersionNumber); } }
		public string tooltip { get { return _template != null ? _template.GetTooltip() : string.Empty; } }
		public Recipe.Type type { get { return _template != null ? _template.type : Recipe.Type.Recipe; } }
		public Recipe.AllowMultiple allowMultiple { get { return _template != null ? _template.allowMultiple : Recipe.AllowMultiple.No; } }
		public ICondition requires { get { return _template != null ? _template.requires : null; } }
		public int? order { get { return _template != null ? _template.order : null; } }
		public int includes { get { return _template != null ? _template.includes.Count : 0; } }
		public bool hasDetached { get { return _template != null ? _template.templates.ContainsAny(t => t.isDetached) : false; } }

		public RecipeTemplate(Recipe template)
		{
			_template = template;
		}

		public Recipe Instantiate()
		{
			var clone = (Recipe)_template.Clone();
			clone.isEnabled = true;
			clone.ResetParameters();
			return clone;
		}

		private Recipe _template;
	}

	public static class RecipeBook
	{
		public static readonly string GlobalInternal = "__internal";
		public static readonly string GlobalExternal = "__global";
		public static readonly string PruneScenario = "__prune-scenario";

		public static readonly string[] StyleGrammar = new string[] {
			"__style_grammar_chat",
			"__style_grammar_novel",
			"__style_grammar_mixed",
			"__style_grammar_decorative",
			"__style_grammar_bold",
			"__style_grammar_brackets",
			"__style_grammar_cjk",
		};

		private static List<Recipe> recipes = new List<Recipe>();
		private static List<RecipePreset> presets = new List<RecipePreset>();

		public static IEnumerable<Recipe> allRecipes { get { return recipes; } }
		public static IEnumerable<RecipePreset> allPresets { get { return presets; } }

		public static void LoadRecipes()
		{
			recipes.Clear();
			presets.Clear();

			// Read recipes from content path
			string contentPath = Utility.ContentPath("Recipes");
			if (Directory.Exists(contentPath))
			{
				var recipeFiles = Utility.FindFilesInFolder(contentPath, "*.xml", true);
				for (int i = 0; i < recipeFiles.Length; ++i)
				{
					Recipe recipe;
					if (LoadRecipe(recipeFiles[i], out recipe))
						recipes.Add(recipe);
				}
				recipes = recipes.DistinctByVersion().ToList();
			}

			// Read snippets
			string snippetPath = Utility.ContentPath("Snippets");
			if (Directory.Exists(snippetPath))
			{
				var snippetFiles = Utility.FindFilesInFolder(snippetPath, "*.snippet", true);
				for (int i = 0; i < snippetFiles.Length; ++i)
				{
					var recipe = new Recipe(snippetFiles[i]);
					if (recipe.LoadFromXml(snippetFiles[i], "Ginger"))
					{
						recipe.type = Recipe.Type.Snippet;
						recipes.Add(recipe);
					}
				}
			}

			// Presets
			string presetPath = Utility.ContentPath("Templates");
			if (Directory.Exists(presetPath))
			{
				var presetFiles = Utility.FindFilesInFolder(presetPath, "*.xml", true);
				for (int i = 0; i < presetFiles.Length; ++i)
				{
					var preset = new RecipePreset();
					if (preset.LoadFromXml(presetFiles[i], "GingerTemplate"))
						presets.Add(preset);
				}
			}

			// Global recipe (external)
			string fnGlobal = Utility.ContentPath("Internal", Constants.InternalFiles.GlobalRecipe);
			if (File.Exists(fnGlobal))
			{
				var global_recipe = new Recipe(fnGlobal);
				if (global_recipe.LoadFromXml(fnGlobal, "Ginger"))
					recipes.Add(global_recipe);
			}

			// Load macros
			Current.LoadMacros();
		}

		private static bool LoadRecipe(string filename, out Recipe recipe)
		{
			byte[] buffer = Utility.LoadFile(filename);
			if (buffer == null || buffer.Length == 0)
			{
				recipe = default(Recipe);
				return false;
			}

			XmlDocument xmlDoc = new XmlDocument();
			try
			{
				using (var stream = new MemoryStream(buffer))
				{
					xmlDoc.Load(stream);
					if (string.Compare(xmlDoc.DocumentElement.Name, "Ginger") != 0)
					{
						recipe = default(Recipe);
						return false;
					}

					recipe = new Recipe(filename);
					if (recipe.LoadFromXml(xmlDoc.DocumentElement))
					{
						string xmlSource = Encoding.UTF8.GetString(stream.ToArray());
						AutodetectFlags(recipe, xmlSource);
						return true;
					}
				}
			}
			catch
			{
			}
			recipe = default(Recipe);
			return false;
		}

		private static string[] _detail_flags = new string[] { "less-detail", "normal-detail", "more-detail" };

		private static void AutodetectFlags(Recipe recipe, string xml)
		{
			int pos_detail = xml.IndexOfAny(_detail_flags, 0, StringComparison.OrdinalIgnoreCase);
			if (pos_detail != -1)
				recipe.flags.Add(Constants.Flag.LevelOfDetail);
			if (recipe.flags.Contains(Constants.Flag.NSFW) == false)
			{
				int pos_nsfw = xml.IndexOf("allow-nsfw", 0, StringComparison.OrdinalIgnoreCase);
				if (pos_nsfw != -1)
					recipe.flags.Add(Constants.Flag.NSFWOptional);
			}
		}

		public static string[] GetFolders(string root, Recipe.Drawer drawer)
		{
			string[] path = Utility.SplitPath(root);

			Func<string[], string[], bool> fnBeginsWith = (r, sub) => {
				if (r.Length >= sub.Length)
					return false;
				for (int i = 0; i < r.Length; ++i)
					if (string.Compare(r[i], sub[i], true) != 0)
						return false;
				return true;
			};

			return recipes
				.Where(r => {
					if (r.path.Length == 0)
						return false;
					if (r.isHidden)
						return false; // Hidden
					if (!fnBeginsWith(path, r.path))
						return false;
					if (r.type == Recipe.Type.Snippet)
						return drawer == Recipe.Drawer.Snippets;
					else if (r.type == Recipe.Type.Component)
						return drawer == Recipe.Drawer.Components;
					else if (r.drawer != drawer)
						return false; // By drawer
					return true;
				})
				.Select(r => r.path[path.Length])
				.DistinctBy(p => p.ToLowerInvariant())
				.OrderBy(p => p)
				.ToArray();
		}

		public static RecipeTemplate[] GetRecipes(string root, Recipe.Drawer drawer)
		{
			string[] path = Utility.SplitPath(root);

			Func<string[], string[], bool> fnExact = (a, b) => {
				if (a.Length != b.Length)
					return false;
				for (int i = 0; i < a.Length; ++i)
					if (string.Compare(a[i], b[i], true) != 0)
						return false;
				return true;
			};

			return recipes
				.Where(r => {
					if (r.isHidden)
						return false; // Hidden
					if (fnExact(path, r.path) == false)
						return false;
					if (r.type == Recipe.Type.Snippet)
						return drawer == Recipe.Drawer.Snippets;
					else if (r.type == Recipe.Type.Component)
						return drawer == Recipe.Drawer.Components;
					else if (r.drawer != drawer)
						return false; // By drawer
					return true;
				})
				.OrderBy(r => r.name.ToLowerInvariant())
				.Select(r => new RecipeTemplate(r))
				.ToArray();
		}

		public static RecipeTemplate[] GetRecipes(string[] path, Recipe.Drawer drawer)
		{
			return GetRecipes(string.Join("/", path), drawer);
		}

		public static string[] GetPresetFolders(string root)
		{
			string[] path = Utility.SplitPath(root);

			Func<string[], string[], bool> fnBeginsWith = (r, sub) => {
				if (r.Length >= sub.Length)
					return false;
				for (int i = 0; i < r.Length; ++i)
					if (string.Compare(r[i], sub[i], true) != 0)
						return false;
				return true;
			};

			return presets
				.Where(r => fnBeginsWith(path, r.path))
				.Select(r => r.path[path.Length])
				.DistinctBy(r => r.ToLowerInvariant())
				.OrderBy(r => r)
				.ToArray();
		}

		public static string[] GetPresets(string root)
		{
			string[] path = Utility.SplitPath(root);

			Func<string[], string[], bool> fnExact = (a, b) => {
				if (a.Length != b.Length)
					return false;
				for (int i = 0; i < a.Length; ++i)
					if (string.Compare(a[i], b[i], true) != 0)
						return false;
				return true;
			};

			return presets
				.Where(r => fnExact(path, r.path))
				.Select(r => r.name)
				.DistinctBy(r => r.ToLowerInvariant())
				.OrderBy(r => r)
				.ToArray();
		}

		public static List<Recipe> WithInternal(Recipe recipe)
		{
			return WithInternal(new Recipe[] { recipe });
		}

		public static List<Recipe> WithInternal(IEnumerable<Recipe> recipes)
		{
			List<Recipe> list = new List<Recipe>(recipes);
			Recipe externalGlobalRecipe = GetRecipeByID(GlobalExternal)?.Instantiate();
			Recipe internalGlobalRecipe = GetRecipeByID(GlobalInternal)?.Instantiate();
			if (externalGlobalRecipe != null)
				list.Insert(0, externalGlobalRecipe);
			if (internalGlobalRecipe != null)
				list.Insert(0, internalGlobalRecipe);
			return list;
		}

		public static RecipeTemplate GetRecipeByName(string path, string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;
			name = name.Replace("/", "//");
			if (string.IsNullOrEmpty(path) == false)
				return GetRecipeByID(string.Concat(path, "/", name));
			else
				return GetRecipeByID(string.Concat(name));
		}

		public static RecipeTemplate GetRecipeByID(string id)
		{
			var recipe = recipes.FirstOrDefault(r => r.id == id);
			if (recipe != null)
				return new RecipeTemplate(recipe);
			return null;
		}

		public static RecipeTemplate GetRecipeByID(StringHandle id)
		{
			var recipe = recipes.FirstOrDefault(r => r.id == id);
			if (recipe != null)
				return new RecipeTemplate(recipe);
			return null;
		}

		public static RecipeTemplate GetRecipeByUID(int uid)
		{
			var recipe = recipes.FirstOrDefault(r => r.uid == uid);
			if (recipe != null)
				return new RecipeTemplate(recipe);
			return null;
		}

		public static RecipePreset GetPresetByName(string path, string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;
			name = name.Replace("/", "//");
			if (string.IsNullOrEmpty(path) == false)
				return GetPresetByID(string.Concat(path, "/", name));
			else
				return GetPresetByID(string.Concat(name));
		}

		public static RecipePreset GetPresetByID(StringHandle id)
		{
			return presets.FirstOrDefault(r => r.id == id);
		}

		public static Recipe CreateRecipeFromResource(string xml, Recipe.Type type = Recipe.Type.Recipe, Recipe.Drawer drawer = Recipe.Drawer.Default)
		{
			byte[] recipeXml = Encoding.UTF8.GetBytes(xml);
			var xmlDoc = Utility.LoadXmlDocumentFromMemory(recipeXml);
			if (xmlDoc == null)
				return default(Recipe);
			Recipe recipe = new Recipe();
			if (recipe.LoadFromXml(xmlDoc.DocumentElement))
			{
				recipe.type = type;
				recipe.drawer = drawer;
				return recipe;
			}
			return default(Recipe);
		}

		public static RecipeTemplate GetEquivalentRecipe(Recipe other)
		{
			if (other == null)
				return null;

			if (other.id == "lorebook")
				return GetRecipeByID("__lorebook");

			var recipe = recipes.FirstOrDefault(r =>
				r.uid == other.uid
				|| (r.id == other.id && r.version >= other.version)
				|| (r.id == other.id && r.isInternal));
			if (recipe == null)
				return null;

			return new RecipeTemplate(recipe);
		}

		public static RecipeTemplate GetSimilarRecipe(Recipe other)
		{
			var recipe = recipes.FirstOrDefault(r => r.id == other.id);
			if (recipe == null)
				return null;
			if (recipe.uid == other.uid)
				return null; // Same
			return new RecipeTemplate(recipe);
		}

		public static IEnumerable<Recipe> DistinctByVersion(this IEnumerable<Recipe> source)
		{
			var recipesByID = source
				.GroupBy(r => r.id)
				.Select(g => {
					StringHandle id = g.Key;
					IEnumerable<Recipe> recipeGroup = g;
					return recipeGroup.OrderByDescending(r => r.version).FirstOrDefault();
				});

			foreach (Recipe element in recipesByID)
				yield return element;
		}
	}

	public class RecipePreset
	{
		public StringHandle id;
		public string name;
		public string filename;
		public string[] path = new string[0];
		public List<Recipe> recipes = new List<Recipe>();

		public bool LoadFromXml(string filename, string rootName)
		{
			this.filename = filename;

			try
			{
				var xmlDoc = Utility.LoadXmlDocument(filename);
				if (xmlDoc == null)
					return false;

				var root = xmlDoc.DocumentElement;
				if (root == null || root.Name != rootName)
					return false;

				// Path / Name / ID
				string strPath = root.GetValueElement("Name", null).SingleLine();
				if (string.IsNullOrEmpty(strPath))
					return false;

				strPath = strPath.Trim();
				if (strPath.Length == 0)
					return false;

				// Path
				strPath = strPath.Replace("//", "%%SLASH%%");
				var lsPath = strPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(s => s.Trim())
					.Where(s => s.Length > 0)
					.Select(s => s.Replace("%%SLASH%%", "/"))
					.ToList();

				name = strPath = lsPath[lsPath.Count - 1];
				path = lsPath.Take(lsPath.Count - 1).ToArray();
				id = root.GetAttribute("id", name);

				// Load recipes in preset
				var recipeNode = root.GetFirstElement("Recipe");
				while (recipeNode != null)
				{
					var recipe = new Recipe();
					if (recipe.LoadFromXml(recipeNode))
						recipes.Add(recipe);
					recipeNode = recipeNode.GetNextSibling();
				}

				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
