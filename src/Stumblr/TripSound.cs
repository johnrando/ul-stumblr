namespace Stumblr
{
	/// <summary>
	/// The noise a trip makes.
	///
	/// Nothing is bundled and nothing is hardcoded: the sound is whatever the player's own entity
	/// class names as its SoundHurtSmall, which is player1painsm for a male character and
	/// player2painsm for a female one, five clips each. Reading it off the entity is the same trick
	/// Door Slammer uses with a door's SurfaceCategory - the right voice without the mod knowing
	/// anything about voices, and a custom character with its own pain sounds gets its own.
	///
	/// Unlike Door Slammer and Fletch Wounds, this one *is* audible to the AI. Those two go out of
	/// their way to play unattributed, with the entity id left at -1, so a slam or a pull makes no
	/// AI noise and no screamer heat. A grunt cannot do that: it has to come out of the player's own
	/// audio source to sound like it came from the player, which means it carries the sound node's
	/// noise value (11) exactly as taking a hit does. That is the right answer here - the player
	/// made a noise - but it is a real difference from the siblings and is documented as one.
	/// </summary>
	internal static class TripSound
	{
		/// <summary>
		/// What the last trip played, reported by <c>sb info</c>. Worth reporting because an unknown
		/// sound name fails silently by design, so this is the only way to see the name a trip
		/// actually asked for.
		/// </summary>
		internal static string LastPlayed = "nothing yet";

		internal static void Play(EntityPlayerLocal _player)
		{
			if (!Settings.PlaySound)
			{
				LastPlayed = "nothing - sound is off";
				return;
			}

			string soundName = _player.GetSoundHurtSmall();
			if (string.IsNullOrEmpty(soundName))
			{
				LastPlayed = "nothing - this character has no SoundHurtSmall";
				return;
			}

			// An unknown sound name is not an error and not a log line: the manager returns at its
			// audioData lookup. That is what makes reading a name off the entity class safe.
			_player.PlayOneShot(soundName);
			LastPlayed = soundName;
		}

		/// <summary>Just the mode, for the <c>sb</c> settings block.</summary>
		internal static string Status()
		{
			return Settings.PlaySound ? "on" : "off";
		}

		/// <summary>The line <c>sb hurt</c> prints after toggling.</summary>
		internal static string Describe()
		{
			return Settings.PlaySound
				? "Trip grunt ON - your character's own SoundHurtSmall, the small-pain sound. "
					+ "Audible to zombies, exactly as taking a hit is."
				: "Trip grunt OFF - a trip is silent.";
		}
	}
}
