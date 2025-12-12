// Generator-related enums (subset of original for compatibility)
using System;

namespace Ginger.Models
{
	public static class Generator
	{
		[Flags]
		public enum Option
		{
			None = 0,
			Export = 1 << 0,
			Bake = 1 << 1,
			Snippet = 1 << 2,
			Linked = 1 << 3,

			Single = 1 << 10,
			All = 1 << 11,
			Group = 1 << 12,

			Preview = 1 << 20,
			Faraday = 1 << 21,
			SillyTavernV2 = 1 << 22,
			SillyTavernV3 = 1 << 23,
			SinglePrompt = 1 << 24,
		}
	}
}
