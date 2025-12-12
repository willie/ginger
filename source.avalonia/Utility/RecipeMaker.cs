// RecipeMaker - Creates recipes from output
// Simplified for Avalonia port

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace Ginger
{
	public static class RecipeMaker
	{
		public static bool CreateRecipe(string filename, string name, string title, Recipe.Category category, string recipeXml, Generator.Output output, IEnumerable<StringHandle> flags = null)
		{
			string[] greetings = null;
			string[] group_greetings = null;
			if (output.greetings != null)
			{
				greetings = output.greetings
					.Select(g => Format(g))
					.NotNull()
					.ToArray();
			}
			if (output.group_greetings != null)
			{
				group_greetings = output.group_greetings
					.Select(g => Format(g))
					.NotNull()
					.ToArray();
			}

			KeyValuePair<string, string>[] loreItems = null;
			if (output.hasLore)
			{
				loreItems = output.lorebook.entries
					.Select(l => new KeyValuePair<string, string>(l.key, GingerString.ConvertNamePlaceholders(l.value)))
					.ToArray();
			}

			var sbXml = new StringBuilder(recipeXml);
			sbXml.Replace("%%NAME%%", SecurityElement.Escape(name));
			sbXml.Replace("%%TITLE%%", SecurityElement.Escape(title));
			sbXml.Replace("%%AUTHOR%%", SecurityElement.Escape((Current.Card.creator ?? "").Trim()));
			if (flags != null)
				sbXml.Replace("%%FLAGS%%", Utility.ListToCommaSeparatedString(flags));
			else
				sbXml.Replace("%%FLAGS%%", "");
			sbXml.Replace("%%NODES%%", "");
			sbXml.Replace("%%ATTRIBUTES%%", "");

			if (category == Recipe.Category.Undefined)
				category = Recipe.Category.Custom;
			sbXml.Replace("%%CATEGORY%%", EnumHelper.ToString(category));

			var system = GingerString.Escape(Format(output.system, true));
			var post_history = GingerString.Escape(Format(output.system_post_history, true));
			var persona = GingerString.Escape(Format(output.persona, true));
			var scenario = GingerString.Escape(Format(output.scenario, true));
			var userPersona = GingerString.Escape(Format(output.userPersona, true));
			var example = GingerString.Escape(Format(output.example));
			var grammar = GingerString.Escape(Format(output.grammar));

			AddRecipeComponent("System", system, null, sbXml);
			AddRecipeComponent("PostHistory", post_history, "important=\"true\"", sbXml);
			AddRecipeComponent("Persona", persona, null, sbXml);
			AddRecipeComponent("User", userPersona, null, sbXml);
			AddRecipeComponent("Scenario", scenario, null, sbXml);
			AddRecipeComponent("Example", example, null, sbXml);
			AddRecipeComponent("Grammar", grammar, null, sbXml);

			if (greetings != null && greetings.Length > 0)
				AddRecipeComponents("Greeting", greetings, null, sbXml);
			else
				AddRecipeComponent("Greeting", null, null, sbXml);

			if (group_greetings != null && group_greetings.Length > 0)
				AddRecipeComponents("GroupGreeting", group_greetings, "group=\"true\"", sbXml);
			else
				AddRecipeComponent("GroupGreeting", null, null, sbXml);

			// Fix <SystemPost>
			sbXml.Replace("<PostHistory", "<System");
			sbXml.Replace("</PostHistory", "</System");
			sbXml.Replace("<GroupGreeting", "<Greeting");
			sbXml.Replace("</GroupGreeting", "</Greeting");

			AddRecipeLore(loreItems, sbXml);

			try
			{
				string path = Path.GetDirectoryName(filename);
				if (!string.IsNullOrEmpty(path) && Directory.Exists(path) == false)
					Directory.CreateDirectory(path);
				File.WriteAllText(filename, sbXml.ToString(), Encoding.UTF8);
			}
			catch
			{
				return false;
			}
			finally
			{
				RecipeBook.LoadRecipes();
			}
			return true;
		}

		private static void AddRecipeComponent(string element, string value, string arguments, StringBuilder sbXml)
		{
			string mask = string.Concat("%%", element.ToUpperInvariant(), "%%");

			if (string.IsNullOrWhiteSpace(value) == false)
			{
				value = value.ConvertLinebreaks(Linebreak.LF);

				StringBuilder sb = new StringBuilder();
				if (element == "Grammar") // Grammar = CDATA
				{
					sb.Append($"\t<{element}");
					if (string.IsNullOrEmpty(arguments) == false)
					{
						sb.Append(" ");
						sb.Append(arguments);
					}
					sb.Append("><![CDATA[");
					sb.Append(value);
					sb.AppendLine($"]]></{element}>\n");
				}
				else
				{
					var lines = SecurityElement.Escape(value)
						.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

					sb.Append($"\t<{element}");
					if (string.IsNullOrEmpty(arguments) == false)
					{
						sb.Append(" ");
						sb.Append(arguments);
					}
					sb.AppendLine(">");
					foreach (var line in lines)
					{
						sb.Append("\t\t");
						sb.AppendLine(line);
					}
					sb.AppendLine($"\t</{element}>\n");

					sb.Replace("&apos;", "'");
					sb.Replace("&quot;", "\"");
				}

				sbXml.Replace(mask, sb.ToString());
			}
			else
			{
				sbXml.Replace(mask, "");
			}
		}

		private static void AddRecipeComponents(string element, string[] values, string arguments, StringBuilder sbXml)
		{
			string mask = string.Concat("%%", element.ToUpperInvariant(), "%%");
			int pos_insert = sbXml.IndexOf(mask, 0);
			if (pos_insert == -1)
				return;
			sbXml.Remove(pos_insert, mask.Length);

			for (int i = values.Length - 1; i >= 0; --i)
			{
				StringBuilder sb = new StringBuilder();

				var lines = SecurityElement.Escape(values[i]).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

				sb.Append($"\t<{element}");
				if (string.IsNullOrEmpty(arguments) == false)
				{
					sb.Append(" ");
					sb.Append(arguments);
				}
				sb.AppendLine(">");
				foreach (var line in lines)
				{
					sb.Append("\t\t");
					sb.AppendLine(line);
				}
				sb.AppendLine($"\t</{element}>\n");

				sb.Replace("&apos;", "'");
				sb.Replace("&quot;", "\"");

				sbXml.Insert(pos_insert, sb.ToString());
			}
		}

		private static void AddRecipeLore(KeyValuePair<string, string>[] lore, StringBuilder sbXml)
		{
			string mask = "%%LORE%%";
			int pos_insert = sbXml.IndexOf(mask, 0);
			if (pos_insert == -1)
				return;
			sbXml.Remove(pos_insert, mask.Length);

			if (lore == null || lore.Length == 0)
				return;

			for (int i = lore.Length - 1; i >= 0; --i)
			{
				StringBuilder sb = new StringBuilder();

				var lines = SecurityElement.Escape(lore[i].Value).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

				sb.AppendLine($"\t<Lore>");
				sb.Append($"\t\t<Name>");
				sb.Append(lore[i].Key);
				sb.AppendLine($"</Name>");
				sb.AppendLine($"\t\t<Value>");
				foreach (var line in lines)
				{
					sb.Append("\t\t\t");
					sb.AppendLine(line);
				}
				sb.AppendLine($"\t\t</Value>");
				sb.AppendLine($"\t</Lore>\n");

				sb.Replace("&apos;", "'");
				sb.Replace("&quot;", "\"");

				sbXml.Insert(pos_insert, sb.ToString());
			}
		}

		private static string Format(GingerString gingerString, bool bKeepLinebreaks = false)
		{
			string text = gingerString.ToString();
			if (string.IsNullOrWhiteSpace(text))
				return null;

			StringBuilder sb = new StringBuilder(text);

			// Unescape
			GingerString.Unescape(sb);
			GingerString.ConvertNamePlaceholders(sb, null, Current.SelectedCharacter);

			sb.Trim();
			sb.ConvertLinebreaks(Linebreak.CRLF);
			if (bKeepLinebreaks)
			{
				sb.Replace("\r\n", "  \r\n"); // Keep linebreaks
				sb.Replace("\r\n  \r\n", "\r\n\r\n"); // Empty row
			}

			return sb.ToString();
		}
	}
}
