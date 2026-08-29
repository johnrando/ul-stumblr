using System;
using System.Reflection;
using HarmonyLib;

namespace Stumblr
{
	/// <summary>
	/// Installs the mod's three Harmony patches. Each is resolved late and gated on its own target,
	/// so a game update that moves one degrades to a log line naming the behaviour that is therefore
	/// missing, rather than an exception during mod init. The zombie hook and the two player hooks
	/// are independent - either half can fail without taking the other down.
	///
	/// Load order needs no declaration. Undead Legacy applies its own patches as a BepInEx plugin
	/// roughly a second before the game calls any IModApi.InitMod, so by the time this runs both are
	/// fully present regardless of mod folder ordering.
	///
	/// The one thing that does need care is which method the zombie hook targets. UL replaces
	/// EntityMoveHelper.UpdateMoveHelper wholesale - a prefix with fourteen `return false` and no
	/// `return true` - so a patch there would never run under UL. UL does not touch
	/// EntityMoveHelper.StartJump, and its replacement calls it, which is why that is the target.
	/// See ZombieJumpTrigger, and ../ul-decomp/CLAUDE.md for the full analysis.
	/// </summary>
	internal static class Patches
	{
		internal const string LogPrefix = "[Stumblr] ";

		private const string HarmonyId = "Stumblr";

		private const string NotRunYet = "not applied - mod init has not run";

		/// <summary>Outcome of each patch, as reported by <c>sb info</c>.</summary>
		internal static string ZombieJumpHookStatus = NotRunYet;

		internal static string PlayerJumpHookStatus = NotRunYet;

		internal static string PlayerLandHookStatus = NotRunYet;

		private static bool applied;

		internal static void Apply()
		{
			if (applied)
			{
				return;
			}
			applied = true;
			try
			{
				ApplyPatches();
			}
			catch (Exception e)
			{
				Log.Error(LogPrefix + "Failed to apply patches; nothing will ever trip.");
				Log.Exception(e);
			}
		}

		private static void ApplyPatches()
		{
			UndeadLegacyInfo.Report();

			Harmony harmony = new Harmony(HarmonyId);
			ApplyZombieJumpHook(harmony);
			ApplyPlayerJumpHook(harmony);
			ApplyPlayerLandHook(harmony);
		}

		/// <summary>
		/// The zombie half. Without it no zombie is ever caught on a fence.
		/// </summary>
		private static void ApplyZombieJumpHook(Harmony _harmony)
		{
			MethodInfo target = AccessTools.DeclaredMethod(typeof(EntityMoveHelper), "StartJump");
			if (target == null)
			{
				ZombieJumpHookStatus = "NOT APPLIED - EntityMoveHelper.StartJump not found";
				Log.Error(LogPrefix + "Zombie jump hook NOT applied: EntityMoveHelper.StartJump could "
					+ "not be found, so zombies will never be caught on a fence.");
				return;
			}

			_harmony.Patch(target, prefix: new HarmonyMethod(
				AccessTools.DeclaredMethod(typeof(ZombieJumpTrigger), nameof(ZombieJumpTrigger.Prefix))));

			ZombieJumpHookStatus = "applied - prefix on EntityMoveHelper.StartJump";
			Log.Out(LogPrefix + "Zombie jump hook applied: a zombie jumping a low obstacle can now "
				+ "catch a foot on it.");
		}

		/// <summary>
		/// Half the player pair: records where a jump began. Without it the landing hook has nothing
		/// to measure and no player ever trips.
		///
		/// EntityAlive.StartJump, and not EntityPlayer.StartJumpMotion, which was the first attempt
		/// and never fired. The local player's jump is driven by UFPS, not by EntityAlive's own
		/// JumpState machine: EntityPlayerLocal.OnUpdateLive sets Jumping = true and later calls
		/// EndJump() directly, so UpdateJump - the only caller of StartJumpMotion - never runs for
		/// the player. The Jumping setter does call StartJump, so this fires for both the player and
		/// zombies; RecordTakeOff filters to the local player itself.
		/// </summary>
		private static void ApplyPlayerJumpHook(Harmony _harmony)
		{
			MethodInfo target = AccessTools.DeclaredMethod(typeof(EntityAlive), "StartJump");
			if (target == null)
			{
				PlayerJumpHookStatus = "NOT APPLIED - EntityAlive.StartJump not found";
				Log.Error(LogPrefix + "Player jump hook NOT applied: EntityAlive.StartJump could not "
					+ "be found, so the landing hook has no take-off to measure from and the player "
					+ "will never trip.");
				return;
			}

			_harmony.Patch(target, postfix: new HarmonyMethod(
				AccessTools.DeclaredMethod(typeof(PlayerJumpTrigger),
					nameof(PlayerJumpTrigger.RecordTakeOff))));

			PlayerJumpHookStatus = "applied - postfix on EntityAlive.StartJump";
			Log.Out(LogPrefix + "Player jump hook applied: take-off positions are now recorded.");
		}

		/// <summary>
		/// The other half: decides on landing. A postfix, which coexists safely with Undead Legacy's
		/// own prefix on the same method (H_EntityPlayerLocal, which returns true and fires
		/// onSelfLandJump).
		/// </summary>
		private static void ApplyPlayerLandHook(Harmony _harmony)
		{
			MethodInfo target = AccessTools.DeclaredMethod(typeof(EntityAlive), "EndJump");
			if (target == null)
			{
				PlayerLandHookStatus = "NOT APPLIED - EntityAlive.EndJump not found";
				Log.Error(LogPrefix + "Player landing hook NOT applied: EntityAlive.EndJump could not "
					+ "be found, so the player will never trip.");
				return;
			}

			_harmony.Patch(target, postfix: new HarmonyMethod(
				AccessTools.DeclaredMethod(typeof(PlayerJumpTrigger), nameof(PlayerJumpTrigger.Postfix))));

			PlayerLandHookStatus = "applied - postfix on EntityAlive.EndJump";
			Log.Out(LogPrefix + "Player landing hook applied: jumping a fence can now cost you your "
				+ "footing.");
		}
	}
}
