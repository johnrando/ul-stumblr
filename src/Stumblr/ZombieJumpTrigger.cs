using System.Collections.Generic;

namespace Stumblr
{
	/// <summary>
	/// Catches a zombie about to jump a low obstacle, by prefixing
	/// <c>EntityMoveHelper.StartJump</c> - the single method all four of the move helper's jump
	/// decisions funnel through.
	///
	/// StartJump and not UpdateMoveHelper, and that is the whole compatibility story. Undead
	/// Legacy's H_ZombieDiggingPatch is a prefix on UpdateMoveHelper with fourteen `return false`
	/// and no `return true`: it reimplements zombie movement from scratch and never lets the vanilla
	/// body run. A patch on UpdateMoveHelper would therefore be dead code the moment UL is
	/// installed. UL does not patch StartJump, and its replacement calls it at four sites, so this
	/// hook fires under both.
	///
	/// A prefix returning false, because a caught zombie is one that never left the ground.
	/// </summary>
	internal static class ZombieJumpTrigger
	{
		/// <summary>
		/// Entities whose jump was allowed to proceed but which are due to go down when they land.
		/// Keyed on entity id rather than holding the entity, so an unload between take-off and
		/// landing leaves a stale int rather than a reference to a dead object.
		/// </summary>
		private static readonly HashSet<int> pendingFarSide = new HashSet<int>();

		/// <summary>Drop the pending set once it passes this many zombies.</summary>
		private const int PruneAbove = 64;

		/// <summary>
		/// Whether this entity was marked for a far-side trip, clearing the mark. Called from the
		/// landing hook.
		/// </summary>
		internal static bool ConsumeFarSide(int _entityId)
		{
			return pendingFarSide.Remove(_entityId);
		}

		internal static bool Prefix(EntityMoveHelper __instance)
		{
			if (!Settings.Enabled)
			{
				return true;
			}

			EntityAlive entity = __instance.entity;
			if (entity == null)
			{
				return true;
			}

			// StartJump's own guard. Without it a "trip" could fire on a call that was never going to
			// produce a jump - a zombie already airborne, or one being electrocuted.
			if (entity.Jumping || !entity.onGround || entity.Electrocuted)
			{
				return true;
			}

			Counters.JumpsSeen++;

			// The entityFlags bit rather than `is EntityZombie`, because it comes from
			// entityclasses.xml: it covers zombie dogs and Undead Legacy's own zombies for free, and
			// excludes bandits.
			if ((entity.entityFlags & EntityFlags.Zombie) == EntityFlags.None)
			{
				return true;
			}

			// Crawlers are excluded by vanilla's own stumble path (walkType 21), which has nothing to
			// play for a rig that is already on the floor.
			if (entity.walkType == 21)
			{
				return true;
			}

			Counters.ZombieJumps++;

			if (!GroundLevel.IsNearGround(entity.world, entity.position))
			{
				return true;
			}

			Counters.ZombieNearGround++;

			// BlockedFlags bit 0 is the low ray - blocked at foot level with nothing at head height,
			// which is exactly a fence, a railing or a car bonnet. A wall trips bit 1 as well and is not
			// something you catch a foot on. Both this and HitInfo are already computed by
			// CheckWorldBlocked on the ticks before the jump, so they cost nothing to read.
			if (__instance.BlockedFlags != 1 || !__instance.HitInfo.bHitValid
				|| !TripBlocks.IsTrippable(__instance.HitInfo.hit.blockValue.Block))
			{
				return true;
			}

			Counters.ZombieOverTrippable++;

			// Checked here rather than up front so the counters above still move with trips switched
			// off - which is what makes "the hook is live but nothing is being caught" diagnosable.
			if (Settings.ZombieMode == ZombieReaction.Off || Settings.ZombieChance <= 0f)
			{
				return true;
			}

			if (entity.rand.RandomFloat * 100f >= Settings.ZombieChance)
			{
				return true;
			}

			Counters.ZombieTrips++;

			// Near side or far. Catching it cancels the jump outright; the alternative is to let it
			// go over and take its legs out on the landing, which is the difference between a zombie
			// that fumbles at the fence and one that clears it and sprawls.
			if (entity.rand.RandomFloat * 100f >= Settings.ZombieCatchPercent)
			{
				// A zombie that unloads or dies mid-jump never reaches the landing hook to have its
				// mark consumed, so the set would creep upward over a long session. There is no
				// harm in dropping the lot: the worst case is a handful of airborne zombies landing
				// on their feet.
				if (pendingFarSide.Count > PruneAbove)
				{
					pendingFarSide.Clear();
				}

				pendingFarSide.Add(entity.entityId);
				Counters.ZombieFarSide++;
				return true;
			}

			Counters.ZombieNearSide++;
			ZombieTrip.Apply(entity);

			// Skip the original: the jump never happens, so the zombie is left on this side of the
			// fence.
			return false;
		}
	}
}
