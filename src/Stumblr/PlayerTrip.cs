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
	/// all. What is left is the three things that tell you it happened.
	///
	/// Deliberately two visual channels rather than one. They fail differently: the roll is a camera
	/// motion and reads at a glance, the weapon jolt is drawn in the player's hands and cannot be
	/// affected by anything that touches the camera. Either alone is easy to miss while sprinting.
	///
	/// In the order the player experiences it: the grunt, the lurch, the hands.
	/// </summary>
	internal static class PlayerTrip
	{
		internal static void Apply(EntityPlayerLocal _player)
		{
			TripSound.Play(_player);
			Roll(_player);
			Jolt(_player);
		}

		/// <summary>
		/// A one-shot roll force: the horizon tips and springs back upright on its own.
		///
		/// This replaced a vp_FPCamera shake, and it is better on both counts that matter. It reads
		/// as tripping rather than as a generic rattle, because a stumble tips you over rather than
		/// vibrating you. And it genuinely does not move your aim: AddRollForce drives the rotation
		/// spring's z axis only, whereas the shake wrote m_Yaw and m_Pitch directly - transiently,
		/// but that is where the crosshair points.
		///
		/// Vanilla's own DoBomb, the explosion knock, uses 1 to 2. A trip sits just under that.
		/// </summary>
		private static void Roll(EntityPlayerLocal _player)
		{
			if (!Settings.RollCamera)
			{
				return;
			}

			vp_FPCamera camera = _player.vp_FPCamera;
			if (!camera)
			{
				return;
			}

			camera.AddRollForce(Signed(_player, Settings.RollForce));
		}

		/// <summary>
		/// A kick to the held item and the hands holding it, using the same spring a gunshot's
		/// recoil drives. Nothing about the camera is touched, which is the point: this is the
		/// channel that still reads if the view effects are turned down or modded away.
		///
		/// For scale, a gunshot's recoil is a positional (0, 0, -0.035) and a rotational
		/// (-10, 0, 0) degrees. A stumble is a bit larger and sideways-biased, so it reads as a
		/// lurch rather than as having fired something.
		/// </summary>
		private static void Jolt(EntityPlayerLocal _player)
		{
			if (!Settings.JoltWeapon)
			{
				return;
			}

			vp_FPWeapon weapon = _player.vp_FPWeapon;
			if (!weapon)
			{
				return;
			}

			// Randomised sideways sign so consecutive trips do not look identical. The downward and
			// backward components keep their sign - hands drop on a stumble, they do not rise.
			float side = Signed(_player, 1f);

			weapon.AddForce(
				new Vector3(Settings.JoltPosition.x * side, Settings.JoltPosition.y,
					Settings.JoltPosition.z),
				new Vector3(Settings.JoltRotation.x, Settings.JoltRotation.y * side,
					Settings.JoltRotation.z * side));
		}

		/// <summary>The magnitude, as likely to tip one way as the other.</summary>
		private static float Signed(EntityPlayerLocal _player, float _magnitude)
		{
			return _player.rand.RandomFloat < 0.5f ? 0f - _magnitude : _magnitude;
		}

		/// <summary>Just the mode, for the <c>sb</c> settings block.</summary>
		internal static string RollStatus()
		{
			return Settings.RollCamera ? "on" : "off";
		}

		/// <summary>The line <c>sb shake</c> prints after toggling.</summary>
		internal static string DescribeRoll()
		{
			return Settings.RollCamera
				? "Trip lurch ON - the horizon tips at force " + Settings.RollForce
					+ " and rights itself. Roll only, so your aim is not moved."
				: "Trip lurch OFF - the view is left alone.";
		}

		/// <summary>The line <c>sb jolt</c> prints after toggling.</summary>
		internal static string DescribeJolt()
		{
			return Settings.JoltWeapon
				? "Weapon jolt ON - your held item kicks, on the same spring gun recoil uses. "
					+ "Nothing about the camera is touched, so this reads even with the lurch off."
				: "Weapon jolt OFF - your hands are left alone.";
		}
	}
}
