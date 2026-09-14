namespace Stumblr
{
	/// <summary>
	/// The one thing this mod exposes to another: DoorSlammer has just slammed a door on a zombie,
	/// so give it a chance to go down.
	///
	/// PUBLISHED CONTRACT. DoorSlammer binds <see cref="TryProc"/> and <see cref="SetFlavor"/> by
	/// reflection - it cannot reference this assembly, because either mod has to work with the other
	/// absent - so those two signatures are the whole interface. Changing one silently switches that
	/// half of the interaction off. Game types and strings only. The shape is identical to
	/// FletchWounds' <c>FlavorInterop</c>, which is how DoorSlammer finds both with one binder.
	/// </summary>
	public static class FlavorInterop
	{
		/// <summary>
		/// A slam has caught a zombie. Roll the door chance and play the trip. The slam's own
		/// damage has already landed, and FletchWounds' arrow proc, if any, ran first; a zombie
		/// either of those killed fails <see cref="ZombieTrip.CanTrip"/>.
		/// </summary>
		/// <param name="_zombie">The zombie the slam caught. Alive, and known to be a zombie.</param>
		/// <param name="_attacker">The player who closed the door. Unused: a trip is credited to nobody.</param>
		/// <param name="_caller">The calling mod's label, e.g. "DoorSlammer". Gated on this side's
		/// switch for it; a label this build does not know is let through until switched off.</param>
		/// <returns>Whether a trip played.</returns>
		public static bool TryProc(EntityAlive _zombie, EntityAlive _attacker, string _caller)
		{
			if (!Settings.Enabled || !FlavorSwitches.IsOn(_caller) || Settings.DoorChance <= 0f)
			{
				return false;
			}

			if (!ZombieTrip.CanTrip(_zombie))
			{
				return false;
			}

			Counters.DoorProcs++;

			if (!ZombieTrip.Active || _zombie.rand.RandomFloat >= Settings.DoorChance)
			{
				return false;
			}

			Counters.DoorTrips++;
			ZombieTrip.Apply(_zombie);
			return true;
		}

		/// <summary>
		/// PUBLISHED CONTRACT, like <see cref="TryProc"/>. A partner calls this when the player
		/// toggles their switch for this mod over there. Sets this side's switch for that partner
		/// and saves. Deliberately does not push back: whoever the player typed at owns the mirror,
		/// which is what stops two mods calling each other forever.
		/// </summary>
		/// <param name="_partner">The calling mod's label, e.g. "DoorSlammer".</param>
		public static void SetFlavor(string _partner, bool _on)
		{
			FlavorSwitches.Set(_partner, _on);
			Config.Save();
		}

		/// <summary>The <c>sb door</c> menu line.</summary>
		internal static string DoorStatus()
		{
			if (Settings.DoorChance <= 0f)
			{
				return "off - a slammed door does not trip";
			}

			return "a door slammed on a zombie trips it " + Format.Percent(Settings.DoorChance * 100f)
				+ " of the time" + (FlavorPartners.DoorSlammer.Found ? string.Empty : " (needs DoorSlammer)");
		}
	}
}
