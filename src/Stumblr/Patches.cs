using System;
using System.Reflection;
using HarmonyLib;

namespace Stumblr
{
	/// <summary>
	/// Installs the mod's two Harmony patches. Each is resolved late and gated on its own target,
	/// so a game update that moves one degrades to a log line naming the behaviour that is therefore
	/// missing, rather than an exception during mod init.
	///
	/// Load order needs no declaration. Undead Legacy applies its own patches as a BepInEx plugin
	/// roughly a second before the game calls any IModApi.InitMod, so by the time this runs both are
	/// fully present regardless of mod folder ordering.
	///
	/// The one thing that does need care is which method the damage hook targets. UL replaces
	/// EntityAlive.ProcessDamageResponseLocal wholesale - a prefix that returns false - so a patch
	/// there would never run under UL. UL does not touch EntityAlive.DamageEntity, which is why that
	/// is the target. See LegHitTrigger, and the sibling ul-decomp project for the toolchain.
	/// </summary>
	internal static class Patches
	{
		internal const string LogPrefix = "[Stumblr] ";

		private const string HarmonyId = "Stumblr";

		private const string NotRunYet = "not applied - mod init has not run";

		/// <summary>Outcome of each patch, as reported by <c>sb info</c>.</summary>
		internal static string LandingHookStatus = NotRunYet;

		internal static string LegHitHookStatus = NotRunYet;

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
			ApplyLandingHook(harmony);
			ApplyLegHitHook(harmony);
		}

		/// <summary>
		/// Records when each zombie lands. Without it the window check never passes and, unless the
		/// window is set to 0, no zombie ever trips. A postfix, which coexists safely with Undead
		/// Legacy's own prefix on the same method (H_EntityPlayerLocal, which returns true).
		/// </summary>
		private static void ApplyLandingHook(Harmony _harmony)
		{
			MethodInfo target = AccessTools.DeclaredMethod(typeof(EntityAlive), "EndJump");
			if (target == null)
			{
				LandingHookStatus = "NOT APPLIED - EntityAlive.EndJump not found";
				Log.Error(LogPrefix + "Landing hook NOT applied: EntityAlive.EndJump could not be "
					+ "found, so no landing is ever recorded and the trip window never opens.");
				return;
			}

			_harmony.Patch(target, postfix: new HarmonyMethod(
				AccessTools.DeclaredMethod(typeof(ZombieLanding), nameof(ZombieLanding.Postfix))));

			LandingHookStatus = "applied - postfix on EntityAlive.EndJump";
			Log.Out(LogPrefix + "Landing hook applied: zombie landings are now timed.");
		}

		/// <summary>
		/// The hit itself. Without it nothing ever trips.
		/// </summary>
		private static void ApplyLegHitHook(Harmony _harmony)
		{
			MethodInfo target = AccessTools.DeclaredMethod(typeof(EntityAlive), "DamageEntity",
				new[] { typeof(DamageSource), typeof(int), typeof(bool), typeof(float) });
			if (target == null)
			{
				LegHitHookStatus = "NOT APPLIED - EntityAlive.DamageEntity not found";
				Log.Error(LogPrefix + "Leg hit hook NOT applied: EntityAlive.DamageEntity(DamageSource, "
					+ "int, bool, float) could not be found, so nothing will ever trip.");
				return;
			}

			_harmony.Patch(target, postfix: new HarmonyMethod(
				AccessTools.DeclaredMethod(typeof(LegHitTrigger), nameof(LegHitTrigger.Postfix))));

			LegHitHookStatus = "applied - postfix on EntityAlive.DamageEntity";
			Log.Out(LogPrefix + "Leg hit hook applied: a zombie perched on a fence can now be tripped "
				+ "with a hit to the leg.");
		}
	}
}
