using UnityEngine;

namespace Stumblr
{
	/// <summary>
	/// Keeps trips near the ground.
	///
	/// The measure is <c>World.GetTerrainHeight</c>, not <c>GetHeight</c>, and the difference is the
	/// point: GetTerrainHeight is the height of the *terrain*, ignoring anything built on it, so a
	/// yard fence reads as at grade while the railing around a fifth-floor catwalk reads as forty
	/// blocks up. GetHeight includes built blocks and would call both of them ground level.
	///
	/// This exists because a stumble is worst exactly where it is least plausible - up on a walkway,
	/// where the ground you catch a foot on is a metal grate and the consequence of a stagger is a
	/// long drop.
	/// </summary>
	internal static class GroundLevel
	{
		internal static bool IsNearGround(World _world, Vector3 _pos)
		{
			if (_world == null)
			{
				return false;
			}

			byte terrainY = _world.GetTerrainHeight(Utils.Fastfloor(_pos.x), Utils.Fastfloor(_pos.z));

			// Zero means the chunk is not loaded, not that the terrain is at sea level. Unknown, so
			// no trip - the alternative is tripping everyone standing over an unloaded chunk.
			if (terrainY == 0)
			{
				return false;
			}

			return Mathf.Abs(_pos.y - terrainY) <= Settings.GroundBand;
		}

		/// <summary>The <c>sb</c> menu line.</summary>
		internal static string Status()
		{
			return "only within " + Settings.GroundBand + " blocks of ground level";
		}
	}
}
