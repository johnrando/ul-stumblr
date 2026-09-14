using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Stumblr
{
	/// <summary>
	/// Reads <see cref="Settings"/> back at startup and writes it out whenever an <c>sb</c> command
	/// changes something. The file lives in the game's user data folder rather than in the mod
	/// folder, so it survives a mod update. Plain <c>key = value</c> text; every line names the
	/// console command that writes it. Nothing here can stop the mod working: any failure degrades
	/// to a log line and the defaults.
	///
	/// Chances are written as percentages, the way <c>sb tire</c> and <c>sb door</c> take them.
	/// </summary>
	internal static class Config
	{
		private const string FolderName = "Stumblr";

		private const string FileName = "settings.txt";

		/// <summary>What the last load or save did, as reported by <c>sb info</c>.</summary>
		internal static string Status = "not loaded - mod init has not run";

		/// <summary>Where the file is, once resolved. Null means it never was.</summary>
		private static string filePath;

		/// <summary>
		/// Called once from <see cref="ModApi.InitMod"/>, before the patches go in, so the startup
		/// log reports the player's settings rather than the defaults. A missing file is a first
		/// run: writing the defaults out is what makes the file discoverable.
		/// </summary>
		internal static void Load()
		{
			if (!Resolve())
			{
				return;
			}

			if (!File.Exists(filePath))
			{
				Save();
				return;
			}

			try
			{
				int applied = 0;
				int rejected = 0;
				foreach (string line in File.ReadAllLines(filePath))
				{
					switch (Parse(line))
					{
					case LineResult.Applied:
						applied++;
						break;
					case LineResult.Rejected:
						rejected++;
						break;
					}
				}

				NarrowBlocks.RulesChanged();
				Status = applied + " settings loaded"
					+ (rejected > 0 ? ", " + rejected + " line(s) ignored" : "")
					+ " - " + filePath;
				Log.Out(Patches.LogPrefix + "Settings loaded from " + filePath + ".");
			}
			catch (Exception e)
			{
				Status = "NOT LOADED - " + e.Message;
				Log.Warning(Patches.LogPrefix + "Could not read " + filePath + ", so the built-in "
					+ "defaults are in force: " + e.Message);
			}
		}

		/// <summary>
		/// Writes the whole file, which is what keeps the comments and ordering intact. Called by
		/// every <c>sb</c> command that changes a setting and by <see cref="FlavorInterop.SetFlavor"/>.
		/// </summary>
		internal static void Save()
		{
			if (!Resolve())
			{
				return;
			}

			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
				File.WriteAllText(filePath, Compose());
				Status = "saved - " + filePath;
			}
			catch (Exception e)
			{
				Status = "NOT SAVED - " + e.Message;
				Log.Warning(Patches.LogPrefix + "Could not write " + filePath + ", so this change "
					+ "will not survive a restart: " + e.Message);
			}
		}

		/// <summary>Works out where the file goes, once.</summary>
		private static bool Resolve()
		{
			if (filePath != null)
			{
				return true;
			}

			try
			{
				string dir = GameIO.GetUserGameDataDir();
				if (string.IsNullOrEmpty(dir))
				{
					Status = "unavailable - the game reported no user data folder";
					return false;
				}
				filePath = Path.Combine(Path.Combine(dir, FolderName), FileName);
				return true;
			}
			catch (Exception e)
			{
				Status = "unavailable - " + e.Message;
				Log.Warning(Patches.LogPrefix + "Could not work out where to keep settings, so they "
					+ "will not persist: " + e.Message);
				return false;
			}
		}

		private static string Compose()
		{
			StringBuilder text = new StringBuilder();
			text.AppendLine("# Stumblr settings.");
			text.AppendLine("#");
			text.AppendLine("# Read once when the game starts and rewritten whenever an 'sb' command changes");
			text.AppendLine("# something, so edit this with the game closed. Every line names the command that");
			text.AppendLine("# sets it; anything after a '#' is a comment, and a line that will not parse is");
			text.AppendLine("# ignored rather than fatal. Chances are percentages.");
			text.AppendLine();
			Setting(text, "enabled", OnOff(Settings.Enabled), "sb on|off");
			Setting(text, "chance", Number(Settings.ChanceMultiplier), "sb chance {mult}");
			Setting(text, "window", Number(Settings.WindowSeconds), "sb window {s}");
			Setting(text, "narrow.width", Number(Settings.NarrowWidth), "sb narrow {w} {h}");
			Setting(text, "narrow.height", Number(Settings.NarrowMinHeight), "sb narrow {w} {h}");
			Setting(text, "ground", Settings.GroundBand.ToString(), "sb ground {n}");
			Setting(text, "zombie", ZombieTrip.Weights(), "sb zombie {stumble} {kneel} {prone} {ragdoll} {shove} - weights");
			Setting(text, "shove.force", Number(Settings.ShoveForce), "sb shove {force}");
			Setting(text, "stun", Number(Settings.ZombieStunSeconds), "stumble stun seconds (no command)");
			Setting(text, "arrow", Number(Settings.ArrowMultiplier), "sb arrow {mult}");
			Setting(text, "tire.seconds", Number(Settings.TireSeconds), "sb tire {s} {pct}");
			Setting(text, "tire.chance", Number(Settings.TireChance * 100f), "sb tire {s} {pct}");
			Setting(text, "tire.small", TripHazards.RuleValue(TireKind.Small), "sb tires small {x} {n} - chance multiplier, zombies per tire");
			Setting(text, "tire.single", TripHazards.RuleValue(TireKind.Single), "sb tires single {x} {n}");
			Setting(text, "tire.pile", TripHazards.RuleValue(TireKind.Pile), "sb tires pile {x} {n}");
			Setting(text, "tire.stack", TripHazards.RuleValue(TireKind.Stack), "sb tires stack {x} {n}");
			Setting(text, "door.chance", Number(Settings.DoorChance * 100f), "sb door {pct}");
			foreach (string label in FlavorSwitches.Labels)
			{
				Setting(text, "flavor." + label.ToLowerInvariant(), OnOff(FlavorSwitches.IsOn(label)),
					"sb flavor " + FlavorPartners.AliasOf(label));
			}
			Setting(text, "include", string.Join(",", Settings.Include.ToArray()), "sb add / sb drop");
			return text.ToString();
		}

		/// <summary>One setting, padded so the values and the commands each share a column.</summary>
		private static void Setting(StringBuilder _text, string _key, string _value, string _command)
		{
			_text.AppendLine(_key.PadRight(19) + "= " + _value.PadRight(8) + " # " + _command);
		}

		private enum LineResult
		{
			/// <summary>Blank or a comment.</summary>
			Skipped,

			Applied,

			Rejected
		}

		/// <summary>
		/// One line of the file. An unknown key is a warning rather than an error: that is what a
		/// file written by a newer version of the mod looks like to an older one.
		/// </summary>
		private static LineResult Parse(string _line)
		{
			int comment = _line.IndexOf('#');
			string text = (comment >= 0 ? _line.Substring(0, comment) : _line).Trim();
			if (text.Length == 0)
			{
				return LineResult.Skipped;
			}

			int split = text.IndexOf('=');
			if (split <= 0)
			{
				Log.Warning(Patches.LogPrefix + "Ignoring a settings line that is not 'key = value': "
					+ _line.Trim());
				return LineResult.Rejected;
			}

			string key = text.Substring(0, split).Trim().ToLowerInvariant();
			string value = text.Substring(split + 1).Trim();
			if (Apply(key, value))
			{
				return LineResult.Applied;
			}

			Log.Warning(Patches.LogPrefix + "Ignoring settings line '" + _line.Trim()
				+ "' - unknown setting or unusable value.");
			return LineResult.Rejected;
		}

		private static bool Apply(string _key, string _value)
		{
			switch (_key)
			{
			case "enabled":
				return TryBool(_value, ref Settings.Enabled);
			case "chance":
				return LoadMeasure(_value, ref Settings.ChanceMultiplier);
			case "window":
				return LoadMeasure(_value, ref Settings.WindowSeconds);
			case "narrow.width":
				return LoadMeasure(_value, ref Settings.NarrowWidth);
			case "narrow.height":
				return LoadMeasure(_value, ref Settings.NarrowMinHeight);
			case "ground":
				return LoadCount(_value, ref Settings.GroundBand);
			case "zombie":
				return TryZombie(_value);
			case "shove.force":
				return LoadMeasure(_value, ref Settings.ShoveForce);
			case "stun":
				return LoadMeasure(_value, ref Settings.ZombieStunSeconds);
			case "arrow":
				return LoadMeasure(_value, ref Settings.ArrowMultiplier);
			case "tire.seconds":
				return LoadMeasure(_value, ref Settings.TireSeconds);
			case "tire.chance":
				return LoadPercent(_value, ref Settings.TireChance);
			case "tire.small":
				return LoadTireRule(TireKind.Small, _value);
			case "tire.single":
				return LoadTireRule(TireKind.Single, _value);
			case "tire.pile":
				return LoadTireRule(TireKind.Pile, _value);
			case "tire.stack":
				return LoadTireRule(TireKind.Stack, _value);
			case "door.chance":
				return LoadPercent(_value, ref Settings.DoorChance);
			case "flavor":
				// The single switch older builds wrote: apply it to every partner.
				return TryFlavor(null, _value);
			case "include":
				return LoadList(_value, Settings.Include);
			default:
				// flavor.<mod>: one partner's switch. Any label is accepted, so a switch a mod
				// this build does not know about created is kept.
				return _key.StartsWith("flavor.") && _key.Length > 7
					&& TryFlavor(_key.Substring(7), _value);
			}
		}

		/// <summary>One partner's switch, or every partner's when the label is null.</summary>
		private static bool TryFlavor(string _label, string _value)
		{
			bool on = false;
			if (!TryBool(_value, ref on))
			{
				return false;
			}
			if (_label == null)
			{
				FlavorSwitches.SetAll(on);
			}
			else
			{
				FlavorSwitches.Set(_label, on);
			}
			return true;
		}

		/// <summary>Accepts what the file writes plus the obvious hand-edit synonyms.</summary>
		private static bool TryBool(string _value, ref bool _target)
		{
			switch (_value.ToLowerInvariant())
			{
			case "on":
			case "true":
			case "yes":
			case "1":
				_target = true;
				return true;
			case "off":
			case "false":
			case "no":
			case "0":
				_target = false;
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Five weights, or one of the words a file from before the table wrote: "stumble" and
		/// "ragdoll" become a table with only that reaction in it, "off" all zeros.
		/// </summary>
		private static bool TryZombie(string _value)
		{
			switch (_value.ToLowerInvariant())
			{
			case "off":
				SetWeights(0f, 0f, 0f, 0f, 0f);
				return true;
			case "stumble":
				SetWeights(1f, 0f, 0f, 0f, 0f);
				return true;
			case "ragdoll":
				SetWeights(0f, 0f, 0f, 1f, 0f);
				return true;
			}

			if (!TryWeights(_value.Split(new[] { ' ', ',' }, System.StringSplitOptions.RemoveEmptyEntries),
				out float[] weights))
			{
				return false;
			}
			SetWeights(weights[0], weights[1], weights[2], weights[3], weights[4]);
			return true;
		}

		/// <summary>Five non-negative numbers. Shared with the console command.</summary>
		internal static bool TryWeights(string[] _values, out float[] _weights)
		{
			_weights = new float[5];
			if (_values.Length != 5)
			{
				return false;
			}
			for (int i = 0; i < 5; i++)
			{
				if (!TryMeasure(_values[i], out _weights[i]) || _weights[i] > 1000f)
				{
					return false;
				}
			}
			return true;
		}

		internal static void SetWeights(float _stumble, float _kneel, float _prone, float _ragdoll, float _shove)
		{
			Settings.WeightStumble = _stumble;
			Settings.WeightKneel = _kneel;
			Settings.WeightProne = _prone;
			Settings.WeightRagdoll = _ragdoll;
			Settings.WeightShove = _shove;
		}

		/// <summary>A multiplier and a count, space or comma separated.</summary>
		private static bool LoadTireRule(TireKind _kind, string _value)
		{
			string[] parts = _value.Split(new[] { ' ', ',' }, System.StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 2 || !TryTireRule(parts[0], parts[1], out TireRule rule))
			{
				return false;
			}
			Settings.SetTireRule(_kind, rule);
			return true;
		}

		/// <summary>Shared with the console command.</summary>
		internal static bool TryTireRule(string _multiplier, string _count, out TireRule _rule)
		{
			_rule = default(TireRule);
			if (!TryMeasure(_multiplier, out float multiplier) || multiplier > 100f
				|| !TryCount(_count, out int count) || count > 1000)
			{
				return false;
			}
			_rule = new TireRule(multiplier, count);
			return true;
		}

		/// <summary>Whole numbers, zero or more. Shared with the console command.</summary>
		internal static bool TryCount(string _value, out int _parsed)
		{
			return int.TryParse(_value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _parsed)
				&& _parsed >= 0;
		}

		private static bool LoadCount(string _value, ref int _target)
		{
			if (!TryCount(_value, out int parsed))
			{
				return false;
			}
			_target = parsed;
			return true;
		}

		/// <summary>
		/// Seconds, blocks or multipliers, zero or more, against the invariant culture so a file
		/// written on one machine means the same on one whose decimal separator is a comma. Shared
		/// with the console command. <c>!(x &gt;= 0)</c> rather than <c>x &lt; 0</c> so NaN is
		/// rejected too.
		/// </summary>
		internal static bool TryMeasure(string _value, out float _parsed)
		{
			return float.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out _parsed)
				&& _parsed >= 0f && !float.IsInfinity(_parsed);
		}

		private static bool LoadMeasure(string _value, ref float _target)
		{
			if (!TryMeasure(_value, out float parsed))
			{
				return false;
			}
			_target = parsed;
			return true;
		}

		/// <summary>A percentage 0 to 100, stored as a fraction. Shared with the console command.</summary>
		internal static bool TryPercent(string _value, out float _fraction)
		{
			if (!TryMeasure(_value, out float percent) || percent > 100f)
			{
				_fraction = 0f;
				return false;
			}
			_fraction = percent / 100f;
			return true;
		}

		private static bool LoadPercent(string _value, ref float _target)
		{
			if (!TryPercent(_value, out float parsed))
			{
				return false;
			}
			_target = parsed;
			return true;
		}

		/// <summary>A comma-separated list of name substrings, replacing the list's contents. An
		/// empty value empties it, which is a legitimate choice.</summary>
		private static bool LoadList(string _value, List<string> _target)
		{
			_target.Clear();
			foreach (string part in _value.Split(','))
			{
				string pattern = part.Trim().ToLowerInvariant();
				if (pattern.Length > 0 && !_target.Contains(pattern))
				{
					_target.Add(pattern);
				}
			}
			return true;
		}

		private static string OnOff(bool _on)
		{
			return _on ? "on" : "off";
		}

		/// <summary>Written the way it is parsed, so a reported value can be typed back in.</summary>
		internal static string Number(float _value)
		{
			return _value.ToString(CultureInfo.InvariantCulture);
		}
	}
}
