using System.Collections.Generic;
using UnityEngine;

namespace Stumblr
{
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
	/// A tire without <c>movement</c> in its Collide list - the small flat tire - is walked
	/// through, so the zombie's feet share its block column; that is the one that fires. The
	/// bigger tires are solid and get walked around, and arm harmlessly.
	///
	/// One roll per zombie per tire, so a zombie standing in a tire is not re-rolled every tick.
	/// </summary>
	internal static class TripHazards
	{
		private sealed class Hazard
		{
			internal float PlacedAt;

			internal string Name;

			/// <summary>Entity ids that have already rolled against this tire.</summary>
			internal readonly HashSet<int> Rolled = new HashSet<int>();
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

			hazards[_blockPos] = new Hazard { PlacedAt = Time.time, Name = __instance.blockName };
			Counters.TiresPlaced++;
			LastTire = __instance.blockName + " placed at " + _blockPos;
		}

		internal static void OnBlockRemovedPostfix(Vector3i _blockPos)
		{
			if (hazards.Count > 0)
			{
				hazards.Remove(_blockPos);
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
			// one below, for a tire aligned down onto uneven terrain.
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

			Counters.TireSteps++;
			LastTire = __instance.EntityName + " stepped into " + hazard.Name + " at " + at + ", "
				+ Format.Seconds(Time.time - hazard.PlacedAt) + " after it was placed";

			if (Settings.ZombieMode == ZombieReaction.Off || Settings.TireChance <= 0f
				|| __instance.rand.RandomFloat >= Settings.TireChance)
			{
				return;
			}

			Counters.TireTrips++;
			ZombieTrip.Apply(__instance);
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
				hazards.Remove(_pos);
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

		/// <summary>Tires still inside their window right now, for <c>sb info</c>.</summary>
		internal static int LiveCount()
		{
			int live = 0;
			foreach (KeyValuePair<Vector3i, Hazard> entry in hazards)
			{
				if (Time.time - entry.Value.PlacedAt <= Settings.TireSeconds)
				{
					live++;
				}
			}
			return live;
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
	}
}
