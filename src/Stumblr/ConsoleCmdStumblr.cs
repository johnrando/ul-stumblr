using System.Collections.Generic;

namespace Stumblr
{
	/// <summary>
	/// <c>sb</c> (or <c>stumblr</c>) - toggles the mod and prints the settings block.
	///
	/// The settings block doubles as the menu: every line names the command that changes it, and
	/// shows what that command left behind. The toggles list their choices with the live one marked,
	/// so the block is also the answer to "what can I set this to".
	///
	/// Everything that answers "is this thing working" lives in <c>sb info</c>, and <c>sb probe</c>
	/// answers "why does the mod think this block is, or is not, narrow" for whatever the crosshair
	/// is on. A trip is rare by design, so not seeing one proves nothing; the per-gate counters and
	/// the probe are the only way to tell a roll that has not come up from a block that never
	/// measures narrow.
	///
	/// Every change is written straight to the settings file; see <see cref="Config"/>.
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
				Config.Save();
				OutputStatus();
				return;

			case "chance":
				SetChance(_params);
				return;

			case "window":
				SetWindow(_params);
				return;

			case "narrow":
				SetNarrow(_params);
				return;

			case "ground":
				SetGround(_params);
				return;

			case "zombie":
				ZombieTrip.Cycle();
				Config.Save();
				Output(ZombieTrip.Describe());
				return;

			case "arrow":
				SetArrow(_params);
				return;

			case "tire":
				SetTire(_params);
				return;

			case "door":
				SetDoor(_params);
				return;

			case "flavor":
				Settings.Flavor = !Settings.Flavor;
				Config.Save();
				DoorSlamInterop.PushFlavor(Settings.Flavor);
				Output(DoorSlamInterop.Describe());
				return;

			case "blocks":
				Output(NarrowBlocks.Describe());
				return;

			case "add":
				AddPattern(_params);
				return;

			case "drop":
				DropPattern(_params);
				return;

			case "probe":
				Probe();
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
					+ "'. Try: sb [chance|window|narrow|ground|zombie|arrow|tire|door|flavor|blocks|add|drop|probe|info|reset]");
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
			Line("sb chance {mult}", LegHitTrigger.Status());
			Line("sb window {s}", ZombieLanding.Status());
			Line("sb narrow {w} {h}", NarrowBlocks.Status());
			Line("sb ground {n}", GroundLevel.Status());
			Line("sb zombie", ZombieChoices());
			Line("sb arrow {mult}", LegHitTrigger.ArrowStatus());
			Line("sb tire {s} {pct}", TripHazards.Status());
			Line("sb door {pct}", DoorSlamInterop.DoorStatus());
			Line("sb flavor", FlavorChoices() + " - " + DoorSlamInterop.FlavorSummary);
			Line("sb blocks", NarrowBlocks.BlocksStatus());
		}

		private static void OutputStatus()
		{
			OutputMenu("Stumblr is now " + (Settings.Enabled ? "ON" : "OFF"));
		}

		/// <summary>The menu, with the read-only lines appended in the same column.</summary>
		private static void OutputInfo()
		{
			OutputMenu("Stumblr is " + (Settings.Enabled ? "ON" : "OFF"));
			Line("settings file", Config.Status);
			Line("Undead Legacy", UndeadLegacyInfo.Status);
			Line("DoorSlammer", DoorSlamInterop.Status);
			Line("landing hook", Patches.LandingHookStatus);
			Line("leg hit hook", Patches.LegHitHookStatus);
			Line("tire hooks", Patches.TireHookStatus);
			Line("tick hook", Patches.TickHookStatus);
			Line("landings seen", Counters.Landings.ToString());
			Line("zombie hits", Counters.ZombieHits + " seen, " + Counters.PlayerHits + " by a player, "
				+ Counters.LegHits + " to a leg");
			Line("perch", Counters.LegHitsInAir + " in the air (" + Counters.AirHitsLanded
				+ " landed in time), " + Counters.LegHitsInWindow + " after landing, "
				+ Counters.LegHitsOnNarrow + " on a narrow block, " + Counters.Trips + " tripped");
			Line("arrows", Counters.ArrowLegHits + " to a leg, " + Counters.ArrowRunningHits
				+ " on a runner, " + Counters.ArrowTrips + " tripped");
			Line("tires", Counters.TiresPlaced + " placed, " + TripHazards.LiveCount() + " armed now, "
				+ Counters.TireSteps + " stepped in, " + Counters.TireTrips + " tripped");
			Line("doors", Counters.DoorProcs + " slams handed over, " + Counters.DoorTrips + " tripped");
			Line("ragdolls", Counters.ZombieRagdolls + " of all trips");
			Line("last roll", LegHitTrigger.LastRoll);
			Line("last arrow", LegHitTrigger.LastArrow);
			Line("last tire", TripHazards.LastTire);
			Line("last footing", NarrowBlocks.LastChecked);

			if (Counters.ZombieHits == 0)
			{
				Output("Note: no hit on a zombie has reached the hook yet. Hitting any zombie should");
				Output("move that number - if it stays at zero, the hook is not live, or you are a");
				Output("client of a dedicated server, where zombies are remote and never trip.");
			}
		}

		/// <summary>
		/// One line of the block. Every label is padded to the width of the longest one -
		/// "sb tire {s} {pct}" - so the settings and the read-only lines share a column and
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
			if (_params.Count != 2)
			{
				Output("Usage: sb chance {mult} - currently: " + LegHitTrigger.Status());
				return;
			}

			if (!TryNumber(_params[1], "multiplier", 1000f, out float multiplier))
			{
				return;
			}

			Settings.ChanceMultiplier = multiplier;
			Config.Save();
			Output("Perch trip chance: " + LegHitTrigger.Status());
		}

		private static void SetWindow(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: sb window {seconds} - currently: " + ZombieLanding.Status()
					+ ". Applies before and after the landing; 0 means any time.");
				return;
			}

			if (!TryNumber(_params[1], "window", 60f, out float seconds))
			{
				return;
			}

			Settings.WindowSeconds = seconds;
			Config.Save();
			Output("Window: " + ZombieLanding.Status());
		}

		private static void SetNarrow(List<string> _params)
		{
			if (_params.Count != 3)
			{
				Output("Usage: sb narrow {width} {height} - currently: " + NarrowBlocks.Status()
					+ ". Both in blocks; 'sb probe' shows a block's measured size.");
				return;
			}

			if (!TryNumber(_params[1], "width", 1f, out float width)
				|| !TryNumber(_params[2], "height", 1f, out float height))
			{
				return;
			}

			Settings.NarrowWidth = width;
			Settings.NarrowMinHeight = height;
			NarrowBlocks.RulesChanged();
			Config.Save();
			Output("Narrow: " + NarrowBlocks.Status());
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
			Config.Save();
			Output("Ground band: " + GroundLevel.Status());
		}

		private static void SetArrow(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: sb arrow {mult} - currently: " + LegHitTrigger.ArrowStatus()
					+ ". A multiplier on the shot's dismember chance; 0 switches the arrow rule off.");
				return;
			}

			if (!TryNumber(_params[1], "multiplier", 1000f, out float multiplier))
			{
				return;
			}

			Settings.ArrowMultiplier = multiplier;
			Config.Save();
			Output("Arrow: " + LegHitTrigger.ArrowStatus());
		}

		private static void SetTire(List<string> _params)
		{
			if (_params.Count != 3)
			{
				Output("Usage: sb tire {seconds} {percent} - currently: " + TripHazards.Status()
					+ ". How long a placed tire stays armed, and the chance a zombie stepping in "
					+ "trips; 0 seconds switches tires off.");
				return;
			}

			if (!TryNumber(_params[1], "seconds", 600f, out float seconds)
				|| !TryPercent(_params[2], out float chance))
			{
				return;
			}

			Settings.TireSeconds = seconds;
			Settings.TireChance = chance;
			Config.Save();
			Output("Tire: " + TripHazards.Status());
		}

		private static void SetDoor(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: sb door {percent} - currently: " + DoorSlamInterop.DoorStatus()
					+ ". The chance a zombie caught in a slammed door trips; needs DoorSlammer and "
					+ "'sb flavor' on. 0 switches it off.");
				return;
			}

			if (!TryPercent(_params[1], out float chance))
			{
				return;
			}

			Settings.DoorChance = chance;
			Config.Save();
			Output("Door: " + DoorSlamInterop.DoorStatus());
		}

		private static void AddPattern(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: sb add {pattern} - a block or shape name substring, like 'balustrade'.");
				return;
			}

			string pattern = _params[1].ToLowerInvariant();
			if (Settings.Include.Contains(pattern))
			{
				Output("'" + pattern + "' is already always narrow.");
				return;
			}

			Settings.Include.Add(pattern);
			NarrowBlocks.RulesChanged();
			Config.Save();
			Output("Added '" + pattern + "'. " + NarrowBlocks.BlocksStatus());
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

			NarrowBlocks.RulesChanged();
			Config.Save();
			Output("Dropped '" + pattern + "'. " + NarrowBlocks.BlocksStatus());
		}

		/// <summary>
		/// The block under the crosshair, measured. The player's own HitInfo is the same raycast the
		/// game uses to decide what you are about to hit, so what this prints is what a swing would
		/// land on.
		/// </summary>
		private static void Probe()
		{
			EntityPlayerLocal player = GetLocalPlayer();
			if (player == null)
			{
				Output("No local player - 'sb probe' needs a player looking at a block, so it works in "
					+ "single player or as the host.");
				return;
			}

			WorldRayHitInfo hitInfo = player.HitInfo;
			if (hitInfo == null || !hitInfo.bHitValid)
			{
				Output("Nothing under the crosshair within reach.");
				return;
			}

			Vector3i pos = hitInfo.hit.blockPos;
			BlockValue blockValue = player.world.GetBlock(pos);
			Output(NarrowBlocks.Probe(blockValue, pos));
		}

		/// <summary>
		/// A non-negative number up to <paramref name="_max"/>. Parsed against the invariant culture
		/// rather than the player's, so "0.4" means the same thing on a machine whose decimal
		/// separator is a comma.
		/// </summary>
		private static bool TryNumber(string _value, string _what, float _max, out float _parsed)
		{
			if (!Config.TryMeasure(_value, out _parsed) || _parsed > _max)
			{
				Output("'" + _value + "' is not a valid " + _what + " - a number from 0 to "
					+ Format.Number(_max) + ".");
				_parsed = 0f;
				return false;
			}
			return true;
		}

		/// <summary>A percentage 0 to 100, handed back as a fraction.</summary>
		private static bool TryPercent(string _value, out float _fraction)
		{
			if (!Config.TryPercent(_value, out _fraction))
			{
				Output("'" + _value + "' is not a valid chance - a percentage from 0 to 100.");
				return false;
			}
			return true;
		}

		/// <summary>A whole number, zero or more. Zero switches the gate off entirely.</summary>
		private static bool TryCount(string _value, string _what, out int _parsed)
		{
			if (!Config.TryCount(_value, out _parsed))
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

		private static string FlavorChoices()
		{
			return Choices(Mark("on", Settings.Flavor), Mark("off", !Settings.Flavor));
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
			return "Usage: sb [chance {mult}|window {s}|narrow {w} {h}|ground {n}|zombie|arrow {mult}"
				+ "|tire {s} {pct}|door {pct}|flavor|blocks|add {pattern}|drop {pattern}|probe|info|reset]"
				+ "\r\n\r\nFour ways to take a zombie's legs out from under it, each playing one of the "
				+ "game's own stumble animations. A zombie that has just scrambled onto a fence, a "
				+ "railing, a pole or any other narrow block has not found its balance yet: hit it in "
				+ "the leg in that moment. An arrow or bolt to the leg of a zombie at a run. A tire "
				+ "you have just put down in its path. And, with DoorSlammer installed, a door "
				+ "slammed in its face. The two leg-hit chances are the shot's or swing's own "
				+ "dismember chance times a multiplier, so they grow with the weapon skill Undead "
				+ "Legacy levels by use - Clubs, Blades, Archery and so on."
				+ "\r\n\r\n'sb' on its own toggles the mod and prints the settings. Each line names "
				+ "the command that changes it, so the settings block is the menu.\r\n\r\n"
				+ "'sb chance {mult}' sets the multiplier on the swing's dismember chance for the "
				+ "perch trip, 2 by default. Under UL a weapon skill of 1 gives 0.25% dismember, 100 "
				+ "gives 25%, so x2 runs from one trip in two hundred leg hits to one in two.\r\n\r\n"
				+ "'sb window {s}' sets how close to a zombie's landing a leg hit has to be, half a "
				+ "second either side by default. A hit while it is still in the air is held and "
				+ "judged when it comes down, on whatever it lands on. 0 drops the check, so any "
				+ "zombie standing on a narrow block can be tripped.\r\n\r\n'sb narrow {w} {h}' sets "
				+ "what counts as narrow: the thinner horizontal extent of the block's bounding box "
				+ "at most w blocks wide, and the box at least h tall. 0.4 and 0.5 by default. Every "
				+ "shape's box is measured from its model when the game loads; 'sb probe' shows the "
				+ "numbers for the block under your crosshair, with the verdict and the reason."
				+ "\r\n\r\n'sb ground {n}' limits perch trips to within n blocks of ground level, 3 by "
				+ "default, measured against the terrain height rather than whatever is built on "
				+ "it. This keeps trips to yard fences and away from rooftop catwalks, where a "
				+ "stumble is both more punishing and less plausible.\r\n\r\n'sb zombie' cycles what "
				+ "a tripping zombie does, whatever tripped it: off, stumble (it staggers and "
				+ "recovers) or ragdoll (it goes down properly). Both are the game's own reactions, "
				+ "and neither deals damage nor triggers rage.\r\n\r\n'sb arrow {mult}' sets the "
				+ "multiplier on the shot's dismember chance for an arrow or bolt to the leg of a "
				+ "running zombie, 2 by default. Running means the zombie is one the game currently "
				+ "has running - feral, night, blood moon, or the ZombieMove setting - and it is "
				+ "actually moving; 'sb info' shows both for the last arrow. Any bow or crossbow "
				+ "counts, no fence needed. 0 switches the rule off.\r\n\r\n'sb tire {s} {pct}' sets "
				+ "how long a tire you put down stays a trip hazard, 5 seconds by default, and the "
				+ "chance a zombie stepping into it trips, 50% by default. One roll per zombie per "
				+ "tire. Under Undead Legacy every tire in the world can be picked up, so it is a "
				+ "matter of carrying one and dropping it in the right place - or at a zombie's feet. "
				+ "0 seconds switches tires off.\r\n\r\n'sb door {pct}' sets the chance a zombie "
				+ "caught in a slammed door trips, 50% by default. Needs DoorSlammer installed and "
				+ "'sb flavor' on. 0 switches it off.\r\n\r\n'sb flavor' toggles the extra behaviour "
				+ "supported mods offer, on by default. It does nothing unless one of them is "
				+ "installed. Currently that is DoorSlammer: a slam that catches a zombie hands it "
				+ "over for the door roll above. All the mods that link up carry this switch and "
				+ "toggling it in any one of them moves all of them.\r\n\r\n'sb blocks' prints the "
				+ "name overrides: substrings of a block or shape name that count as narrow "
				+ "regardless of their box, like a hedge, and ones that never count, like a fence "
				+ "door. 'sb add' and 'sb drop' edit the first list.\r\n\r\nEvery setting here takes "
				+ "effect immediately and is written straight to a settings file, so it survives a "
				+ "restart - and survives updating the mod, because the file lives in the game's "
				+ "user data folder next to Saves rather than in Mods. 'sb info' prints its full "
				+ "path. It is plain 'key = value' text and can be edited by hand with the game "
				+ "closed; a line that will not parse is ignored rather than fatal, and deleting the "
				+ "file goes back to the built-in defaults in Settings.cs.\r\n\r\n'sb info' prints "
				+ "the same block with the patch state and the counters added. There is one counter "
				+ "per gate, in the order they are checked: leg hits but none in the air or after "
				+ "landing means you were too slow or the zombie walked up rather than jumped; in the "
				+ "window but none on a narrow block means the block is not measuring narrow - probe "
				+ "it. 'last footing' says what the mod saw under the zombie on the most recent "
				+ "check; 'last arrow' and 'last tire' do the same for those rules.\r\n\r\n'sb reset' "
				+ "zeroes the counters so one scenario can be measured on its own.\r\n\r\n'stumblr' "
				+ "is an alias for 'sb'.";
		}
	}
}
