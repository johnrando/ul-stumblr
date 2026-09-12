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
	/// Runtime knobs, all switchable from the <c>sb</c> console command. Deliberately plain statics
	/// rather than a config file: the multiplier is what you want to wind up to 100 for one test and
	/// back down again, with a zombie on a fence in front of you.
	/// </summary>
	internal static class Settings
	{
		/// <summary>Master switch. When off, both hooks return immediately.</summary>
		internal static bool Enabled = true;

		/// <summary>
		/// The trip chance is the attacking swing's dismember chance times this. The dismember
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
		/// How far from ground level, in blocks, a trip can still happen. Measured against
		/// <c>World.GetTerrainHeight</c>, which ignores built structures, so this keeps trips to
		/// yard fences and guardrails and away from rooftop catwalks - where a stumble is both more
		/// punishing and less plausible. See <see cref="GroundLevel"/>.
		/// </summary>
		internal static int GroundBand = 3;

		/// <summary>What a tripping zombie does. Cycled with <c>sb zombie</c>.</summary>
		internal static ZombieReaction ZombieMode = ZombieReaction.Stumble;

		/// <summary>
		/// Seconds a stumbling zombie stays stunned. 1 second is vanilla's own value for this
		/// reaction, copied from <c>EntityHuman.ExecuteDestroyBlockBehavior</c>.
		/// </summary>
		internal static float ZombieStunSeconds = 1f;

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
