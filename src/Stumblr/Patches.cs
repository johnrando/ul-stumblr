using System;
using System.Reflection;
using HarmonyLib;

namespace Stumblr
{
	/// <summary>
	/// Installs the mod's Harmony patches. Each is resolved late and gated on its own target, so a
	/// game update that moves one degrades to a log line naming the behaviour that is therefore
	/// missing, rather than an exception during mod init.
	///
	/// Load order needs no declaration. Undead Legacy applies its own patches as a BepInEx plugin
	/// roughly a second before the game calls any IModApi.InitMod, so by the time this runs both are
	/// fully present regardless of mod folder ordering.
	///
	/// The one thing that does need care is which method the damage hook targets. UL replaces
	/// EntityAlive.ProcessDamageResponseLocal wholesale - a prefix that returns false - so a patch
	/// there would never run under UL. UL does not touch EntityAlive.DamageEntity, which is why that
	/// is the target. See LegHitTrigger, and the sibling ul-decomp project for the toolchain. Of the
	/// other targets, UL patches only EntityPlayerLocal.OnUpdateLive (not the EntityAlive base
	/// patched here) and BlockSleepingBag.PlaceBlock (not OnBlockAdded), so none collide.
	/// </summary>
	internal static class Patches
	{
		internal const string LogPrefix = "[Stumblr] ";

		private const string HarmonyId = "Stumblr";

		private const string NotRunYet = "not applied - mod init has not run";

		/// <summary>Outcome of each patch, as reported by <c>sb info</c>.</summary>
		internal static string LandingHookStatus = NotRunYet;

		internal static string LegHitHookStatus = NotRunYet;

		internal static string TireHookStatus = NotRunYet;

		internal static string TickHookStatus = NotRunYet;

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
			FlavorPartners.Resolve();

			Harmony harmony = new Harmony(HarmonyId);
			ApplyLandingHook(harmony);
			ApplyLegHitHook(harmony);
			ApplyTireHooks(harmony);
			ApplyTickHook(harmony);
		}

		/// <summary>
		/// Records when each zombie lands. Without it the window check never passes and, unless the
		/// window is set to 0, no zombie ever trips on a perch. A postfix, which coexists safely with
		/// Undead Legacy's own prefix on the same method (H_EntityPlayerLocal, which returns true).
		/// </summary>
		private static void ApplyLandingHook(Harmony _harmony)
		{
			MethodInfo target = AccessTools.DeclaredMethod(typeof(EntityAlive), "EndJump");
			if (target == null)
			{
				LandingHookStatus = "NOT APPLIED - EntityAlive.EndJump not found";
				Log.Error(LogPrefix + "Landing hook NOT applied: EntityAlive.EndJump could not be "
					+ "found, so no landing is ever recorded and the perch trip window never opens.");
				return;
			}

			_harmony.Patch(target, postfix: new HarmonyMethod(
				AccessTools.DeclaredMethod(typeof(ZombieLanding), nameof(ZombieLanding.Postfix))));

			LandingHookStatus = "applied - postfix on EntityAlive.EndJump";
			Log.Out(LogPrefix + "Landing hook applied: zombie landings are now timed.");
		}

		/// <summary>The hit itself: both the perch rule and the arrow rule hang off it.</summary>
		private static void ApplyLegHitHook(Harmony _harmony)
		{
			MethodInfo target = AccessTools.DeclaredMethod(typeof(EntityAlive), "DamageEntity",
				new[] { typeof(DamageSource), typeof(int), typeof(bool), typeof(float) });
			if (target == null)
			{
				LegHitHookStatus = "NOT APPLIED - EntityAlive.DamageEntity not found";
				Log.Error(LogPrefix + "Leg hit hook NOT applied: EntityAlive.DamageEntity(DamageSource, "
					+ "int, bool, float) could not be found, so no leg hit will ever trip.");
				return;
			}

			_harmony.Patch(target, postfix: new HarmonyMethod(
				AccessTools.DeclaredMethod(typeof(LegHitTrigger), nameof(LegHitTrigger.Postfix))));

			LegHitHookStatus = "applied - postfix on EntityAlive.DamageEntity";
			Log.Out(LogPrefix + "Leg hit hook applied: a zombie perched on a fence, or running at "
				+ "you, can now be tripped with a hit to the leg.");
		}

		/// <summary>
		/// Arms a tire when a player places it and disarms it when it goes. Both on the Block base
		/// class; neither vanilla's tire blocks nor UL's ULM_Decor override them.
		/// </summary>
		private static void ApplyTireHooks(Harmony _harmony)
		{
			MethodInfo added = AccessTools.DeclaredMethod(typeof(Block), "OnBlockAdded");
			MethodInfo removed = AccessTools.DeclaredMethod(typeof(Block), "OnBlockRemoved");
			if (added == null || removed == null)
			{
				TireHookStatus = "NOT APPLIED - Block.OnBlockAdded/OnBlockRemoved not found";
				Log.Error(LogPrefix + "Tire hooks NOT applied: Block.OnBlockAdded or OnBlockRemoved "
					+ "could not be found, so a placed tire is never armed.");
				return;
			}

			_harmony.Patch(added, postfix: new HarmonyMethod(
				AccessTools.DeclaredMethod(typeof(TripHazards), nameof(TripHazards.OnBlockAddedPostfix))));
			_harmony.Patch(removed, postfix: new HarmonyMethod(
				AccessTools.DeclaredMethod(typeof(TripHazards), nameof(TripHazards.OnBlockRemovedPostfix))));

			TireHookStatus = "applied - postfixes on Block.OnBlockAdded and OnBlockRemoved";
			Log.Out(LogPrefix + "Tire hooks applied: a tire a player puts down is a trip hazard for "
				+ Format.Seconds(Settings.TireSeconds) + ".");
		}

		/// <summary>
		/// The per-zombie tick that looks for a tire underfoot. Returns at once whenever no tire is
		/// armed, so the cost in ordinary play is one integer compare per entity per tick.
		/// </summary>
		private static void ApplyTickHook(Harmony _harmony)
		{
			MethodInfo target = AccessTools.DeclaredMethod(typeof(EntityAlive), "OnUpdateLive");
			if (target == null)
			{
				TickHookStatus = "NOT APPLIED - EntityAlive.OnUpdateLive not found";
				Log.Error(LogPrefix + "Tick hook NOT applied: EntityAlive.OnUpdateLive could not be "
					+ "found, so no zombie ever notices a tire.");
				return;
			}

			_harmony.Patch(target, postfix: new HarmonyMethod(
				AccessTools.DeclaredMethod(typeof(TripHazards), nameof(TripHazards.OnUpdateLivePostfix))));

			TickHookStatus = "applied - postfix on EntityAlive.OnUpdateLive";
			Log.Out(LogPrefix + "Tick hook applied: zombies now notice an armed tire underfoot.");
		}
	}
}
