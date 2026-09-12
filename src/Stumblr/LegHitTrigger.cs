namespace Stumblr
{
	/// <summary>
	/// Trips a zombie that has just scrambled onto a fence and takes a hit to the leg, by
	/// postfixing <c>EntityAlive.DamageEntity</c>.
	///
	/// DamageEntity and not ProcessDamageResponseLocal, and that is the whole compatibility story.
	/// Undead Legacy replaces ProcessDamageResponseLocal wholesale - a prefix that returns false
	/// on EntityAlive (H_Equipment) and another on EntityHuman (H_RageChancePatch) - so a patch
	/// there would never run under UL. DamageEntity is the authoritative entry every hit funnels
	/// through: EntityHuman and EntityEnemy override it and both call base, and UL patches only the
	/// EntityPlayer overrides, for its camera shake. It is also where the body part is known:
	/// vanilla builds its DamageResponse from <c>_damageSource.GetEntityDamageBodyPart</c>, a tag
	/// lookup on the collider the swing hit, and the same call is cheap enough to repeat here.
	///
	/// The chance comes from the swing itself. <c>DamageSource.DismemberChance</c> is filled by the
	/// attack from the game's DismemberChance passive, with every perk already applied - under UL
	/// that is the weapon's action skill, Clubs or Blades or Brawler, from 0.25% at level 1 to 25%
	/// at 100, plus its skill books. Taking a limb off and taking the legs out from under are the
	/// same kind of luck, so the trip chance is that number times a multiplier.
	///
	/// A hit that lands while the zombie is still in the air is handed to <see cref="ZombieLanding"/>
	/// to settle when it comes down; one that lands after must fall inside the window.
	/// </summary>
	internal static class LegHitTrigger
	{
		/// <summary>The last dismember chance seen on a qualifying hit, for <c>sb info</c>.</summary>
		internal static string LastRoll = "no leg hit on a perched zombie yet";

		internal static void Postfix(EntityAlive __instance, DamageSource _damageSource)
		{
			if (!Settings.Enabled || __instance == null || _damageSource == null)
			{
				return;
			}

			// The entityFlags bit rather than `is EntityZombie`, because it comes from
			// entityclasses.xml: it covers zombie dogs and Undead Legacy's own zombies for free, and
			// excludes bandits.
			if ((__instance.entityFlags & EntityFlags.Zombie) == EntityFlags.None)
			{
				return;
			}

			// The move helper and the stun run on the authoritative side. On a dedicated-server
			// client the zombie is remote and DamageEntity still runs, for the visuals; a stun from
			// there would be overwritten by the next position update.
			if (__instance.isEntityRemote)
			{
				return;
			}

			Counters.ZombieHits++;

			World world = __instance.world;
			if (world == null || !(world.GetEntity(_damageSource.getEntityId()) is EntityPlayer))
			{
				return;
			}

			Counters.PlayerHits++;

			if (!_damageSource.GetEntityDamageBodyPart(__instance).IsLeg())
			{
				return;
			}

			Counters.LegHits++;

			// Dead, already down, or a crawler - the game's own stumble path has nothing to play
			// for a rig that is already on the floor (walkType 21), and neither does this.
			if (__instance.IsDead() || __instance.bodyDamage.CurrentStun != EnumEntityStunType.None
				|| __instance.walkType == 21)
			{
				return;
			}

			float chance = _damageSource.DismemberChance * Settings.ChanceMultiplier;

			// Still in the air: nothing to stand on yet, so the landing decides. Only worth parking
			// when there is a window for it to land inside.
			if (__instance.Jumping && Settings.WindowSeconds > 0f)
			{
				Counters.LegHitsInAir++;
				ZombieLanding.RecordAirHit(__instance.entityId, chance);
				return;
			}

			if (!ZombieLanding.LandedWithin(__instance.entityId, Settings.WindowSeconds))
			{
				return;
			}

			Counters.LegHitsInWindow++;
			TryTrip(__instance, chance);
		}

		/// <summary>
		/// The footing and the roll, shared by a hit judged on the spot and one settled on landing.
		/// <paramref name="_chance"/> is a fraction, 0 to 1.
		/// </summary>
		internal static void TryTrip(EntityAlive _zombie, float _chance)
		{
			if (!GroundLevel.IsNearGround(_zombie.world, _zombie.position))
			{
				return;
			}

			if (!NarrowBlocks.IsStandingOnNarrow(_zombie))
			{
				return;
			}

			Counters.LegHitsOnNarrow++;

			LastRoll = Format.Percent(_chance * 100f) + " (dismember chance "
				+ Format.Times(Settings.ChanceMultiplier) + ")";

			// Checked here rather than up front so the counters above still move with trips
			// switched off - which is what makes "the hook is live but nothing is tripping"
			// diagnosable.
			if (Settings.ZombieMode == ZombieReaction.Off || _chance <= 0f)
			{
				return;
			}

			if (_zombie.rand.RandomFloat >= _chance)
			{
				return;
			}

			Counters.Trips++;
			ZombieTrip.Apply(_zombie);
		}

		/// <summary>The <c>sb chance</c> menu line.</summary>
		internal static string Status()
		{
			return "trip chance = swing's dismember chance " + Format.Times(Settings.ChanceMultiplier);
		}
	}
}
