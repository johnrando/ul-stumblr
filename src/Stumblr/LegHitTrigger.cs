namespace Stumblr
{
	/// <summary>
	/// The two leg-hit rules, both hung off a postfix on <c>EntityAlive.DamageEntity</c>: an arrow
	/// or bolt to the leg of a running zombie, and any leg hit on a zombie that has just scrambled
	/// onto a fence.
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
	/// that is the weapon's action skill, Clubs or Blades or Archery, from 0.25% at level 1 to 25%
	/// at 100, plus its skill books. Taking a limb off and taking the legs out from under are the
	/// same kind of luck, so each trip chance is that number times a multiplier.
	///
	/// A projectile is told apart by what fired it. <c>ProjectileMoveScript</c> hands
	/// <c>ItemActionAttack.Hit</c> the launcher - the bow or crossbow - as the damaging item, with a
	/// Piercing damage type, so a hit whose item carries an <c>ItemActionLauncher</c> is an arrow or
	/// a bolt. That is a structural test rather than a tag list, so Undead Legacy's bows and any
	/// modded one count without being named.
	///
	/// "Running" is two things at once: <c>EntityHuman.IsRunning</c>, the game's own answer to
	/// whether this zombie is currently a runner (feral, night, blood moon, or the ZombieMove
	/// setting), and <c>speedForward</c> above the threshold the game itself uses to tell moving
	/// from standing. A runner that has stopped is not running.
	///
	/// A perch hit that lands while the zombie is still in the air is handed to
	/// <see cref="ZombieLanding"/> to settle when it comes down; one that lands after must fall
	/// inside the window.
	/// </summary>
	internal static class LegHitTrigger
	{
		/// <summary>The game's own moving-vs-idle threshold on speedForward (EntityAlive.OnUpdateEntity).</summary>
		private const float MovingSpeed = 0.01942f;

		/// <summary>The last dismember chance seen on a qualifying perch hit, for <c>sb info</c>.</summary>
		internal static string LastRoll = "no leg hit on a perched zombie yet";

		/// <summary>The last arrow or bolt to a leg, for <c>sb info</c>.</summary>
		internal static string LastArrow = "no arrow to a leg yet";

		internal static void Postfix(EntityAlive __instance, DamageSource _damageSource)
		{
			if (!Settings.Enabled || __instance == null || _damageSource == null)
			{
				return;
			}

			if ((__instance.entityFlags & EntityFlags.Zombie) == EntityFlags.None)
			{
				return;
			}

			// On a dedicated-server client the zombie is remote and DamageEntity still runs, for
			// the visuals; nothing can be tripped from there.
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

			if (!ZombieTrip.CanTrip(__instance))
			{
				return;
			}

			float dismember = _damageSource.DismemberChance;

			if (IsLauncherShot(_damageSource) && TryArrowTrip(__instance, dismember))
			{
				return;
			}

			float chance = dismember * Settings.ChanceMultiplier;

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

		/// <summary>Whether this hit was an arrow or bolt: Piercing, from an item that launches.</summary>
		private static bool IsLauncherShot(DamageSource _damageSource)
		{
			if (_damageSource.GetDamageType() != EnumDamageTypes.Piercing)
			{
				return false;
			}

			ItemValue item = _damageSource.AttackingItem;
			ItemClass itemClass = item == null ? null : item.ItemClass;
			if (itemClass == null || itemClass.Actions == null)
			{
				return false;
			}

			for (int i = 0; i < itemClass.Actions.Length; i++)
			{
				if (itemClass.Actions[i] is ItemActionLauncher)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// The arrow rule. Returns true when a trip played, so the perch rule does not roll again
		/// on the same hit; a failed roll falls through to it.
		/// </summary>
		private static bool TryArrowTrip(EntityAlive _zombie, float _dismember)
		{
			Counters.ArrowLegHits++;

			bool runner = _zombie.IsRunning;
			float speed = _zombie.speedForward;
			bool running = runner && speed > MovingSpeed;
			float chance = _dismember * Settings.ArrowMultiplier;

			LastArrow = "dismember " + Format.Percent(_dismember * 100f) + " "
				+ Format.Times(Settings.ArrowMultiplier) + " = " + Format.Percent(chance * 100f)
				+ ", " + (running ? "running" : runner ? "a runner, but standing still" : "not a runner")
				+ " (speed " + Format.Number(speed) + ")";

			if (!running)
			{
				return false;
			}

			Counters.ArrowRunningHits++;

			if (!ZombieTrip.Active || chance <= 0f
				|| _zombie.rand.RandomFloat >= chance)
			{
				return false;
			}

			Counters.ArrowTrips++;
			ZombieTrip.Apply(_zombie);
			return true;
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
			if (!ZombieTrip.Active || _chance <= 0f)
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
			return "perch trip chance = swing's dismember chance " + Format.Times(Settings.ChanceMultiplier);
		}

		/// <summary>The <c>sb arrow</c> menu line.</summary>
		internal static string ArrowStatus()
		{
			if (Settings.ArrowMultiplier <= 0f)
			{
				return "off - an arrow to the leg is just an arrow to the leg";
			}

			return "arrow to a running zombie's leg trips at dismember chance "
				+ Format.Times(Settings.ArrowMultiplier);
		}
	}
}
