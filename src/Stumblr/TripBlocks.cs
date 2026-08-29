using System.Collections.Generic;

namespace Stumblr
{
	/// <summary>
	/// Decides whether a block is the kind of thing you catch a foot on.
	///
	/// This is name matching, and it is name matching because there is nothing better to match on.
	/// Vanilla's blocks.xml has no Tags value containing "fence" or "railing" anywhere; the only
	/// fence marker in the whole config is FilterTags="...,SC_fences", which is a creative-menu
	/// filter present on a handful of blocks and absent from every chainlink piece. So the block's
	/// own name is the signal, matched as a case-insensitive substring so that one pattern covers
	/// the sixty-odd chainlink variants and picks up modded fences named the obvious way.
	///
	/// The default patterns match 169 of vanilla's 6299 blocks and all 28 of Undead Legacy's own
	/// railings and fences. Exclusions are checked first and exist because the include set is
	/// deliberately broad: a fence *door* is opened rather than hopped, and the *Helper blocks are
	/// never placed in a world.
	/// </summary>
	internal static class TripBlocks
	{
		/// <summary>
		/// Answers cached by block id, so the substring scan runs once per block type rather than
		/// once per jump. Cleared whenever the patterns change.
		/// </summary>
		private static readonly Dictionary<int, bool> cache = new Dictionary<int, bool>();

		internal static bool IsTrippable(Block _block)
		{
			if (_block == null)
			{
				return false;
			}

			if (cache.TryGetValue(_block.blockID, out bool cached))
			{
				return cached;
			}

			bool trippable = Classify(_block.blockName);
			cache[_block.blockID] = trippable;
			return trippable;
		}

		private static bool Classify(string _blockName)
		{
			if (string.IsNullOrEmpty(_blockName))
			{
				return false;
			}

			// ToLowerInvariant rather than the player's culture: block names are ASCII identifiers,
			// and a Turkish locale would otherwise stop "railing" matching "Railing".
			string name = _blockName.ToLowerInvariant();

			for (int i = 0; i < Settings.Exclude.Count; i++)
			{
				if (name.Contains(Settings.Exclude[i]))
				{
					return false;
				}
			}

			for (int i = 0; i < Settings.Include.Count; i++)
			{
				if (name.Contains(Settings.Include[i]))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>Called by <c>sb add</c> / <c>sb drop</c>; answers are no longer valid.</summary>
		internal static void PatternsChanged()
		{
			cache.Clear();
		}

		/// <summary>
		/// The <c>sb</c> menu line. Reports the block count as "so far", because the cache only
		/// holds block types some entity has actually jumped near - counting the whole catalogue
		/// would mean walking every block in the game on a settings print.
		/// </summary>
		internal static string Status()
		{
			int matched = 0;
			foreach (KeyValuePair<int, bool> entry in cache)
			{
				if (entry.Value)
				{
					matched++;
				}
			}

			return Settings.Include.Count + " patterns, " + matched + " of " + cache.Count
				+ " blocks seen so far";
		}

		/// <summary>The full pattern lists, printed by <c>sb blocks</c>.</summary>
		internal static string Describe()
		{
			return "Trippable: " + string.Join(", ", Settings.Include.ToArray())
				+ "\r\nNever: " + string.Join(", ", Settings.Exclude.ToArray());
		}
	}
}
