using System.Collections.Generic;

namespace Stumblr
{
	/// <summary>Which reaction a tripped zombie plays, if any.</summary>
	internal enum ZombieReaction
	{
		/// <summary>Nothing. A leg hit is just a leg hit.</summary>
		Off,

		/// <summary>The game's own StumbleBreakThrough - it staggers and recovers on its feet.</summary>
		Stumble,

		/// <summary>The game's own StumbleBreakThroughRagdoll - it goes down properly.</summary>
		Ragdoll
	}

	/// <summary>
	/// Runtime knobs, all switchable from the <c>sb</c> console command. The values here are the
	/// built-in defaults; <see cref="Config"/> reads the player's file over them at startup and
	/// writes it back whenever a command changes one.
	/// </summary>
	internal static class Settings
	{
		/// <summary>Master switch. When off, every hook returns immediately.</summary>
		internal static bool Enabled = true;

		/// <summary>
		/// The perch trip chance is the attacking swing's dismember chance times this. The dismember
		/// chance is the game's own number for the hit - Undead Legacy raises it from 0.25% to 25%
		/// as the weapon's action skill levels 1 to 100, and its skill books add more - so a
		/// multiplier of 2 means a fresh character trips a perched zombie about one swing in two
		/// hundred and a master about one in two. See <see cref="LegHitTrigger"/>.
		/// </summary>
		internal static float ChanceMultiplier = 2f;

		/// <summary>
		/// Seconds either side of a zombie's landing during which a leg hit can trip it. The idea
		/// is a zombie that has just scrambled onto a fence and has not found its balance; one that
		/// has been standing up there a while has. A hit before the landing is parked and judged
		/// when it comes down; see <see cref="ZombieLanding"/>. 0 switches the window check off,
		/// so any zombie standing on a narrow block counts.
		/// </summary>
		internal static float WindowSeconds = 0.5f;

		/// <summary>
		/// A block is narrow when the thinner of its two horizontal extents is at most this many
		/// blocks wide. The game clamps a shape's bounds to no less than 0.2 wide, so a pole reads
		/// as 0.2; a fence or railing as 0.1 to 0.3; a full cube as 1.
		/// </summary>
		internal static float NarrowWidth = 0.4f;

		/// <summary>
		/// A narrow block must also be at least this tall, so a thin floor plate does not count as
		/// something to lose your footing on.
		/// </summary>
		internal static float NarrowMinHeight = 0.5f;

		/// <summary>
		/// How far from ground level, in blocks, a perch trip can still happen. Measured against
		/// <c>World.GetTerrainHeight</c>, which ignores built structures, so this keeps trips to
		/// yard fences and guardrails and away from rooftop catwalks - where a stumble is both more
		/// punishing and less plausible. See <see cref="GroundLevel"/>.
		/// </summary>
		internal static int GroundBand = 3;

		/// <summary>What a tripping zombie does, whatever tripped it. Cycled with <c>sb zombie</c>.</summary>
		internal static ZombieReaction ZombieMode = ZombieReaction.Stumble;

		/// <summary>
		/// Seconds a stumbling zombie stays stunned. 1 second is vanilla's own value for this
		/// reaction, copied from <c>EntityHuman.ExecuteDestroyBlockBehavior</c>.
		/// </summary>
		internal static float ZombieStunSeconds = 1f;

		/// <summary>
		/// An arrow or bolt to the leg of a running zombie trips it with the shot's dismember chance
		/// times this. Separate from <see cref="ChanceMultiplier"/> because the two rules are
		/// balanced apart: this one needs no fence. 0 switches the arrow rule off. See
		/// <see cref="LegHitTrigger"/>.
		/// </summary>
		internal static float ArrowMultiplier = 2f;

		/// <summary>
		/// How long after a player puts a tire down it stays a trip hazard. 0 switches tires off.
		/// See <see cref="TripHazards"/>.
		/// </summary>
		internal static float TireSeconds = 5f;

		/// <summary>Chance, 0 to 1, that a zombie stepping into a live tire trips. Rolled once per
		/// zombie per tire.</summary>
		internal static float TireChance = 0.5f;

		/// <summary>
		/// Chance, 0 to 1, that a zombie caught in a slammed door trips. Only reachable through
		/// DoorSlammer with flavor on in both mods. 0 switches it off. See <see cref="DoorSlamInterop"/>.
		/// </summary>
		internal static float DoorChance = 0.5f;

		/// <summary>Take part in the extra behaviour supported mods offer. Does nothing unless one
		/// is installed. Currently DoorSlammer; both mods carry this switch and both must be on.</summary>
		internal static bool Flavor = true;

		/// <summary>
		/// Case-insensitive substrings of a block's name that make it a trip hazard when a player
		/// places it. Every tire block in vanilla and Undead Legacy has "tire" in its name.
		/// </summary>
		internal static readonly List<string> TirePatterns = new List<string> { "tire" };

		/// <summary>
		/// Case-insensitive substrings that count as narrow regardless of their measured bounds,
		/// matched against both the block's name and its shape's - see <see cref="NarrowBlocks"/>
		/// for why both. The bounds test does most of the work; this is for things whose bounds
		/// are wide but whose footing is not, like a hedge or a coil of barbed wire. Editable
		/// in-game with <c>sb add</c> / <c>sb drop</c>.
		/// </summary>
		internal static readonly List<string> Include = new List<string>
		{
			"fence", "railing", "guardrail", "handrail", "baluster",
			"barbedwire", "barbwire", "barrier", "pole",
			"planthedge", "plantshrub"
		};

		/// <summary>
		/// Substrings that veto a match, checked before either the include list or the bounds. Doors
		/// and gates are opened rather than perched on, and the helper blocks are never placed.
		/// </summary>
		internal static readonly List<string> Exclude = new List<string>
		{
			"fencedoor", "gate", "randomhelper", "varianthelper", "poivariant",
			"pendantpole", "polelight"
		};
	}
}
