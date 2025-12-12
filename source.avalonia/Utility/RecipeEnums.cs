// Recipe enums - separated for dependency management
// These are referenced by Constants.cs before Recipe.cs is loaded

using System;
using System.Collections.Generic;
using System.Xml;
using Avalonia.Media;

namespace Ginger
{
	public partial class Recipe
	{
		public static readonly int FormatVersion = 1;
		public static readonly int MaxNameLength = 64;

		public enum Component
		{
			Invalid		= -1,
			System		= 0,
			Persona,
			UserPersona,
			Scenario,
			Example,
			Grammar,
			Greeting,
			System_PostHistory, // System/Important
			Greeting_Group,

			Count,
		}

		public enum Drawer
		{
			Undefined,

			Model,
			Character,
			Mind,
			Traits,
			Story,

			Components,
			Snippets,
			Lore,

			Default = Undefined,
		}

		public enum Type
		{
			Recipe,
			Component,
			Snippet,
			Lore,
		}

		public enum Category
		{
			// Recipe categories
			Undefined = 0,

			Base	= 100,
			Model, // (default)
			Modifier,

			Archetype = 200,
			Character, // (default)

			Appearance = 300,
			Special,
			Trait, // (default)
			Body,
			Feature,
			Speech,

			Personality = 400,
			Mind, // (default)
			Behavior,
			Quirk,
			Emotion,
			Sexual,

			User = 500,
			Relationship,

			Story = 600, // (default)
			Role,
			Job,
			Cast,

			World = 700,
			Scenario,
			Location,
			Concept,

			Custom = 750,

			Chat = 800, // Undocumented
			Lore = 900, // Undocumented
		}

		public enum DetailLevel
		{
			Default = 0,
			Less = 1,
			Normal = 2,
			More = 3,
		}

		public enum AllowMultiple
		{
			No = 0,
			Yes,
			One,
		}
	}

	/// <summary>
	/// Version number parsing for recipe files.
	/// </summary>
	public struct VersionNumber
	{
		public int major;
		public int minor;
		public int build;

		public static readonly VersionNumber Zero = new VersionNumber(0, 0, 0);

		public VersionNumber(int major, int minor = 0, int build = 0)
		{
			this.major = major;
			this.minor = minor;
			this.build = build;
		}

		public static VersionNumber Parse(string version)
		{
			if (string.IsNullOrEmpty(version))
				return new VersionNumber();

			var parts = version.Split('.');
			int major = 0, minor = 0, build = 0;

			if (parts.Length > 0)
				int.TryParse(parts[0], out major);
			if (parts.Length > 1)
				int.TryParse(parts[1], out minor);
			if (parts.Length > 2)
				int.TryParse(parts[2], out build);

			return new VersionNumber(major, minor, build);
		}

		public override string ToString()
		{
			if (build > 0)
				return $"{major}.{minor}.{build}";
			if (minor > 0)
				return $"{major}.{minor}";
			return $"{major}";
		}

		public int CompareTo(VersionNumber other)
		{
			if (major != other.major)
				return major.CompareTo(other.major);
			if (minor != other.minor)
				return minor.CompareTo(other.minor);
			return build.CompareTo(other.build);
		}

		public static bool operator >=(VersionNumber a, VersionNumber b)
		{
			return a.CompareTo(b) >= 0;
		}

		public static bool operator <=(VersionNumber a, VersionNumber b)
		{
			return a.CompareTo(b) <= 0;
		}

		public static bool operator >(VersionNumber a, VersionNumber b)
		{
			return a.CompareTo(b) > 0;
		}

		public static bool operator <(VersionNumber a, VersionNumber b)
		{
			return a.CompareTo(b) < 0;
		}
	}

	/// <summary>
	/// JSON extension data for preserving unknown properties during round-trip.
	/// </summary>
	public class JsonExtensionData : Dictionary<string, object>
	{
	}
}
