using System.Collections.Generic;
using UnityEngine;

namespace Stumblr
{
	/// <summary>
	/// Times each zombie's landings, and settles leg hits taken in the air, by postfixing
	/// <c>EntityAlive.EndJump</c>.
	///
	/// The window is two-sided. A hit in the half second after the feet touch is checked on the
	/// spot by <see cref="LegHitTrigger"/>. A hit in the half second *before* - a player quick
	/// enough to swing while the zombie is still coming down - cannot be judged then, because the
	/// zombie is not standing on anything yet. So the hit is parked here with its chance, and the
	/// landing decides: if the zombie comes down on a narrow block, the trip plays at the moment it
	/// lands, which is also where it reads best.
	///
	/// EndJump is virtual and EntityHuman overrides it with a call to base, so this fires for every
	/// zombie rig. It also fires for the player, hence the flag check. A postfix coexists with
	/// Undead Legacy's own prefix on the same method (H_EntityPlayerLocal, which returns true).
	///
	/// Both tables are keyed on entity id rather than holding the entity, so a zombie that unloads
	/// between the two events leaves a stale int rather than a reference to a dead object.
	/// </summary>
	internal static class ZombieLanding
	{
		private struct AirHit
		{
			internal float Time;

			internal float Chance;
		}

		private static readonly Dictionary<int, float> landedAt = new Dictionary<int, float>();

		private static readonly Dictionary<int, AirHit> airHits = new Dictionary<int, AirHit>();

		/// <summary>Drop a table once it passes this many zombies.</summary>
		private const int PruneAbove = 64;

		internal static void Postfix(EntityAlive __instance)
		{
			if (!Settings.Enabled || __instance == null)
			{
				return;
			}

			// The entityFlags bit rather than `is EntityZombie`, because it comes from
			// entityclasses.xml: it covers zombie dogs and Undead Legacy's own zombies for free, and
			// excludes bandits and the player.
			if ((__instance.entityFlags & EntityFlags.Zombie) == EntityFlags.None)
			{
				return;
			}

			Counters.Landings++;

			// A zombie that dies or unloads is never removed, so the tables would creep upward over
			// a long session. Dropping the lot costs at most a handful of missed windows.
			if (landedAt.Count > PruneAbove)
			{
				landedAt.Clear();
			}

			int id = __instance.entityId;
			landedAt[id] = Time.time;

			// A leg hit taken on the way down is judged now, on whatever it landed on.
			if (airHits.TryGetValue(id, out AirHit hit))
			{
				airHits.Remove(id);
				if (Time.time - hit.Time <= Settings.WindowSeconds)
				{
					Counters.AirHitsLanded++;
					LegHitTrigger.TryTrip(__instance, hit.Chance);
				}
			}
		}

		/// <summary>
		/// Park a leg hit taken mid-jump until the landing. A second hit in the same jump replaces
		/// the first, so the freshest swing is the one that counts.
		/// </summary>
		internal static void RecordAirHit(int _entityId, float _chance)
		{
			if (airHits.Count > PruneAbove)
			{
				airHits.Clear();
			}

			airHits[_entityId] = new AirHit { Time = Time.time, Chance = _chance };
		}

		/// <summary>
		/// Whether this zombie landed within the last <paramref name="_seconds"/>. A window of zero
		/// or less means the check is off and every zombie qualifies.
		/// </summary>
		internal static bool LandedWithin(int _entityId, float _seconds)
		{
			if (_seconds <= 0f)
			{
				return true;
			}

			return landedAt.TryGetValue(_entityId, out float at) && Time.time - at <= _seconds;
		}

		/// <summary>The <c>sb</c> menu line.</summary>
		internal static string Status()
		{
			if (Settings.WindowSeconds <= 0f)
			{
				return "any time it is standing on one";
			}

			return "leg hit within " + Format.Seconds(Settings.WindowSeconds)
				+ " either side of landing";
		}
	}
}
