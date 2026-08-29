using System.Collections.Generic;

namespace Stumblr
{
	/// <summary>
	/// Decides whether a block is the kind of thing you catch a foot on.
	///
	/// This is name matching, and it is name matching because there is nothing better to match on.
	/// Vanilla's blocks.xml has no Tags value containing "fence" or "railing" anywhere; the only
	/// fence marker in the whole config is FilterTags="...,SC_fences", which is a creative-menu
	/// filter present on a handful of blocks and absent from every chainlink piece. So the name is
	/// the signal, matched as a case-insensitive substring so that one pattern covers the sixty-odd
	/// chainlink variants and picks up modded fences named the obvious way.
	///
	/// Both the block's name and its shape's, because for a large family of blocks the fence-ness
	/// lives entirely in the shape. A picket fence built out of the shape menu is a woodShapes or
	/// concreteShapes block - the name is the material - carrying a shape called fencePicket or
	/// fenceCentered. Matching only the block name misses every one of those, which is most of the
	/// fences a player actually builds.
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

			string blockName = _block.blockName;
			string shapeName = ShapeName(_block);

			// An exclusion on either name vetoes, so a fence door whose shape happens to match is
			// still a door. Only then does either name get to say yes.
			bool trippable = !Matches(blockName, Settings.Exclude)
				&& !Matches(shapeName, Settings.Exclude)
				&& (Matches(blockName, Settings.Include) || Matches(shapeName, Settings.Include));

			cache[_block.blockID] = trippable;
			return trippable;
		}

		/// <summary>
		/// The block's shape name, or null. Block.shape is a plain field, so one block has one
		/// shape and caching the answer by block id stays valid. GetName is empty on the base
		/// BlockShape and only meaningful on BlockShapeNew, which is what the shape menu uses.
		/// </summary>
		private static string ShapeName(Block _block)
		{
			return _block.shape == null ? null : _block.shape.GetName();
		}

		private static bool Matches(string _name, List<string> _patterns)
		{
			if (string.IsNullOrEmpty(_name))
			{
				return false;
			}

			// ToLowerInvariant rather than the player's culture: these are ASCII identifiers, and a
			// Turkish locale would otherwise stop "railing" matching "Railing".
			string name = _name.ToLowerInvariant();

			for (int i = 0; i < _patterns.Count; i++)
			{
				if (name.Contains(_patterns[i]))
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
