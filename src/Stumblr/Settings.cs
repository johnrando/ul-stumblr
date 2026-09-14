using System.Collections.Generic;

namespace Stumblr
{
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

		/// <summary>
		/// What a tripping zombie does, whatever tripped it: one reaction drawn from this weighted
		/// table, set with <c>sb zombie</c>. A weight of 0 takes a reaction out; all five at 0 is
		/// off. The break-through pair - stumble and ragdoll - always lurch forward, which is
		/// toward the player; the other three go down sideways or backward, so they carry the
		/// weight by default. See <see cref="ZombieTrip"/>.
		/// </summary>
		internal static float WeightStumble = 1f;

		internal static float WeightKneel = 1f;

		internal static float WeightProne = 2f;

		internal static float WeightRagdoll = 1f;

		internal static float WeightShove = 2f;

		/// <summary>
		/// The impulse behind a shove, in the units <c>Rigidbody.AddForce</c> takes for an impulse.
		/// Vanilla gives a knockdown hit 20 plus half its damage, and clamps at eight times the
		/// zombie's mass; 60 is a solid push without looking like a sledgehammer. 0 turns a shove
		/// into a prone fall.
		/// </summary>
		internal static float ShoveForce = 60f;

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
		/// zombie per tire, times the kind's multiplier below.</summary>
		internal static float TireChance = 0.5f;

		/// <summary>
		/// Per kind of tire: a multiplier on <see cref="TireChance"/> and how many zombies one tire
		/// can trip before it is spent. A donut spare is a smaller target and a lighter fall; a
		/// heap of three is a bigger one and can take more than one zombie down. A vertical stack
		/// is a wall, not a hazard, so it is off. Set with <c>sb tires</c>; see <see cref="TripHazards"/>.
		/// </summary>
		internal static TireRule TireSmall = new TireRule(0.75f, 1);

		internal static TireRule TireSingle = new TireRule(1f, 2);

		internal static TireRule TirePile = new TireRule(1.5f, 3);

		internal static TireRule TireStack = new TireRule(0f, 0);

		internal static TireRule TireRule(TireKind _kind)
		{
			switch (_kind)
			{
			case TireKind.Small:
				return TireSmall;
			case TireKind.Pile:
				return TirePile;
			case TireKind.Stack:
				return TireStack;
			default:
				return TireSingle;
			}
		}

		internal static void SetTireRule(TireKind _kind, TireRule _rule)
		{
			switch (_kind)
			{
			case TireKind.Small:
				TireSmall = _rule;
				break;
			case TireKind.Pile:
				TirePile = _rule;
				break;
			case TireKind.Stack:
				TireStack = _rule;
				break;
			default:
				TireSingle = _rule;
				break;
			}
		}

		/// <summary>
		/// Chance, 0 to 1, that a zombie caught in a slammed door trips. Only reachable through
		/// DoorSlammer with flavor on in both mods. 0 switches it off. See <see cref="FlavorInterop"/>.
		/// </summary>
		internal static float DoorChance = 0.5f;

		// The per-partner flavor switches live in FlavorSwitches: one per mod this one links up
		// with, all on by default, and mirrored pairwise rather than as one shared value.

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
