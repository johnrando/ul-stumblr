using UnityEngine;

namespace Stumblr
{
	/// <summary>
	/// What a tripping player gets. Feedback, and nothing else.
	///
	/// This is the design constraint of the whole mod rather than an unfinished effect list. A stun,
	/// a slowdown, a stagger, a dropped weapon or a nudged aim all mean the same thing when there
	/// are zombies behind you, and it is not "mildly inconvenient". So a trip takes no health, no
	/// stamina, applies no buff, and changes neither movement nor aim - and the player always clears
	/// the obstacle, because being left on the wrong side of a fence is the most lethal outcome of
	/// all. What is left is the two things that tell you it happened.
	///
	/// In the order the player experiences it: the grunt, then the jolt.
	/// </summary>
	internal static class PlayerTrip
	{
		internal static void Apply(EntityPlayerLocal _player)
		{
			TripSound.Play(_player);
			Shake(_player);
		}

		private static void Shake(EntityPlayerLocal _player)
		{
			if (!Settings.ShakeCamera)
			{
				return;
			}

			GameManager gameManager = GameManager.Instance;
			if (!gameManager)
			{
				return;
			}

			// The same coroutine vanilla uses for damage and for EnumCameraShake, whose own sizes are
			// 5 for Tiny, 10 for Small and 20 for Big. A trip is a Tiny.
			//
			// Vector3.one is what vanilla passes for a shake with no direction to it, and a stumble
			// has none - you did not get hit from somewhere.
			gameManager.StartCoroutine(_player.shakeCamera(Vector3.one, Settings.ShakeSeconds,
				Settings.ShakeStrength));
		}

		/// <summary>Just the mode, for the <c>sb</c> settings block.</summary>
		internal static string ShakeStatus()
		{
			return Settings.ShakeCamera ? "on" : "off";
		}

		/// <summary>The line <c>sb shake</c> prints after toggling.</summary>
		internal static string DescribeShake()
		{
			return Settings.ShakeCamera
				? "Trip shake ON - a " + Settings.ShakeSeconds + "s jolt at strength "
					+ Settings.ShakeStrength + ", vanilla's own Tiny. Your aim is not moved."
				: "Trip shake OFF - the view is left alone.";
		}
	}
}
