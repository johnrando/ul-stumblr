namespace Stumblr
{
	/// <summary>
	/// What a tripped zombie does, whatever tripped it: a leg hit on a fence, an arrow at a run, a
	/// tire underfoot or a door in the face all end here.
	///
	/// Nothing here is invented. Both reactions are lifted from
	/// <c>EntityHuman.ExecuteDestroyBlockBehavior</c> - the game's own "tripped while breaking
	/// through something" response, which is already weighted into every zombie's
	/// DestroyBlockBehavior table in entityclasses.xml. Replaying it rather than writing a new one
	/// means a trip looks like the game, animates correctly on every zombie rig, and works on
	/// modded zombies the mod has never seen.
	///
	/// The two ClearBlocked/ClearTempMove calls are part of that copied sequence and are not
	/// incidental: they drop the move helper's idea of what it was doing, so the zombie re-plans
	/// after the stagger instead of resuming its walk into the fence.
	///
	/// A trip deals no damage, grants no XP, sets no revenge target and triggers no rage. Vanilla's
	/// version can start a rage, from the RagePer/RageTime fields on the behaviour it was handed;
	/// there is no behaviour here, so that branch is simply not copied.
	/// </summary>
	internal static class ZombieTrip
	{
		/// <summary>
		/// Whether this entity is one a trip can be played on right now. The shared front gate for
		/// every trigger.
		///
		/// The entityFlags bit rather than <c>is EntityZombie</c>, because it comes from
		/// entityclasses.xml: it covers zombie dogs and Undead Legacy's own zombies for free, and
		/// excludes bandits and the player.
		///
		/// Remote entities are skipped because the move helper and the stun run on the
		/// authoritative side: on a dedicated-server client a stun set here would be overwritten by
		/// the next position update.
		///
		/// Dead, already stunned, or a crawler (walkType 21) - the game's own stumble path has
		/// nothing to play for a rig that is already on the floor, and neither does this.
		/// </summary>
		internal static bool CanTrip(EntityAlive _entity)
		{
			if (_entity == null || (_entity.entityFlags & EntityFlags.Zombie) == EntityFlags.None)
			{
				return false;
			}

			if (_entity.isEntityRemote || _entity.IsDead())
			{
				return false;
			}

			return _entity.bodyDamage.CurrentStun == EnumEntityStunType.None && _entity.walkType != 21;
		}

		internal static void Apply(EntityAlive _zombie)
		{
			if (_zombie.emodel == null || _zombie.emodel.avatarController == null
				|| _zombie.moveHelper == null)
			{
				return;
			}

			_zombie.moveHelper.ClearBlocked();
			_zombie.moveHelper.ClearTempMove();

			// Feeds the animator's random variation, so two zombies caught on the same fence do not
			// play the same stumble.
			_zombie.emodel.avatarController.UpdateInt("RandomSelector", _zombie.rand.RandomRange(0, 64));

			if (Settings.ZombieMode == ZombieReaction.Ragdoll)
			{
				_zombie.emodel.avatarController.BeginStun(EnumEntityStunType.StumbleBreakThroughRagdoll,
					EnumBodyPartHit.LeftUpperLeg, Utils.EnumHitDirection.None, _criticalHit: false, 1f);
				_zombie.SetStun(EnumEntityStunType.StumbleBreakThroughRagdoll);

				// No StunDuration is set, matching vanilla: the ragdoll ends when the body settles,
				// not on a timer.
				Counters.ZombieRagdolls++;
				return;
			}

			_zombie.emodel.avatarController.BeginStun(EnumEntityStunType.StumbleBreakThrough,
				EnumBodyPartHit.LeftUpperLeg, Utils.EnumHitDirection.None, _criticalHit: false, 1f);
			_zombie.SetStun(EnumEntityStunType.StumbleBreakThrough);
			_zombie.bodyDamage.StunDuration = Settings.ZombieStunSeconds;
		}

		/// <summary>Just the mode, for the <c>sb</c> settings block and the settings file.</summary>
		internal static string Status()
		{
			switch (Settings.ZombieMode)
			{
			case ZombieReaction.Stumble:
				return "stumble";
			case ZombieReaction.Ragdoll:
				return "ragdoll";
			default:
				return "off";
			}
		}

		/// <summary>Off -&gt; Stumble -&gt; Ragdoll -&gt; Off.</summary>
		internal static void Cycle()
		{
			switch (Settings.ZombieMode)
			{
			case ZombieReaction.Off:
				Settings.ZombieMode = ZombieReaction.Stumble;
				break;
			case ZombieReaction.Stumble:
				Settings.ZombieMode = ZombieReaction.Ragdoll;
				break;
			default:
				Settings.ZombieMode = ZombieReaction.Off;
				break;
			}
		}

		/// <summary>The line <c>sb zombie</c> prints after cycling.</summary>
		internal static string Describe()
		{
			switch (Settings.ZombieMode)
			{
			case ZombieReaction.Stumble:
				return "Zombies STUMBLE - legs taken out from under them, staggered for "
					+ Settings.ZombieStunSeconds + "s, back on their feet. The game's own "
					+ "StumbleBreakThrough reaction.";
			case ZombieReaction.Ragdoll:
				return "Zombies RAGDOLL - knocked clean off their feet. The game's own "
					+ "StumbleBreakThroughRagdoll reaction; it ends when the body settles.";
			default:
				return "Zombie trips OFF - every trigger still counts in 'sb info', but nothing plays.";
			}
		}
	}
}
