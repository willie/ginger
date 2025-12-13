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

	// BackyardModelDatabase is now implemented in Utility/ModelInfo.cs

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
