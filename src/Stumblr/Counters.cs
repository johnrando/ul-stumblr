namespace Stumblr
{
	/// <summary>
	/// Live counters behind <c>sb info</c>. The startup log only proves the patches were installed;
	/// these are what prove they are being reached.
	///
	/// There is one counter per gate, in the order the gates are checked, because that is what makes
	/// a mis-tuned setting diagnosable without a debugger: the stage where the number stops moving
	/// names the setting to change. Jumps but no near-ground means the band is too tight; near-ground
	/// but nothing trippable means the block list is missing the fence you are standing at.
	///
	/// No locking: all writes happen on the main thread, from the two jump hooks.
	/// </summary>
	internal static class Counters
	{
		/// <summary>Every jump either hook saw, player or zombie. The proof they are live.</summary>
		internal static int JumpsSeen;

		internal static int PlayerJumps;

		internal static int PlayerNearGround;

		internal static int PlayerOverTrippable;

		internal static int PlayerTrips;

		internal static int ZombieJumps;

		internal static int ZombieNearGround;

		internal static int ZombieOverTrippable;

		internal static int ZombieTrips;

		/// <summary>Of the trips, the ones caught before the jump.</summary>
		internal static int ZombieNearSide;

		/// <summary>Of the trips, the ones that cleared it and went down on landing.</summary>
		internal static int ZombieFarSide;

		internal static int ZombieRagdolls;

		internal static void Reset()
		{
			JumpsSeen = 0;
			PlayerJumps = 0;
			PlayerNearGround = 0;
			PlayerOverTrippable = 0;
			PlayerTrips = 0;
			ZombieJumps = 0;
			ZombieNearGround = 0;
			ZombieOverTrippable = 0;
			ZombieTrips = 0;
			ZombieNearSide = 0;
			ZombieFarSide = 0;
			ZombieRagdolls = 0;
		}
	}
}
