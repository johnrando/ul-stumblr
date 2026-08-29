using UnityEngine;

namespace Stumblr
{
	/// <summary>
	/// Decides whether the local player's jump crossed something trippable, and reacts on landing.
	///
	/// Two hooks, because the two halves of the question are answered at different moments. The
	/// take-off position is only knowable at take-off, and a stumble only makes sense on the landing,
	/// so <c>EntityPlayer.StartJumpMotion</c> records where the jump began and
	/// <c>EntityAlive.EndJump</c> decides what it crossed.
	///
	/// The obstacle is found by walking the voxels along the horizontal line between the two, rather
	/// than by probing forward at take-off. That is what makes this actually answer "did I go over a
	/// fence" - it covers a diagonal hop, it does not care which way the camera was facing, and it
	/// needs no raycast. Zombies get this for free from the move helper's own HitInfo; the player has
	/// no equivalent, so this is the player's version of it.
	/// </summary>
	internal static class PlayerJumpTrigger
	{
		/// <summary>Where the local player's current jump started. Only one player is local.</summary>
		private static Vector3 takeOff;

		private static bool hasTakeOff;

		/// <summary>When the last trip fired, against <c>Time.time</c>.</summary>
		private static float lastTrip = float.NegativeInfinity;

		/// <summary>How far along the jump line to look, in voxels.</summary>
		private const int MaxSteps = 8;

		/// <summary>Postfix on <c>EntityPlayer.StartJumpMotion</c>.</summary>
		internal static void RecordTakeOff(EntityPlayer __instance)
		{
			if (!(__instance is EntityPlayerLocal))
			{
				return;
			}

			takeOff = __instance.position;
			hasTakeOff = true;
		}

		/// <summary>
		/// Postfix on <c>EntityAlive.EndJump</c>. That method is virtual and EntityHuman overrides
		/// it, so this fires for zombies too - hence the local-player gate first thing.
		/// </summary>
		internal static void Postfix(EntityAlive __instance)
		{
			if (!Settings.Enabled)
			{
				return;
			}

			if (!(__instance is EntityPlayerLocal player))
			{
				return;
			}

			// Consume the take-off either way: a landing without one recorded (spawning in mid-air,
			// stepping off a ledge) is not a jump we can measure.
			bool hadTakeOff = hasTakeOff;
			Vector3 from = takeOff;
			hasTakeOff = false;

			Counters.JumpsSeen++;
			Counters.PlayerJumps++;

			if (!hadTakeOff)
			{
				return;
			}

			// Undead Legacy adds a double jump, which can raise this hook twice for one crossing.
			if (Time.time - lastTrip < Settings.PlayerCooldownSeconds)
			{
				return;
			}

			World world = player.world;
			Vector3 to = player.position;

			// Both ends, not just one: hopping a railing to drop off a roof starts at ground level
			// and is the case where a stumble would feel worst.
			if (!GroundLevel.IsNearGround(world, from) || !GroundLevel.IsNearGround(world, to))
			{
				return;
			}

			Counters.PlayerNearGround++;

			if (!CrossedTrippable(world, from, to))
			{
				return;
			}

			Counters.PlayerOverTrippable++;

			if (player.rand.RandomFloat * 100f >= TripChance.ForPlayer(player))
			{
				return;
			}

			Counters.PlayerTrips++;
			lastTrip = Time.time;
			PlayerTrip.Apply(player);
		}

		/// <summary>
		/// Whether any block on the horizontal line from take-off to landing is trippable, checked at
		/// foot level and one above it - a fence is a block tall and the feet clear it, so the block
		/// you actually went over is usually the upper of the two.
		/// </summary>
		private static bool CrossedTrippable(World _world, Vector3 _from, Vector3 _to)
		{
			if (_world == null)
			{
				return false;
			}

			Vector3 span = _to - _from;
			span.y = 0f;

			float distance = span.magnitude;
			int steps = Mathf.Clamp(Mathf.CeilToInt(distance) + 1, 1, MaxSteps);

			// The lower of the two ends: a fence crossed on the way up sits at the take-off's feet,
			// one crossed on the way down at the landing's.
			int footY = Utils.Fastfloor(Mathf.Min(_from.y, _to.y));

			int lastX = int.MinValue;
			int lastZ = int.MinValue;

			for (int i = 0; i <= steps; i++)
			{
				Vector3 at = _from + span * ((float)i / steps);
				int x = Utils.Fastfloor(at.x);
				int z = Utils.Fastfloor(at.z);

				// The line usually lands in the same column twice running; only look once.
				if (x == lastX && z == lastZ)
				{
					continue;
				}
				lastX = x;
				lastZ = z;

				if (IsTrippableAt(_world, x, footY, z) || IsTrippableAt(_world, x, footY + 1, z))
				{
					return true;
				}
			}

			return false;
		}

		private static bool IsTrippableAt(World _world, int _x, int _y, int _z)
		{
			BlockValue blockValue = _world.GetBlock(_x, _y, _z);
			return !blockValue.isair && TripBlocks.IsTrippable(blockValue.Block);
		}
	}
}
