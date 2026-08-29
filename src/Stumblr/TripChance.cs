using System.Globalization;
using UnityEngine;

namespace Stumblr
{
	/// <summary>
	/// How likely the player is to trip, given their Athletics level.
	///
	/// Athletics is an Undead Legacy perk, not a vanilla one. Vanilla's skillAgilityAthletics is
	/// only a category header - an empty effect_group whose children are Parkour, Cardio and so on -
	/// so there is no such thing as an Athletics *level* without UL. UL adds actionPerkAthletics,
	/// a use-based perk capped at level 100 that levels from walking and running, which is exactly
	/// the long-run curve wanted here. Without UL the closest vanilla perk in that tree, perkParkour,
	/// stands in; with neither, the chance is simply not scaled.
	///
	/// The curve is linear rather than geometric, so it fits in one line of the README and a player
	/// can predict it: full chance at level 1, the floor at the cap, straight line between.
	/// </summary>
	internal static class TripChance
	{
		/// <summary>UL's perk, preferred. Capped at 100 and levelled by moving.</summary>
		private const string UlPerk = "actionPerkAthletics";

		/// <summary>Vanilla's stand-in, under the same Athletics skill heading.</summary>
		private const string VanillaPerk = "perkParkour";

		/// <summary>Which perk was found, for <c>sb info</c>.</summary>
		internal static string PerkName = "not looked up yet";

		/// <summary>The chance for this player right now, as a percentage.</summary>
		internal static float ForPlayer(EntityPlayer _player)
		{
			float span = Settings.PlayerChance - Settings.PlayerChanceFloor;
			if (span <= 0f)
			{
				// Floor at or above the base: the scaling is switched off rather than inverted.
				return Settings.PlayerChance;
			}

			ProgressionValue perk = FindPerk(_player);
			if (perk == null)
			{
				return Settings.PlayerChance;
			}

			int maxLevel = perk.ProgressionClass != null ? perk.ProgressionClass.MaxLevel : 0;
			if (maxLevel <= 1)
			{
				return Settings.PlayerChance;
			}

			float t = Mathf.Clamp01((perk.Level - 1f) / (maxLevel - 1f));
			return Settings.PlayerChance - span * t;
		}

		/// <summary>
		/// The perk backing the scaling, or null. Not cached: a player can respec, and UL's perk
		/// levels while you walk, so this has to be read fresh. It is a dictionary lookup on a jump,
		/// which is not a hot path.
		/// </summary>
		private static ProgressionValue FindPerk(EntityPlayer _player)
		{
			Progression progression = _player.Progression;
			if (progression == null)
			{
				PerkName = "none - player has no progression yet";
				return null;
			}

			// GetProgressionValue returns null for a name this install has never heard of, which is
			// exactly what happens to the UL perk on a vanilla install - so this is a null check
			// rather than a try/catch.
			ProgressionValue perk = progression.GetProgressionValue(UlPerk);
			if (perk != null)
			{
				PerkName = UlPerk;
				return perk;
			}

			perk = progression.GetProgressionValue(VanillaPerk);
			if (perk != null)
			{
				PerkName = VanillaPerk;
				return perk;
			}

			PerkName = "none found - chance is not scaled";
			return null;
		}

		/// <summary>
		/// The <c>sb info</c> line. This is the only place the scaling is visible in-game, and the
		/// only way to tell "my Athletics is high" from "the perk name is wrong".
		/// </summary>
		internal static string Status(EntityPlayer _player)
		{
			if (_player == null)
			{
				return "no local player";
			}

			float chance = ForPlayer(_player);
			ProgressionValue perk = FindPerk(_player);

			if (perk == null)
			{
				return PerkName + ", trips at " + Percent(chance);
			}

			int maxLevel = perk.ProgressionClass != null ? perk.ProgressionClass.MaxLevel : 0;
			return PerkName + " " + perk.Level + "/" + maxLevel + ", trips at " + Percent(chance);
		}

		/// <summary>Printed the same way it is parsed, so a reported value can be typed back in.</summary>
		internal static string Percent(float _value)
		{
			return _value.ToString("0.###", CultureInfo.InvariantCulture) + "%";
		}
	}
}
