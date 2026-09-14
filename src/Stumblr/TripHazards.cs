using System.Collections.Generic;
using UnityEngine;

namespace Stumblr
{
	/// <summary>Which of the four tire kinds a placed block is; see <see cref="TripHazards.Classify"/>.</summary>
	internal enum TireKind
	{
		/// <summary>The donut spare - decoCarTireSmallFlat and Undead Legacy's _S tires.</summary>
		Small,

		/// <summary>One full-size tire lying flat.</summary>
		Single,

		/// <summary>Several tires lying in a heap - decoCarTirePile, two blocks wide.</summary>
		Pile,

		/// <summary>A vertical stack, which nobody trips on. Off by default.</summary>
		Stack
	}

	/// <summary>One kind's rule: what it does to the base chance, and how many zombies it can trip.</summary>
	internal struct TireRule
	{
		/// <summary>Multiplier on <see cref="Settings.TireChance"/>. 0 means this kind never arms.</summary>
		internal float Multiplier;

		/// <summary>Zombies one tire of this kind can trip before it is spent. 0 means never arms.</summary>
		internal int MaxTrips;

		internal TireRule(float _multiplier, int _maxTrips)
		{
			Multiplier = _multiplier;
			MaxTrips = _maxTrips;
		}

		internal bool Arms
		{
			get { return Multiplier > 0f && MaxTrips > 0; }
		}
	}

	/// <summary>
	/// Tires as trip hazards. A tire a player puts down stays armed for a few seconds; a zombie
	/// that steps into it in that time can go down.
	///
	/// Three hooks. <c>Block.OnBlockAdded</c> is where a placed block reaches the world on the
	/// authoritative side, and its <c>_addedByPlayer</c> argument is non-null exactly when a player
	/// placed it (Chunk.SetBlock resolves it from the changing entity id), which is what keeps
	/// generated tires in a car park from arming themselves. <c>Block.OnBlockRemoved</c> disarms one
	/// that is picked back up or destroyed. <c>EntityAlive.OnUpdateLive</c> is the per-zombie tick
	/// that looks under its feet; it returns on the first line whenever no tire is armed, which is
	/// nearly always.
	///
	/// Under Undead Legacy every tire block carries UL's <c>ULM_Decor</c> class, which defaults
	/// <c>CanPickup</c> to true, so a tire is picked up from any wreck and put down in a zombie's
	/// path, or at its feet. Neither class overrides the two Block methods patched here. Vanilla
	/// without UL has no way to obtain one; the rule is simply never reached.
	///
	/// Which tire matters. The small flat tire has no <c>movement</c> in its Collide list, so a
	/// zombie walks through it and its feet share the tire's block column: that one fires as the
	/// zombie passes. The full-size tire and the pile are solid, so a zombie walks around them, or
	/// steps up onto them when they are in its path - and the block under its feet is then the
	/// tire, which is the other way in. A pile is two blocks wide and both columns are armed. Each
	/// kind has its own multiplier on the chance and its own cap on how many zombies it can trip
	/// before it is spent; a vertical stack is nothing to trip on and is off by default.
	///
	/// One roll per zombie per tire, so a zombie standing in a tire is not re-rolled every tick.
	/// </summary>
	internal static class TripHazards
	{
		private sealed class Hazard
		{
			internal float PlacedAt;

			internal string Name;

			internal TireKind Kind;

			/// <summary>Every block column the tire occupies; a pile has two.</summary>
			internal readonly List<Vector3i> Footprint = new List<Vector3i>();

			/// <summary>Entity ids that have already rolled against this tire.</summary>
			internal readonly HashSet<int> Rolled = new HashSet<int>();

			internal int Trips;
		}

		private static readonly Dictionary<Vector3i, Hazard> hazards = new Dictionary<Vector3i, Hazard>();

		private static readonly List<Vector3i> expired = new List<Vector3i>();

		/// <summary>The last tire placed or stepped in, for <c>sb info</c>.</summary>
		internal static string LastTire = "no tire placed yet";

		internal static void OnBlockAddedPostfix(Block __instance, Vector3i _blockPos, BlockValue _blockValue,
			PlatformUserIdentifierAbs _addedByPlayer)
		{
			if (!Settings.Enabled || Settings.TireSeconds <= 0f || _addedByPlayer == null
				|| __instance == null || _blockValue.ischild)
			{
				return;
			}

			if (!NarrowBlocks.Matches(__instance.blockName, Settings.TirePatterns, out _))
			{
				return;
			}

			Prune();

			TireKind kind = Classify(__instance);
			if (!Settings.TireRule(kind).Arms)
			{
				LastTire = __instance.blockName + " placed at " + _blockPos + " - " + Name(kind)
					+ " tires do not arm";
				return;
			}

			Hazard hazard = new Hazard
			{
				PlacedAt = Time.time,
				Name = __instance.blockName,
				Kind = kind
			};
			foreach (Vector3i pos in Footprint(__instance, _blockPos, _blockValue))
			{
				hazard.Footprint.Add(pos);
				hazards[pos] = hazard;
			}

			Counters.TiresPlaced++;
			LastTire = __instance.blockName + " (" + Name(kind) + ") placed at " + _blockPos;
		}

		/// <summary>
		/// The parent position plus every child's, for a multi-block tire. The offsets are rotated
		/// the way the game rotates them when it places the children, so a pile turned sideways
		/// arms the right two columns.
		/// </summary>
		private static IEnumerable<Vector3i> Footprint(Block _block, Vector3i _blockPos, BlockValue _blockValue)
		{
			yield return _blockPos;

			if (!_block.isMultiBlock || _block.multiBlockPos == null)
			{
				yield break;
			}

			for (int i = 0; i < _block.multiBlockPos.Length; i++)
			{
				Vector3i offset = _block.multiBlockPos.Get(i, _blockValue.type, _blockValue.rotation);
				if (offset != Vector3i.zero)
				{
					yield return _blockPos + offset;
				}
			}
		}

		/// <summary>
		/// Which kind a tire block is, from its name and shape. Undead Legacy's blocks are named
		/// for their model - ulmDecoTire3_S is a small one, ulmDecoTires2_1 two in a heap,
		/// ulmDecoTires4_1 four in a column two blocks tall - and vanilla's say it in words.
		/// The multi-block height catches any stack whatever it is called.
		/// </summary>
		internal static TireKind Classify(Block _block)
		{
			string name = _block.blockName.ToLowerInvariant();

			if (_block.isMultiBlock && _block.multiBlockPos != null && _block.multiBlockPos.dim.y > 1)
			{
				return TireKind.Stack;
			}
			if (name.Contains("stack") || name.Contains("tires4"))
			{
				return TireKind.Stack;
			}
			if (name.Contains("pile") || name.Contains("tires2"))
			{
				return TireKind.Pile;
			}
			if (name.Contains("small") || name.EndsWith("_s"))
			{
				return TireKind.Small;
			}
			return TireKind.Single;
		}

		internal static void OnBlockRemovedPostfix(Vector3i _blockPos)
		{
			if (hazards.Count > 0 && hazards.TryGetValue(_blockPos, out Hazard hazard))
			{
				Forget(hazard);
			}
		}

		/// <summary>Drops a tire from every column it occupies.</summary>
		private static void Forget(Hazard _hazard)
		{
			for (int i = 0; i < _hazard.Footprint.Count; i++)
			{
				hazards.Remove(_hazard.Footprint[i]);
			}
		}

		internal static void OnUpdateLivePostfix(EntityAlive __instance)
		{
			if (hazards.Count == 0 || !Settings.Enabled)
			{
				return;
			}

			if (!ZombieTrip.CanTrip(__instance))
			{
				return;
			}

			Vector3 pos = __instance.position;

			// The feet block first: a walk-through tire shares the column with the feet. Then the
			// one below, for a solid tire the zombie has stepped up onto, or one aligned down onto
			// uneven terrain.
			Vector3i feet = new Vector3i(Utils.Fastfloor(pos.x), Utils.Fastfloor(pos.y + 0.05f),
				Utils.Fastfloor(pos.z));

			if (!TryHazardAt(feet, out Hazard hazard, out Vector3i at))
			{
				Vector3i below = new Vector3i(feet.x, feet.y - 1, feet.z);
				if (!TryHazardAt(below, out hazard, out at))
				{
					return;
				}
			}

			if (!hazard.Rolled.Add(__instance.entityId))
			{
				return;
			}

			TireRule rule = Settings.TireRule(hazard.Kind);
			float chance = Mathf.Min(1f, Settings.TireChance * rule.Multiplier);

			Counters.TireSteps++;
			LastTire = __instance.EntityName + " stepped into " + hazard.Name + " (" + Name(hazard.Kind)
				+ ") at " + at + ", " + Format.Seconds(Time.time - hazard.PlacedAt)
				+ " after it was placed, " + Format.Percent(chance * 100f) + " to trip";

			if (!ZombieTrip.Active || chance <= 0f || __instance.rand.RandomFloat >= chance)
			{
				return;
			}

			Counters.TireTrips++;
			hazard.Trips++;
			ZombieTrip.Apply(__instance);

			if (hazard.Trips >= rule.MaxTrips)
			{
				LastTire += " - tripped, tire spent after " + hazard.Trips;
				Forget(hazard);
			}
			else
			{
				LastTire += " - tripped, " + (rule.MaxTrips - hazard.Trips) + " more to go";
			}
		}

		/// <summary>A live hazard at this position, or false. An expired one is dropped on sight.</summary>
		private static bool TryHazardAt(Vector3i _pos, out Hazard _hazard, out Vector3i _at)
		{
			_at = _pos;
			if (!hazards.TryGetValue(_pos, out _hazard))
			{
				return false;
			}

			if (Time.time - _hazard.PlacedAt > Settings.TireSeconds)
			{
				Forget(_hazard);
				_hazard = null;
				return false;
			}

			return true;
		}

		/// <summary>Drops every expired tire. Called on each placement, so the table cannot grow
		/// past the tires placed inside one window.</summary>
		private static void Prune()
		{
			expired.Clear();
			foreach (KeyValuePair<Vector3i, Hazard> entry in hazards)
			{
				if (Time.time - entry.Value.PlacedAt > Settings.TireSeconds)
				{
					expired.Add(entry.Key);
				}
			}
			for (int i = 0; i < expired.Count; i++)
			{
				hazards.Remove(expired[i]);
			}
		}

		/// <summary>Tires still inside their window right now, for <c>sb info</c>. A pile counts once.</summary>
		internal static int LiveCount()
		{
			HashSet<Hazard> live = new HashSet<Hazard>();
			foreach (KeyValuePair<Vector3i, Hazard> entry in hazards)
			{
				if (Time.time - entry.Value.PlacedAt <= Settings.TireSeconds)
				{
					live.Add(entry.Value);
				}
			}
			return live.Count;
		}

		internal static string Name(TireKind _kind)
		{
			switch (_kind)
			{
			case TireKind.Small:
				return "small";
			case TireKind.Pile:
				return "pile";
			case TireKind.Stack:
				return "stack";
			default:
				return "single";
			}
		}

		/// <summary><c>sb tires</c> takes the kind by name; null when it is not one.</summary>
		internal static TireKind? Parse(string _name)
		{
			switch (_name.ToLowerInvariant())
			{
			case "small":
				return TireKind.Small;
			case "single":
				return TireKind.Single;
			case "pile":
				return TireKind.Pile;
			case "stack":
				return TireKind.Stack;
			default:
				return null;
			}
		}

		/// <summary>The <c>sb tire</c> menu line.</summary>
		internal static string Status()
		{
			if (Settings.TireSeconds <= 0f)
			{
				return "off - a placed tire is just a tire";
			}

			return "a placed tire trips " + Format.Percent(Settings.TireChance * 100f)
				+ " of zombies stepping in for " + Format.Seconds(Settings.TireSeconds);
		}

		/// <summary>The <c>sb tires</c> menu line: every kind's multiplier and cap.</summary>
		internal static string KindsStatus()
		{
			return RuleStatus(TireKind.Small) + ", " + RuleStatus(TireKind.Single) + ", "
				+ RuleStatus(TireKind.Pile) + ", " + RuleStatus(TireKind.Stack);
		}

		/// <summary>One kind, as the file writes it: multiplier then cap.</summary>
		internal static string RuleValue(TireKind _kind)
		{
			TireRule rule = Settings.TireRule(_kind);
			return Format.Number(rule.Multiplier) + " " + rule.MaxTrips;
		}

		private static string RuleStatus(TireKind _kind)
		{
			TireRule rule = Settings.TireRule(_kind);
			if (!rule.Arms)
			{
				return Name(_kind) + " off";
			}
			return Name(_kind) + " " + Format.Times(rule.Multiplier) + " up to " + rule.MaxTrips;
		}
	}
}
