namespace Stumblr
{
	/// <summary>
	/// Live counters behind <c>sb info</c>. The startup log only proves the patches were installed;
	/// these are what prove they are being reached.
	///
	/// There is one counter per gate, in the order the gates are checked, because that is what makes
	/// a mis-tuned setting diagnosable without a debugger: the stage where the number stops moving
	/// names the setting to change. Leg hits but none in the window means the window is too tight;
	/// in the window but none on a narrow block means the block you are testing on is not
	/// measuring narrow - <c>sb probe</c> it.
	///
	/// No locking: all writes happen on the main thread, from the two hooks.
	/// </summary>
	internal static class Counters
	{
		/// <summary>Every zombie landing the landing hook saw. The proof it is live.</summary>
		internal static int Landings;

		/// <summary>Every hit on a non-remote zombie the damage hook saw. The proof it is live.</summary>
		internal static int ZombieHits;

		internal static int PlayerHits;

		internal static int LegHits;

		/// <summary>Leg hits taken mid-jump and parked for the landing.</summary>
		internal static int LegHitsInAir;

		/// <summary>Of those, the ones whose landing came inside the window.</summary>
		internal static int AirHitsLanded;

		/// <summary>Leg hits taken on the ground inside the window after a landing.</summary>
		internal static int LegHitsInWindow;

		internal static int LegHitsOnNarrow;

		internal static int Trips;

		internal static int ZombieRagdolls;

		internal static void Reset()
		{
			Landings = 0;
			ZombieHits = 0;
			PlayerHits = 0;
			LegHits = 0;
			LegHitsInAir = 0;
			AirHitsLanded = 0;
			LegHitsInWindow = 0;
			LegHitsOnNarrow = 0;
			Trips = 0;
			ZombieRagdolls = 0;
		}
	}
}
