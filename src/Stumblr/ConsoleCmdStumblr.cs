using System.Collections.Generic;
using System.Globalization;

namespace Stumblr
{
	/// <summary>
	/// <c>sb</c> (or <c>stumblr</c>) - toggles the mod and prints the settings block.
	///
	/// The settings block doubles as the menu: every line names the command that changes it, and
	/// shows what that command left behind. The toggles list their choices with the live one marked,
	/// so the block is also the answer to "what can I set this to".
	///
	/// It is kept short on purpose. Everything that answers "is this thing working" lives in
	/// <c>sb info</c> instead, and matters more here than in either sibling: a trip is rare by
	/// design, so not seeing one proves nothing. The per-gate counters are the only way to tell a
	/// 1% roll that has not come up from a block list that never matches.
	/// </summary>
	public class ConsoleCmdStumblr : ConsoleCmdAbstract
	{
		public override bool IsExecuteOnClient => false;

		public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
		{
			string command = _params.Count > 0 ? _params[0].ToLower() : string.Empty;

			switch (command)
			{
			case "":
				Settings.Enabled = !Settings.Enabled;
				OutputStatus();
				return;

			case "chance":
				SetChance(_params);
				return;

			case "floor":
				SetFloor(_params);
				return;

			case "ground":
				SetGround(_params);
				return;

			case "hurt":
				Settings.PlaySound = !Settings.PlaySound;
				Output(TripSound.Describe());
				return;

			case "shake":
				Settings.ShakeCamera = !Settings.ShakeCamera;
				Output(PlayerTrip.DescribeShake());
				return;

			case "catch":
				SetCatch(_params);
				return;

			case "zombie":
				ZombieTrip.Cycle();
				Output(ZombieTrip.Describe());
				return;

			case "blocks":
				Output(TripBlocks.Describe());
				return;

			case "add":
				AddPattern(_params);
				return;

			case "drop":
				DropPattern(_params);
				return;

			case "info":
				OutputInfo();
				return;

			case "reset":
				Counters.Reset();
				Output("Counters reset.");
				return;

			default:
				Output("Unknown option '" + _params[0]
					+ "'. Try: sb [chance|floor|ground|hurt|shake|catch|zombie|blocks|add|drop|info|reset]");
				return;
			}
		}

		/// <summary>
		/// The menu. <paramref name="_header"/> differs because <c>sb</c> has just changed something
		/// and <c>sb info</c> has not.
		/// </summary>
		private static void OutputMenu(string _header)
		{
			Output(_header);
			Line("sb chance {p} {z}", ChanceLine());
			Line("sb floor {p}", FloorLine());
			Line("sb ground {n}", GroundLevel.Status());
			Line("sb hurt", Choices(Mark("on", Settings.PlaySound), Mark("off", !Settings.PlaySound)));
			Line("sb shake", Choices(Mark("on", Settings.ShakeCamera),
				Mark("off", !Settings.ShakeCamera)));
			Line("sb catch {pct}", CatchLine());
			Line("sb zombie", ZombieChoices());
			Line("sb blocks", TripBlocks.Status());
		}

		private static void OutputStatus()
		{
			OutputMenu("Stumblr is now " + (Settings.Enabled ? "ON" : "OFF"));
		}

		/// <summary>The menu, with the read-only lines appended in the same column.</summary>
		private static void OutputInfo()
		{
			OutputMenu("Stumblr is " + (Settings.Enabled ? "ON" : "OFF"));
			Line("Undead Legacy", UndeadLegacyInfo.Status);
			Line("zombie jump hook", Patches.ZombieJumpHookStatus);
			Line("player jump hook", Patches.PlayerJumpHookStatus);
			Line("player land hook", Patches.PlayerLandHookStatus);
			Line("Athletics", TripChance.Status(GetLocalPlayer()));
			Line("last trip sound", TripSound.LastPlayed);
			Line("jumps seen", Counters.JumpsSeen.ToString());
			Line("player", Counters.PlayerJumps + " jumps, " + Counters.PlayerNearGround
				+ " near ground, " + Counters.PlayerOverTrippable + " over a fence, "
				+ Counters.PlayerTrips + " tripped");
			Line("zombies", Counters.ZombieJumps + " jumps, " + Counters.ZombieNearGround
				+ " near ground, " + Counters.ZombieOverTrippable + " over a fence, "
				+ Counters.ZombieTrips + " tripped (" + Counters.ZombieNearSide + " caught, "
				+ Counters.ZombieFarSide + " on landing, " + Counters.ZombieRagdolls
				+ " ragdolled)");

			if (Counters.JumpsSeen == 0)
			{
				Output("Note: no jump has reached the hooks yet. Jumping anywhere should move that");
				Output("number - if it stays at zero, the hooks are not live.");
			}
		}

		/// <summary>
		/// One line of the block. Every label is padded to the width of the longest one -
		/// "sb chance {p} {z}" - so the settings and the read-only lines share a column and
		/// <c>sb info</c> reads as one block rather than two.
		/// </summary>
		private static void Line(string _label, string _value)
		{
			Output("  " + _label.PadRight(18) + ": " + _value);
		}

		private static EntityPlayerLocal GetLocalPlayer()
		{
			GameManager gameManager = GameManager.Instance;
			if (!gameManager || gameManager.World == null)
			{
				return null;
			}
			return gameManager.World.GetPrimaryPlayer();
		}

		private static void SetChance(List<string> _params)
		{
			if (_params.Count != 3)
			{
				Output("Usage: sb chance {p} {z} - currently: " + ChanceLine());
				return;
			}

			if (!TryPercent(_params[1], "player chance", out float player)
				|| !TryPercent(_params[2], "zombie chance", out float zombie))
			{
				return;
			}

			if (player < Settings.PlayerChanceFloor)
			{
				Output("Player chance " + TripChance.Percent(player) + " is below the Athletics floor "
					+ TripChance.Percent(Settings.PlayerChanceFloor)
					+ " - lower the floor first with 'sb floor'.");
				return;
			}

			Settings.PlayerChance = player;
			Settings.ZombieChance = zombie;
			Output("Trip chance: " + ChanceLine());
		}

		private static void SetFloor(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: sb floor {p} - currently: " + FloorLine());
				return;
			}

			if (!TryPercent(_params[1], "floor", out float floor))
			{
				return;
			}

			// Rejected rather than clamped: a floor above the base would run the curve backwards, so
			// say so instead of quietly doing something else.
			if (floor > Settings.PlayerChance)
			{
				Output("A floor of " + TripChance.Percent(floor) + " is above the base chance "
					+ TripChance.Percent(Settings.PlayerChance)
					+ " - raise the base first with 'sb chance'.");
				return;
			}

			Settings.PlayerChanceFloor = floor;
			Output("Athletics floor: " + FloorLine());
		}

		private static void SetGround(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: sb ground {n} - currently: " + GroundLevel.Status());
				return;
			}

			if (!TryCount(_params[1], "block count", out int band))
			{
				return;
			}

			Settings.GroundBand = band;
			Output("Ground band: " + GroundLevel.Status());
		}

		private static void SetCatch(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: sb catch {pct} - currently: " + CatchLine());
				return;
			}

			if (!TryPercent(_params[1], "catch share", out float share))
			{
				return;
			}

			Settings.ZombieCatchPercent = share;
			Output("Catch share: " + CatchLine());
		}

		private static void AddPattern(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: sb add {pattern} - a block-name substring, like 'balustrade'.");
				return;
			}

			string pattern = _params[1].ToLowerInvariant();
			if (Settings.Include.Contains(pattern))
			{
				Output("'" + pattern + "' is already trippable.");
				return;
			}

			Settings.Include.Add(pattern);
			TripBlocks.PatternsChanged();
			Output("Added '" + pattern + "'. " + TripBlocks.Status());
		}

		private static void DropPattern(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: sb drop {pattern} - currently: "
					+ string.Join(", ", Settings.Include.ToArray()));
				return;
			}

			string pattern = _params[1].ToLowerInvariant();
			if (!Settings.Include.Remove(pattern))
			{
				Output("'" + pattern + "' is not in the list. Currently: "
					+ string.Join(", ", Settings.Include.ToArray()));
				return;
			}

			TripBlocks.PatternsChanged();
			Output("Dropped '" + pattern + "'. " + TripBlocks.Status());
		}

		/// <summary>
		/// A percentage from 0 to 100. Parsed against the invariant culture rather than the player's,
		/// so "0.1" means the same thing on a machine whose decimal separator is a comma.
		/// </summary>
		private static bool TryPercent(string _value, string _what, out float _parsed)
		{
			// !(x >= 0f) rather than x < 0f, because NaN parses successfully and then fails every
			// comparison - a plain "less than zero" test would wave it through.
			if (!float.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out _parsed)
				|| !(_parsed >= 0f) || _parsed > 100f)
			{
				Output("'" + _value + "' is not a valid " + _what + " - a percentage from 0 to 100, "
					+ "like 1.0.");
				_parsed = 0f;
				return false;
			}
			return true;
		}

		/// <summary>A whole number, zero or more. Zero switches the gate off entirely.</summary>
		private static bool TryCount(string _value, string _what, out int _parsed)
		{
			if (!int.TryParse(_value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _parsed)
				|| _parsed < 0)
			{
				Output("'" + _value + "' is not a valid " + _what + " - whole numbers from 0 up.");
				_parsed = 0;
				return false;
			}
			return true;
		}

		/// <summary>The choice list for a toggle, with the live value marked.</summary>
		private static string Choices(params string[] _options)
		{
			return "[ " + string.Join(" | ", _options) + " ]";
		}

		private static string Mark(string _option, bool _live)
		{
			return _live ? ">" + _option + "<" : _option;
		}

		private static string ZombieChoices()
		{
			return Choices(
				Mark("off", Settings.ZombieMode == ZombieReaction.Off),
				Mark("stumble", Settings.ZombieMode == ZombieReaction.Stumble),
				Mark("ragdoll", Settings.ZombieMode == ZombieReaction.Ragdoll));
		}

		private static string ChanceLine()
		{
			return TripChance.Percent(Settings.PlayerChance) + " player / "
				+ TripChance.Percent(Settings.ZombieChance) + " zombie";
		}

		private static string CatchLine()
		{
			return TripChance.Percent(Settings.ZombieCatchPercent)
				+ " of zombie trips caught before the jump, rest on landing";
		}

		private static string FloorLine()
		{
			return "Athletics cuts the player to " + TripChance.Percent(Settings.PlayerChanceFloor);
		}

		private static void Output(string _line)
		{
			SdtdConsole.Instance.Output(_line);
		}

		public override string[] getCommands()
		{
			return new string[2] { "sb", "stumblr" };
		}

		public override string getDescription()
		{
			return "Toggles the Stumblr mod and reports its status.";
		}

		public override string getHelp()
		{
			return "Usage: sb [chance {p} {z}|floor {p}|ground {n}|hurt|shake|catch {pct}|zombie"
				+ "|blocks|add {pattern}|drop {pattern}|info|reset]"
				+ "\r\n\r\nJumping a fence, a railing or a guardrail carries a small chance of "
				+ "catching a foot on it. A zombie goes down in one of the game's own stumble "
				+ "animations. The player always clears the obstacle and only lands badly - a grunt "
				+ "and a jolt of the view, and nothing else at all: no damage, no stamina, no buff, "
				+ "no loss of speed, control or aim. Anything more than that means death with a "
				+ "horde behind you, which is the point.\r\n\r\n'sb' on its own toggles the mod and "
				+ "prints the settings. Each line names the command that changes it, so the settings "
				+ "block is the menu.\r\n\r\n'sb chance {p} {z}' sets the percentage chance for the "
				+ "player and for zombies, 1.0 and 2.0 by default. The player's is the chance at "
				+ "Athletics level 1.\r\n\r\n'sb floor {p}' sets what Athletics can reduce the "
				+ "player's chance to at the perk's cap, 0.1% by default - a tenfold reduction that "
				+ "never quite reaches zero. The curve between the two is a straight line. Athletics "
				+ "is an Undead Legacy perk; without UL the mod falls back to vanilla's perkParkour, "
				+ "and with neither the chance is not scaled. 'sb info' shows which one was found."
				+ "\r\n\r\n'sb ground {n}' limits trips to within n blocks of ground level, 3 by "
				+ "default, measured against the terrain height rather than whatever is built on it. "
				+ "This keeps trips to yard fences and away from rooftop catwalks, where a stumble is "
				+ "both more punishing and less plausible. 0 leaves only entities standing exactly "
				+ "at terrain height, which is as good as off.\r\n\r\n"
				+ "'sb hurt' toggles the grunt, which is your own character's SoundHurtSmall - so "
				+ "nothing is bundled and the voice always matches the character. Unlike Door Slammer "
				+ "and Fletch Wounds this sound is audible to zombies, exactly as taking a hit is."
				+ "\r\n\r\n'sb shake' toggles the camera jolt. It is vanilla's own Tiny shake and it "
				+ "does not move your aim.\r\n\r\n'sb catch {pct}' splits zombie trips between the "
				+ "two sides of the obstacle: caught before the jump, so they never leave the "
				+ "ground, or allowed over and taken down on the landing. 50 by default, an even "
				+ "mix - all-near looks like an invisible wall, all-far like the fence never "
				+ "troubled them.\r\n\r\n'sb zombie' cycles what a tripping zombie does: off, "
				+ "stumble (it staggers and recovers) or ragdoll (it goes down properly). Both are "
				+ "the game's own reactions, and neither deals damage nor triggers rage.\r\n\r\n"
				+ "'sb blocks' prints the block-name substrings that count as trippable, and 'sb add' "
				+ "and 'sb drop' edit them. Matching is on the name because the game ships no fence "
				+ "or railing tag to match on instead.\r\n\r\nAll of these take effect immediately "
				+ "and last until the game is restarted; the defaults live in Settings.cs.\r\n\r\n"
				+ "'sb info' prints the same block with the patch state and the counters added. There "
				+ "is one counter per gate, in the order they are checked, because a trip is rare by "
				+ "design and not seeing one proves nothing - the stage where the numbers stop moving "
				+ "names the setting to change. Jumps but nothing near ground means the band is too "
				+ "tight; near ground but nothing over a fence means the block list is missing the "
				+ "fence you are standing at.\r\n\r\n'sb reset' zeroes the counters so one scenario "
				+ "can be measured on its own.\r\n\r\n'stumblr' is an alias for 'sb'.";
		}
	}
}
