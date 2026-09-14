using UnityEngine;

namespace Stumblr
{
	/// <summary>
	/// What a tripped zombie does, whatever tripped it: a leg hit on a fence, an arrow at a run, a
	/// tire underfoot or a door in the face all end here. One of five reactions is drawn from a
	/// weighted table; see <see cref="Settings"/> for the weights.
	///
	/// Nothing here is invented. Every reaction is one the game already plays on every zombie rig:
	///
	/// - stumble and ragdoll are lifted from <c>EntityHuman.ExecuteDestroyBlockBehavior</c>, the
	///   "tripped while breaking through something" response. Both lurch forward, because that is
	///   the animation: a zombie that has just smashed through a block falls into the hole.
	/// - kneel and prone are the knockdown stuns a heavy hit deals in <c>EntityAlive.ProcessDamageResponse</c>.
	///   They take a hit direction, so unlike the break-through pair they can put the zombie down
	///   away from the player.
	/// - shove is the same call the game makes for a critical bashing knockdown: a physics ragdoll
	///   with an impulse, aimed here backward along the zombie's own facing with a random lean to
	///   one side, so it goes over away from where it was heading.
	///
	/// The two ClearBlocked/ClearTempMove calls are part of the copied sequence and are not
	/// incidental: they drop the move helper's idea of what it was doing, so the zombie re-plans
	/// after the stagger instead of resuming its walk into the fence.
	///
	/// A trip deals no damage, grants no XP, sets no revenge target and triggers no rage. Vanilla's
	/// version can start a rage, from the RagePer/RageTime fields on the behaviour it was handed;
	/// there is no behaviour here, so that branch is simply not copied.
	/// </summary>
	internal static class ZombieTrip
	{
		/// <summary>The reaction the table came up with; the counters and <c>sb info</c> use the names.</summary>
		internal enum Reaction
		{
			Stumble,
			Kneel,
			Prone,
			Ragdoll,
			Shove
		}

		/// <summary>What the most recent trip played, for <c>sb info</c>.</summary>
		internal static string LastReaction = "no trip yet";

		/// <summary>False when every weight is 0 - the "off" state. Every trigger checks this
		/// before rolling, so the counters still move but nothing plays.</summary>
		internal static bool Active
		{
			get { return TotalWeight() > 0f; }
		}

		private static float TotalWeight()
		{
			return Settings.WeightStumble + Settings.WeightKneel + Settings.WeightProne
				+ Settings.WeightRagdoll + Settings.WeightShove;
		}

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

			Reaction reaction = Roll(_zombie.rand);

			_zombie.moveHelper.ClearBlocked();
			_zombie.moveHelper.ClearTempMove();

			// Feeds the animator's random variation, so two zombies caught on the same fence do not
			// play the same stumble.
			_zombie.emodel.avatarController.UpdateInt("RandomSelector", _zombie.rand.RandomRange(0, 64));

			switch (reaction)
			{
			case Reaction.Kneel:
				Knockdown(_zombie, EnumEntityStunType.Kneel, KneelSeconds(_zombie));
				Counters.ZombieKneels++;
				break;

			case Reaction.Prone:
				Knockdown(_zombie, EnumEntityStunType.Prone, ProneSeconds(_zombie));
				Counters.ZombieProne++;
				break;

			case Reaction.Ragdoll:
				_zombie.emodel.avatarController.BeginStun(EnumEntityStunType.StumbleBreakThroughRagdoll,
					EnumBodyPartHit.LeftUpperLeg, Utils.EnumHitDirection.None, _criticalHit: false, 1f);
				_zombie.SetStun(EnumEntityStunType.StumbleBreakThroughRagdoll);

				// No StunDuration is set, matching vanilla: the ragdoll ends when the body settles,
				// not on a timer.
				Counters.ZombieRagdolls++;
				break;

			case Reaction.Shove:
				if (Shove(_zombie))
				{
					Counters.ZombieShoves++;
					break;
				}
				// A rig with no ragdoll - it gets the animated fall instead.
				reaction = Reaction.Prone;
				Knockdown(_zombie, EnumEntityStunType.Prone, ProneSeconds(_zombie));
				Counters.ZombieProne++;
				break;

			default:
				_zombie.emodel.avatarController.BeginStun(EnumEntityStunType.StumbleBreakThrough,
					EnumBodyPartHit.LeftUpperLeg, Utils.EnumHitDirection.None, _criticalHit: false, 1f);
				_zombie.SetStun(EnumEntityStunType.StumbleBreakThrough);
				_zombie.bodyDamage.StunDuration = Settings.ZombieStunSeconds;
				Counters.ZombieStumbles++;
				break;
			}

			LastReaction = Name(reaction) + " - " + _zombie.EntityName;
		}

		/// <summary>One draw from the weighted table. Only called when <see cref="Active"/>.</summary>
		private static Reaction Roll(GameRandom _rand)
		{
			float pick = _rand.RandomFloat * TotalWeight();

			pick -= Settings.WeightStumble;
			if (pick < 0f)
			{
				return Reaction.Stumble;
			}
			pick -= Settings.WeightKneel;
			if (pick < 0f)
			{
				return Reaction.Kneel;
			}
			pick -= Settings.WeightProne;
			if (pick < 0f)
			{
				return Reaction.Prone;
			}
			pick -= Settings.WeightRagdoll;
			if (pick < 0f)
			{
				return Reaction.Ragdoll;
			}
			return Reaction.Shove;
		}

		/// <summary>
		/// The animated knockdown, as <c>ProcessDamageResponse</c> plays it for a hit that was not
		/// heavy enough to ragdoll. The hit direction picks the fall: a random one of front, left
		/// and right, never back, since a hit from behind is the one fall that lands the zombie
		/// forward onto the player.
		/// </summary>
		private static void Knockdown(EntityAlive _zombie, EnumEntityStunType _stun, float _seconds)
		{
			Utils.EnumHitDirection direction;
			switch (_zombie.rand.RandomRange(0, 3))
			{
			case 0:
				direction = Utils.EnumHitDirection.Left;
				break;
			case 1:
				direction = Utils.EnumHitDirection.Right;
				break;
			default:
				direction = Utils.EnumHitDirection.Front;
				break;
			}

			_zombie.SetStun(_stun);
			_zombie.emodel.avatarController.BeginStun(_stun, EnumBodyPartHit.LeftUpperLeg, direction,
				_criticalHit: false, _zombie.rand.RandomFloat);
			_zombie.bodyDamage.StunDuration = _seconds;
		}

		/// <summary>
		/// The physics ragdoll with an impulse, as <c>EModelBase.DoRagdoll</c> plays it for a
		/// knockdown. The impulse is backward along the zombie's facing, leaned up to 45 degrees to
		/// one side and lifted 20 to 40 degrees, the same lift range vanilla rolls for a body hit.
		/// The game clamps the force to eight times the zombie's mass, so a big number cannot
		/// launch it. False when the rig has no ragdoll, so the caller can fall back.
		/// </summary>
		private static bool Shove(EntityAlive _zombie)
		{
			if (!_zombie.emodel.HasRagdoll() || Settings.ShoveForce <= 0f)
			{
				return false;
			}

			Vector3 back = -_zombie.GetForwardVector();
			back.y = 0f;
			if (back.sqrMagnitude < 0.001f)
			{
				back = Vector3.back;
			}
			back.Normalize();

			float lean = _zombie.rand.RandomRange(-45f, 45f);
			Vector3 direction = Quaternion.AngleAxis(lean, Vector3.up) * back;
			float lift = _zombie.rand.RandomRange(20f, 40f);
			Vector3 axis = Vector3.Cross(direction, Vector3.up);
			Vector3 force = Quaternion.AngleAxis(lift, axis) * (direction * Settings.ShoveForce);

			float seconds = ProneSeconds(_zombie);
			_zombie.emodel.DoRagdoll(seconds, EnumBodyPartHit.Torso, force, Vector3.zero, isRemote: false);
			_zombie.SetStun(EnumEntityStunType.Prone);
			_zombie.bodyDamage.StunDuration = seconds;
			return true;
		}

		/// <summary>
		/// The zombie's own knockdown range from entityclasses.xml, 0.5 to 1.8 seconds for the
		/// vanilla template, rolled the way the game rolls it. A class with no range set gets the
		/// stumble's seconds.
		/// </summary>
		private static float KneelSeconds(EntityAlive _zombie)
		{
			return StunSeconds(_zombie, EntityClass.list[_zombie.entityClass].KnockdownKneelStunDuration);
		}

		private static float ProneSeconds(EntityAlive _zombie)
		{
			return StunSeconds(_zombie, EntityClass.list[_zombie.entityClass].KnockdownProneStunDuration);
		}

		private static float StunSeconds(EntityAlive _zombie, Vector2 _range)
		{
			if (_range.y <= 0f)
			{
				return Settings.ZombieStunSeconds;
			}
			return _zombie.rand.RandomRange(_range.x, _range.y);
		}

		internal static string Name(Reaction _reaction)
		{
			switch (_reaction)
			{
			case Reaction.Kneel:
				return "kneel";
			case Reaction.Prone:
				return "prone";
			case Reaction.Ragdoll:
				return "ragdoll";
			case Reaction.Shove:
				return "shove";
			default:
				return "stumble";
			}
		}

		/// <summary>The weights as <c>sb zombie</c> takes them, in the same order.</summary>
		internal static string Weights()
		{
			return Format.Number(Settings.WeightStumble) + " " + Format.Number(Settings.WeightKneel)
				+ " " + Format.Number(Settings.WeightProne) + " " + Format.Number(Settings.WeightRagdoll)
				+ " " + Format.Number(Settings.WeightShove);
		}

		/// <summary>The <c>sb zombie</c> menu line: each reaction with its share of trips.</summary>
		internal static string Status()
		{
			float total = TotalWeight();
			if (total <= 0f)
			{
				return "off - every trigger still counts, nothing plays";
			}

			return Share("stumble", Settings.WeightStumble, total)
				+ Share(" kneel", Settings.WeightKneel, total)
				+ Share(" prone", Settings.WeightProne, total)
				+ Share(" ragdoll", Settings.WeightRagdoll, total)
				+ Share(" shove", Settings.WeightShove, total);
		}

		private static string Share(string _name, float _weight, float _total)
		{
			return _name + " " + Format.Percent(_weight / _total * 100f);
		}

		/// <summary>The <c>sb shove</c> menu line.</summary>
		internal static string ShoveStatus()
		{
			if (Settings.ShoveForce <= 0f)
			{
				return "no impulse - a shove plays as a prone fall";
			}
			return "impulse " + Format.Number(Settings.ShoveForce) + ", backward with a random lean";
		}
	}
}
