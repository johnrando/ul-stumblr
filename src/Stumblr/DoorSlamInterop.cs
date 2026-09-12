using System;
using System.Reflection;
using HarmonyLib;

namespace Stumblr
{
	/// <summary>
	/// The one thing this mod exposes to another: DoorSlammer has just slammed a door on a zombie,
	/// so give it a chance to go down.
	///
	/// PUBLISHED CONTRACT. DoorSlammer binds <see cref="TryProc"/> and <see cref="SetFlavor"/> by
	/// reflection - it cannot reference this assembly, because either mod has to work with the other
	/// absent - so those two signatures are the whole interface. Changing one silently switches that
	/// half of the interaction off. Both are expressed in game types only. The shape is identical
	/// to FletchWounds' <c>DoorSlamInterop</c>, which is how DoorSlammer finds both with one bridge.
	/// </summary>
	public static class DoorSlamInterop
	{
		private const string AssemblyName = "DoorSlammer";

		private const string TypeName = "DoorSlammer.FlavorInterop";

		private const string Label = "DoorSlammer";

		/// <summary>One-line presence report for <c>sb info</c>.</summary>
		internal static string Status = "not checked";

		/// <summary>The tail of the <c>sb flavor</c> menu line, ready to print.</summary>
		internal static string FlavorSummary = "no supported mods installed";

		private static bool present;

		/// <summary>DoorSlammer's own flavor setter, bound the same way it binds ours.</summary>
		private static Action<bool> setTheirs;

		/// <summary>
		/// A slam has caught a zombie. Roll the door chance and play the trip. The slam's own
		/// damage has already landed, and FletchWounds' arrow proc, if any, ran first; a zombie
		/// either of those killed fails <see cref="ZombieTrip.CanTrip"/>.
		/// </summary>
		/// <param name="_zombie">The zombie the slam caught. Alive, and known to be a zombie.</param>
		/// <param name="_slammer">The player who closed the door. Unused: a trip is credited to nobody.</param>
		/// <returns>Whether a trip played.</returns>
		public static bool TryProc(EntityAlive _zombie, EntityAlive _slammer)
		{
			if (!Settings.Enabled || !Settings.Flavor || Settings.DoorChance <= 0f)
			{
				return false;
			}

			if (!ZombieTrip.CanTrip(_zombie))
			{
				return false;
			}

			Counters.DoorProcs++;

			if (Settings.ZombieMode == ZombieReaction.Off || _zombie.rand.RandomFloat >= Settings.DoorChance)
			{
				return false;
			}

			Counters.DoorTrips++;
			ZombieTrip.Apply(_zombie);
			return true;
		}

		/// <summary>
		/// PUBLISHED CONTRACT, like <see cref="TryProc"/>. DoorSlammer calls this when flavor is
		/// toggled there, or in any other mod it bridges. Deliberately does not push back: DoorSlammer
		/// is the hub, and a receiver that pushed would loop.
		/// </summary>
		public static void SetFlavor(bool _on)
		{
			Settings.Flavor = _on;
			Config.Save();
		}

		/// <summary>Mirror this mod's flavor setting onto DoorSlammer, which passes it on to every
		/// other mod it bridges. Called only from the console command.</summary>
		internal static void PushFlavor(bool _on)
		{
			if (setTheirs == null)
			{
				return;
			}

			try
			{
				setTheirs(_on);
			}
			catch (Exception e)
			{
				setTheirs = null;
				Log.Warning(Patches.LogPrefix + "Could not mirror the flavor switch to " + Label
					+ "; set it there by hand. " + e.Message);
			}
		}

		/// <summary>The line <c>sb flavor</c> prints after toggling.</summary>
		internal static string Describe()
		{
			bool linked = setTheirs != null;

			if (!Settings.Flavor)
			{
				return linked
					? "Flavor OFF here and in " + Label + " - a slammed door only does what DoorSlammer says."
					: "Flavor OFF - a slammed door only does what DoorSlammer says.";
			}

			if (!present)
			{
				return "Flavor ON - but no mod that hooks into it is installed, so nothing changes.";
			}

			return linked
				? "Flavor ON here and in " + Label + " - a door slammed on a zombie now trips it "
					+ Format.Percent(Settings.DoorChance * 100f) + " of the time."
				: "Flavor ON - a door slammed on a zombie trips it "
					+ Format.Percent(Settings.DoorChance * 100f) + " of the time. " + Label
					+ " needs 'ds flavor' on too.";
		}

		/// <summary>The <c>sb door</c> menu line.</summary>
		internal static string DoorStatus()
		{
			if (Settings.DoorChance <= 0f)
			{
				return "off - a slammed door does not trip";
			}

			return "a door slammed on a zombie trips it " + Format.Percent(Settings.DoorChance * 100f)
				+ " of the time" + (present ? string.Empty : " (needs DoorSlammer)");
		}

		/// <summary>
		/// Notes whether DoorSlammer is installed and links the two flavor switches if it is.
		/// Purely advisory: this side of the interaction is passive, so nothing here gates anything.
		/// </summary>
		internal static void Report()
		{
			Assembly assembly = UndeadLegacyInfo.FindAssembly(AssemblyName);
			present = assembly != null;
			Status = present ? "installed" : "not installed";
			FlavorSummary = present
				? "enhanced mod interaction with " + Label
				: "no supported mods installed";

			if (present)
			{
				BindTheirFlavorSetter(assembly);
				Log.Out(Patches.LogPrefix + Label + " detected: a door slammed on a zombie can now trip "
					+ "it. Toggle with 'sb flavor', tune with 'sb door'.");
			}
		}

		/// <summary>
		/// Optional, and separate from <see cref="TryProc"/>: a DoorSlammer build too old to carry
		/// the setter still gets the interaction, the player just has to set both switches.
		///
		/// Resolved through the loaded Assembly rather than Type.GetType with an assembly-qualified
		/// name: on a Windows dedicated server Mod.loadAssembly loads mods from bytes, and byte-loaded
		/// assemblies are not found by name probing.
		/// </summary>
		private static void BindTheirFlavorSetter(Assembly _assembly)
		{
			try
			{
				Type type = _assembly.GetType(TypeName, false);
				MethodInfo method = type == null
					? null
					: AccessTools.DeclaredMethod(type, "SetFlavor", new[] { typeof(bool) });
				if (method == null)
				{
					Log.Warning(Patches.LogPrefix + Label + " has no flavor switch to link, so "
						+ "'sb flavor' only sets this side. Set 'ds flavor' too.");
					return;
				}
				setTheirs = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), method);
			}
			catch (Exception e)
			{
				Log.Warning(Patches.LogPrefix + "Could not bind the flavor switch in " + Label + ": "
					+ e.Message);
			}
		}
	}
}
