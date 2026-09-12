using System.Collections.Generic;
using UnityEngine;

namespace Stumblr
{
	/// <summary>
	/// Decides whether a block is the kind of thing a zombie has no business balancing on.
	///
	/// The main test is geometric. Every shape in the game carries a bounding box per rotation,
	/// computed from its mesh when the shape loads (<c>BlockShapeNew.CalcBounds</c>) and served by
	/// <c>Block.shape.GetBounds</c>. A fence, railing or pole is a box a fraction of a block wide
	/// in at least one horizontal direction; a floor is a full block wide in both. So: narrow if
	/// the thinner horizontal extent is under <see cref="Settings.NarrowWidth"/> and the box is at
	/// least <see cref="Settings.NarrowMinHeight"/> tall. Nothing in shapes.xml or blocks.xml
	/// encodes this - the geometry lives in the FBX models - so it has to be measured at runtime,
	/// and <c>sb probe</c> exists to show the measurement for whatever you are looking at.
	///
	/// Names are kept as an override on either side of that. An exclusion vetoes first: a fence
	/// door measures narrow but is opened, not perched on. An inclusion then accepts regardless of
	/// bounds, for things like hedges and barbed wire whose box is wide but whose footing is not.
	/// Both the block's name and its shape's are checked, because for the shape-menu blocks the
	/// fence-ness lives entirely in the shape: a picket fence is a woodShapes block with a
	/// fencePicket shape.
	/// </summary>
	internal static class NarrowBlocks
	{
		/// <summary>
		/// How far the zombie's feet may be above or below the block's measured top and still count
		/// as standing on it, rather than on the ground beside it with the fence in the same column.
		/// </summary>
		private const float TopTolerance = 0.3f;

		/// <summary>
		/// Answers cached by block id and rotation, since a rotation can swap which axis is the
		/// thin one. Cleared whenever the patterns or thresholds change.
		/// </summary>
		private static readonly Dictionary<int, bool> cache = new Dictionary<int, bool>();

		/// <summary>The last standing check, for <c>sb info</c>.</summary>
		internal static string LastChecked = "nothing yet";

		/// <summary>
		/// Whether the block directly under this entity's feet is narrow, and the entity is
		/// actually up on it.
		/// </summary>
		internal static bool IsStandingOnNarrow(EntityAlive _entity)
		{
			World world = _entity.world;
			if (world == null)
			{
				return false;
			}

			Vector3 pos = _entity.position;

			// A hair below the feet, so a zombie standing exactly on y=71.0 looks at block 70.
			Vector3i below = new Vector3i(Utils.Fastfloor(pos.x), Utils.Fastfloor(pos.y - 0.1f),
				Utils.Fastfloor(pos.z));
			BlockValue blockValue = world.GetBlock(below);

			if (blockValue.isair)
			{
				LastChecked = "air under " + _entity.EntityName + " at " + below;
				return false;
			}

			string why;
			bool narrow = IsNarrow(blockValue, out why);
			if (!narrow)
			{
				LastChecked = Name(blockValue) + " under " + _entity.EntityName + " - " + why;
				return false;
			}

			// On top of it, not beside it: the feet must be near the block's measured top. A zombie
			// walking along the base of a fence shares a column with the fence's lower block.
			float top = below.y + TopOf(blockValue);
			if (Mathf.Abs(pos.y - top) > TopTolerance)
			{
				LastChecked = Name(blockValue) + " under " + _entity.EntityName + " but feet at "
					+ Format.Number(pos.y - top) + " from its top";
				return false;
			}

			LastChecked = Name(blockValue) + " under " + _entity.EntityName + " - " + why;
			return true;
		}

		/// <summary>
		/// The verdict for one block, with the reason in words. Exclusions first, then inclusions,
		/// then the bounds.
		/// </summary>
		internal static bool IsNarrow(BlockValue _blockValue, out string _why)
		{
			Block block = _blockValue.Block;
			if (block == null)
			{
				_why = "no block";
				return false;
			}

			int key = CacheKey(_blockValue);
			if (cache.TryGetValue(key, out bool cached))
			{
				_why = cached ? "narrow (cached)" : "not narrow (cached)";
				return cached;
			}

			string blockName = block.blockName;
			string shapeName = ShapeName(block);
			bool narrow;

			string hit;
			if (Matches(blockName, Settings.Exclude, out hit) || Matches(shapeName, Settings.Exclude, out hit))
			{
				_why = "excluded by name '" + hit + "'";
				narrow = false;
			}
			else if (Matches(blockName, Settings.Include, out hit) || Matches(shapeName, Settings.Include, out hit))
			{
				_why = "included by name '" + hit + "'";
				narrow = true;
			}
			else
			{
				narrow = BoundsAreNarrow(_blockValue, out _why);
			}

			cache[key] = narrow;
			return narrow;
		}

		/// <summary>
		/// The geometric test. With several boxes, every box that reaches the top has to be narrow -
		/// a railing on a full-width step is a step you can stand on.
		/// </summary>
		private static bool BoundsAreNarrow(BlockValue _blockValue, out string _why)
		{
			Bounds[] bounds = GetBounds(_blockValue);
			if (bounds == null || bounds.Length == 0)
			{
				_why = "no bounds";
				return false;
			}

			float top = TopOf(_blockValue);
			bool any = false;

			for (int i = 0; i < bounds.Length; i++)
			{
				Vector3 size = bounds[i].size;
				if (bounds[i].max.y < top - 0.05f)
				{
					// Below the walking surface; irrelevant to footing.
					continue;
				}

				float width = Mathf.Min(size.x, size.z);
				if (width > Settings.NarrowWidth)
				{
					_why = "wide: " + Size(size);
					return false;
				}
				if (size.y < Settings.NarrowMinHeight)
				{
					_why = "too low: " + Size(size);
					return false;
				}
				any = true;
			}

			if (!any)
			{
				_why = "no box reaches its top";
				return false;
			}

			_why = "narrow by bounds: " + Size(bounds[0].size)
				+ (bounds.Length > 1 ? " (" + bounds.Length + " boxes)" : string.Empty);
			return true;
		}

		/// <summary>The highest point of any box, in block-local units.</summary>
		private static float TopOf(BlockValue _blockValue)
		{
			Bounds[] bounds = GetBounds(_blockValue);
			if (bounds == null || bounds.Length == 0)
			{
				return 1f;
			}

			float top = float.NegativeInfinity;
			for (int i = 0; i < bounds.Length; i++)
			{
				top = Mathf.Max(top, bounds[i].max.y);
			}
			return top;
		}

		/// <summary>
		/// Bounds are block-local with the block's corner at the origin, which is how
		/// <c>Block.GetCollisionAABB</c> uses them: it adds the block position to the centre.
		/// </summary>
		private static Bounds[] GetBounds(BlockValue _blockValue)
		{
			Block block = _blockValue.Block;
			if (block == null || block.shape == null)
			{
				return null;
			}
			return block.shape.GetBounds(_blockValue);
		}

		/// <summary>
		/// The block id in the low bits and the rotation above it. BlockShapeNew serves a different
		/// box per rotation, so the two have to be cached apart.
		/// </summary>
		private static int CacheKey(BlockValue _blockValue)
		{
			return (_blockValue.rotation << 24) | (_blockValue.Block.blockID & 0xFFFFFF);
		}

		/// <summary>
		/// The block's shape name, or null. GetName is empty on the base BlockShape and only
		/// meaningful on BlockShapeNew, which is what the shape menu uses.
		/// </summary>
		private static string ShapeName(Block _block)
		{
			return _block.shape == null ? null : _block.shape.GetName();
		}

		internal static bool Matches(string _name, List<string> _patterns, out string _hit)
		{
			_hit = null;
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
					_hit = _patterns[i];
					return true;
				}
			}

			return false;
		}

		/// <summary>Called by <c>sb add</c> / <c>sb drop</c> / <c>sb narrow</c>; answers are no longer valid.</summary>
		internal static void RulesChanged()
		{
			cache.Clear();
		}

		private static string Name(BlockValue _blockValue)
		{
			Block block = _blockValue.Block;
			if (block == null)
			{
				return "?";
			}
			string shape = ShapeName(block);
			return block.blockName + (string.IsNullOrEmpty(shape) ? string.Empty : " (" + shape + ")");
		}

		private static string Size(Vector3 _size)
		{
			return Format.Number(_size.x) + " x " + Format.Number(_size.y) + " x " + Format.Number(_size.z);
		}

		/// <summary>
		/// Everything <c>sb probe</c> prints for one block: name, shape, rotation, every box, and
		/// the verdict with its reason.
		/// </summary>
		internal static string Probe(BlockValue _blockValue, Vector3i _pos)
		{
			if (_blockValue.isair || _blockValue.Block == null)
			{
				return "Air at " + _pos + ".";
			}

			string why;
			bool narrow = IsNarrow(_blockValue, out why);

			string text = Name(_blockValue) + " at " + _pos + ", rotation " + _blockValue.rotation
				+ "\r\n  verdict: " + (narrow ? "NARROW" : "not narrow") + " - " + why;

			Bounds[] bounds = GetBounds(_blockValue);
			if (bounds == null || bounds.Length == 0)
			{
				return text + "\r\n  bounds: none";
			}

			for (int i = 0; i < bounds.Length; i++)
			{
				Vector3 min = bounds[i].min;
				Vector3 max = bounds[i].max;
				text += "\r\n  box " + i + ": size " + Size(bounds[i].size) + ", from ("
					+ Format.Number(min.x) + ", " + Format.Number(min.y) + ", " + Format.Number(min.z)
					+ ") to (" + Format.Number(max.x) + ", " + Format.Number(max.y) + ", "
					+ Format.Number(max.z) + ")";
			}

			return text;
		}

		/// <summary>The <c>sb narrow</c> menu line.</summary>
		internal static string Status()
		{
			return "thinner than " + Format.Number(Settings.NarrowWidth) + " wide, at least "
				+ Format.Number(Settings.NarrowMinHeight) + " tall";
		}

		/// <summary>
		/// The <c>sb blocks</c> menu line. Reports the block count as "so far", because the cache
		/// only holds block types some zombie has actually stood on or the probe has looked at.
		/// </summary>
		internal static string BlocksStatus()
		{
			int matched = 0;
			foreach (KeyValuePair<int, bool> entry in cache)
			{
				if (entry.Value)
				{
					matched++;
				}
			}

			return Settings.Include.Count + " name overrides, " + matched + " of " + cache.Count
				+ " blocks seen were narrow";
		}

		/// <summary>The full pattern lists, printed by <c>sb blocks</c>.</summary>
		internal static string Describe()
		{
			return "Always narrow: " + string.Join(", ", Settings.Include.ToArray())
				+ "\r\nNever: " + string.Join(", ", Settings.Exclude.ToArray())
				+ "\r\nOtherwise by bounds: " + Status();
		}
	}
}
