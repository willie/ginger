// Stub implementations for missing utilities in Avalonia port
// These provide minimal interfaces to allow the macro system to compile

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ginger
{
	/// <summary>
	/// Extension methods for enum flags.
	/// </summary>
	public static class EnumExtensions
	{
		public static bool Contains<T>(this T flags, T flag) where T : Enum
		{
			// Check if enum contains flag
			var flagsVal = Convert.ToInt64(flags);
			var flagVal = Convert.ToInt64(flag);
			return (flagsVal & flagVal) == flagVal;
		}

		public static bool ContainsAny<T>(this T flags, T flag) where T : Enum
		{
			// Check if any of the specified flags are set
			var flagsVal = Convert.ToInt64(flags);
			var flagVal = Convert.ToInt64(flag);
			return (flagsVal & flagVal) != 0;
		}
	}

	/// <summary>
	/// Simple hash table implementation (wrapper around Dictionary with list values).
	/// Used by Generator for interleaving example messages.
	/// </summary>
	public class HashTable<TKey, TValue> where TKey : notnull
	{
		private Dictionary<TKey, List<TValue>> _dict = new Dictionary<TKey, List<TValue>>();

		public void Add(TKey key, TValue value)
		{
			if (!_dict.ContainsKey(key))
				_dict[key] = new List<TValue>();
			_dict[key].Add(value);
		}

		public List<TValue> this[TKey key]
		{
			get
			{
				if (_dict.TryGetValue(key, out var list))
					return list;
				return new List<TValue>();
			}
		}

		public IEnumerable<TKey> Keys => _dict.Keys;

		public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
	}

	/// <summary>
	/// Stub for BackyardModelDatabase - not used in MVP
	/// </summary>
	public static class BackyardModelDatabase
	{
		public static void Refresh() { }

		public static string[] FindModels(string pattern)
		{
			// Stub: Returns empty array - full implementation would search model database
			return Array.Empty<string>();
		}

		// Takes two strings (modelDirectory, modelsJson) - original signature
		public static void FindModels(string modelDirectory, string modelsJson)
		{
			// Stub: Does nothing - full implementation would parse and store model info
		}

		public static string[] FindModels(string pattern, out string[] modelNames)
		{
			// Stub: Returns empty arrays - full implementation would search model database
			modelNames = Array.Empty<string>();
			return Array.Empty<string>();
		}
	}

	/// <summary>
	/// Stub for Resources - Windows Forms resource system
	/// </summary>
	public static class Resources
	{
		public static byte[] portrait_default => null;
		public static byte[] default_portrait => null;
	}

	/// <summary>
	/// Application version information.
	/// </summary>
	public static class AppVersion
	{
		public static string ProductVersion => "1.0.0";
		public static string Version => "1.0.0.0";
	}
}
