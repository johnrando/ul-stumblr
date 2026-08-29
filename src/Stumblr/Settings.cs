using System.Collections.Generic;

namespace Stumblr
{
	/// <summary>Which reaction a caught zombie plays, if any.</summary>
	internal enum ZombieReaction
	{
		/// <summary>Nothing. Zombies jump fences exactly as vanilla lets them.</summary>
		Off,

		/// <summary>The game's own StumbleBreakThrough - it staggers and recovers on its feet.</summary>
		Stumble,

		/// <summary>The game's own StumbleBreakThroughRagdoll - it goes down properly.</summary>
		Ragdoll
	}

	/// <summary>
	/// Runtime knobs, all switchable from the <c>sb</c> console command. Deliberately plain statics
	/// rather than a config file: the chances are what you want to wind up to 100 for one test and
	/// back down again, with a fence in front of you.
	/// </summary>
	internal static class Settings
	{
		/// <summary>Master switch. When off, both jump hooks return immediately.</summary>
		internal static bool Enabled = true;

		/// <summary>
		/// Percent chance the player trips, at Athletics level 1. Scaled down toward
		/// <see cref="PlayerChanceFloor"/> as the perk levels; see <see cref="TripChance"/>.
		/// </summary>
		internal static float PlayerChance = 1f;

		/// <summary>
		/// Percent chance the player trips at the Athletics cap. Never zero by default: a tenfold
		/// reduction is meant to be worth earning without ever making a fence free again.
		/// </summary>
		internal static float PlayerChanceFloor = 0.1f;

		/// <summary>
		/// Percent chance a zombie is caught. Not scaled by anything - zombies have no skill to
		/// read, and a horde spilling over a fence is where this is worth seeing.
		/// </summary>
		internal static float ZombieChance = 2f;

		/// <summary>
		/// How far from ground level, in blocks, a trip can still happen. Measured against
		/// <c>World.GetTerrainHeight</c>, which ignores built structures, so this keeps trips to
		/// yard fences and guardrails and away from rooftop catwalks - where a stumble is both more
		/// punishing and less plausible. See <see cref="GroundLevel"/>.
		/// </summary>
		internal static int GroundBand = 3;

		/// <summary>Whether a trip plays the player's own small-pain grunt.</summary>
		internal static bool PlaySound = true;

		/// <summary>Whether a trip jolts the camera.</summary>
		internal static bool ShakeCamera = true;

		/// <summary>
		/// Of the zombies that trip, the percentage caught on the near side - the jump is cancelled
		/// and they never leave the ground. The rest clear the obstacle and go down on the far side
		/// instead. 50 means an even mix, which reads better than either extreme: all-near looks
		/// like an invisible wall, all-far like they were never troubled by the fence.
		/// </summary>
		internal static float ZombieCatchPercent = 50f;

		/// <summary>What a tripping zombie does. Cycled with <c>sb zombie</c>.</summary>
		internal static ZombieReaction ZombieMode = ZombieReaction.Stumble;

		/// <summary>
		/// Seconds a stumbling zombie stays stunned. 1 second is vanilla's own value for this
		/// reaction, copied from <c>EntityHuman.ExecuteDestroyBlockBehavior</c>.
		/// </summary>
		internal static float ZombieStunSeconds = 1f;

		/// <summary>
		/// Camera shake strength and duration. Vanilla's own EnumCameraShake sizes are 5 for Tiny,
		/// 10 for Small and 20 for Big; a trip is a Tiny.
		/// </summary>
		internal static float ShakeStrength = 5f;

		internal static float ShakeSeconds = 0.3f;

		/// <summary>
		/// Minimum seconds between two player trips. Undead Legacy adds a double jump, so one fence
		/// can raise the landing hook more than once; this keeps that to a single grunt.
		/// </summary>
		internal static float PlayerCooldownSeconds = 0.5f;

		/// <summary>
		/// Case-insensitive substrings that count as trippable, matched against both the block's
		/// name and its shape's - see <see cref="TripBlocks"/> for why both. Vanilla ships no fence
		/// or railing tag, so names are all there is to go on. Editable in-game with <c>sb add</c>
		/// / <c>sb drop</c>.
		/// </summary>
		internal static readonly List<string> Include = new List<string>
		{
			"fence", "railing", "guardrail", "handrail", "baluster",
			"barbedwire", "barbwire", "barrier", "pole",
			"planthedge", "plantshrub"
		};

		/// <summary>
		/// Substrings that veto a match. These are all caught by <see cref="Include"/> but are not
		/// things you hop: doors and gates are opened, and the helper blocks are never placed.
		/// </summary>
		internal static readonly List<string> Exclude = new List<string>
		{
			"fencedoor", "gate", "randomhelper", "varianthelper", "poivariant",
			"pendantpole", "polelight"
		};
	}
}
