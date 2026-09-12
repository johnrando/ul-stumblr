# Stumblr

A 7 Days To Die mod. Four ways to take a zombie off its feet, each played in one of the game's
own stumble animations:

- **A leg hit as it lands on a fence.** A zombie that has just scrambled onto a fence, a railing, a
  pole or any other **narrow** block has not found its balance. Hit it in the leg as it lands, half
  a second either side, and it can go down.
- **An arrow or bolt to the leg while it runs.** A runner takes a shaft in the leg mid-stride and
  can go down. Walkers are unmoved.
- **A tire in its path.** Put a tire down and for a few seconds it is a trip hazard. Under Undead
  Legacy every tire in the world can be picked up, so carry one.
- **A door in its face**, with [DoorSlammer](../ul-doorslammer) installed: a zombie caught in a
  slammed door can go down too.

**Undead Legacy is not required** and nothing of UL's is modified, but the mod is built with UL in
mind: the leg-hit chances grow with the weapon skills UL levels by use, and tires are only
obtainable through UL's pick-up. Tested against **UL 2.7.15 - 2.7.31**.

## Installing

Build (below), then copy the staged `dist/Stumblr/` into the game's `Mods/`:

```
Mods/Stumblr/
├── ModInfo.xml
└── Stumblr.dll
```

Load order does not matter.

## Console commands

`sb` toggles the mod and prints the menu — `stumblr` is an alias. Every line names the command that
changes it, so the menu is also the reference:

```
Stumblr is now ON
  sb chance {mult}  : perch trip chance = swing's dismember chance x2
  sb window {s}     : leg hit within 0.5s either side of landing
  sb narrow {w} {h} : thinner than 0.4 wide, at least 0.5 tall
  sb ground {n}     : only within 3 blocks of ground level
  sb zombie         : [ off | >stumble< | ragdoll ]
  sb arrow {mult}   : arrow to a running zombie's leg trips at dismember chance x2
  sb tire {s} {pct} : a placed tire trips 50% of zombies stepping in for 5s
  sb door {pct}     : a door slammed on a zombie trips it 50% of the time
  sb flavor         : [ >on< | off ] - enhanced mod interaction with DoorSlammer
  sb blocks         : 11 name overrides, 3 of 9 blocks seen were narrow
```

`sb blocks` prints the name overrides in full; `sb add` and `sb drop` edit them. A setter called
with no arguments prints its usage and current value. **Changes are saved** — see
[Settings file](#settings-file).

`sb probe` prints the block under your crosshair: its name, shape, rotation, every bounding box the
game measured for it, and whether the mod calls it narrow and why. This is how you find out whether
the fence in front of you counts, and how to tune `sb narrow` if it doesn't.

`sb info` prints the menu with the patch state and counters added; `sb reset` zeroes the counters.

## What a trip does

**A zombie goes down.** It plays one of the game's own reactions: `stumble` staggers it for a
second, `ragdoll` knocks it over. `sb zombie` picks which, for every trigger at once. Neither deals
damage, grants XP, sets a revenge target or triggers rage. Being knocked off a fence is, of course,
its own consequence.

**Crawlers, corpses and zombies already stunned are never tripped**, whatever the trigger, matching
the game's own stumble path, which has nothing to play for a rig already on the floor.

## The perch trip

**Only on a leg hit**, using the same body-part lookup the game uses for dismemberment, so what the
game would call a leg shot the mod does too. Melee and ranged both count.

**Only around the landing.** The landing hook times every zombie jump; a leg hit within half a
second after the feet touch counts, later ones do not. A hit within half a second *before* — a
player quick enough to swing while the zombie is still coming down — is held and judged when it
lands, on whatever it lands on, so the stumble plays the moment it touches the fence. `sb window 0`
drops the check, so any zombie standing on a narrow block can be tripped at any time.

**Only near the ground** — within 3 blocks of terrain height, so a yard fence counts and a
fifth-floor catwalk railing does not.

### The chance

The trip chance is the swing's own **dismember chance**, times 2. That number is filled in by the
game for every hit with every perk already applied, so nothing here has to know about perks:

| Weapon skill (UL) | Dismember | Trip chance |
|---|---|---|
| level 1 | 0.25% | 0.5% |
| level 50 | 12.5% | 25% |
| level 100 | 25% | 50% |

Under Undead Legacy that is the action skill for the weapon in hand — Clubs, Blades, Sledgehammers,
Spears, Axes, Brawler and the rest — each levelled from 1 to 100 by using it, plus whatever its
skill books add. Without UL, vanilla's own dismember perks feed the same number. `sb chance` sets
the multiplier; `sb info` shows the last roll.

### What counts as narrow

Nothing in the game's config says "this is a fence". The names only sometimes do, and the geometry
lives in the models. But when the game loads a shape it measures a bounding box from its mesh, per
rotation, and that is what the mod reads: a block is **narrow** when the thinner of its two
horizontal extents is at most **0.4** blocks and the box is at least **0.5** tall. A pole measures
0.2 wide, a fence or railing 0.1 to 0.3, a full block 1. Where a shape has several boxes, every box
that reaches its top has to be narrow — a railing on a full-width step is a step.

The zombie also has to actually be *on* it: its feet within 0.3 of the block's measured top, not
walking along the base with the fence in the same column.

Two name lists override the measurement. **Exclusions** win: a fence door measures narrow but is
opened, not perched on. **Inclusions** then accept things whose box is wide but whose footing is not,
like a hedge or a coil of barbed wire. Both lists match against the block's name and its shape's,
because a picket fence built from the shape menu is a `woodShapes` block whose fence-ness lives
entirely in the `fencePicket` shape.

## The arrow trip

An **arrow or bolt to the leg of a running zombie** trips it, no fence needed. The chance is the
shot's dismember chance times its own multiplier, 2 by default — under UL that is the Archery skill,
so the table above applies. `sb arrow` sets the multiplier; 0 switches the rule off.

A shot is told apart by what fired it: the game hands the hit the bow or crossbow as the damaging
item, so anything that launches counts, UL's bows and modded ones included. Bullets do not.

**Running** is two things at once: the zombie is one the game currently has running — feral,
night, blood moon, or the ZombieMove setting — and it is actually moving. A runner standing still
is not running. `sb info`'s `last arrow` line shows both for the most recent shot, so you can see
why one did or did not qualify.

A shot that qualifies but misses its roll falls through to the perch rule, so a runner on a fence
still gets that chance too.

## The tire

**Put a tire down and it is a trip hazard for 5 seconds.** A zombie that steps into it in that time
trips half the time. `sb tire {s} {pct}` sets both; 0 seconds switches tires off.

Only tires **a player places** arm — the game says who placed a block, and generated tires in a car
park say nobody. Picking a tire back up, or its destruction, disarms it. One roll per zombie per
tire, so a zombie standing in one is not re-rolled every tick. There is no requirement that the
zombie be moving: dropping a tire at a zombie's feet is the intended throw.

The tire that works is the **small flat one** (`decoCarTireSmallFlat` and its ground-aligned twin):
zombies walk through it, so their feet share its block. The larger tires and piles are solid and
get walked around; they arm harmlessly.

Any block whose name contains `tire` counts, which covers every vanilla and UL tire. **Under Undead
Legacy every tire can be picked up**: UL gives them its `ULM_Decor` class, which is pickable by
default. Vanilla without UL has no way to obtain one, so the rule is never reached there.

## The door

With **DoorSlammer** installed and `sb flavor` on in both mods, a zombie caught in a slammed door
is handed over and trips half the time. `sb door {pct}` sets the chance; 0 switches it off. The
slam's own damage still lands, and if FletchWounds is also installed its arrow proc runs first.

Neither mod references the other; each notices whether the other is there. DoorSlammer is the hub
for the shared flavor switch: **toggling it in any of the linked mods moves all of them**.

## Settings file

Every setting survives a restart. A change made with `sb` is written straight out to:

```
%APPDATA%/7DaysToDie/Stumblr/settings.txt
```

— the game's own user data folder, next to `Saves`, rather than `Mods/Stumblr/`, so updating the
mod does not take your settings with it. `sb info` prints the full path and whether the last read
or write worked.

It is plain `key = value` text, one line per setting, each naming the command that sets it:

```
enabled       = on       # sb
chance        = 2        # sb chance {mult}
window        = 0.5      # sb window {s}
narrow.width  = 0.4      # sb narrow {w} {h}
narrow.height = 0.5      # sb narrow {w} {h}
ground        = 3        # sb ground {n}
zombie        = stumble  # sb zombie - off, stumble or ragdoll
stun          = 1        # stumble stun seconds (no command)
arrow         = 2        # sb arrow {mult}
tire.seconds  = 5        # sb tire {s} {pct}
tire.chance   = 50       # sb tire {s} {pct}
door.chance   = 50       # sb door {pct}
flavor        = on       # sb flavor
include       = fence,railing,...  # sb add / sb drop
```

Edit it by hand with the game closed — it is rewritten whenever an `sb` command changes something.
A line that will not parse is logged and ignored rather than fatal, and deleting the file brings
back the defaults below (which live in `Settings.cs`).

## Defaults

| Setting | Default |
|---|---|
| perch chance multiplier | ×2 on the swing's dismember chance |
| window either side of landing | 0.5 s |
| narrow width / min height | 0.4 / 0.5 blocks |
| ground band | 3 blocks |
| zombie reaction | stumble |
| zombie stun | 1 s |
| arrow chance multiplier | ×2 on the shot's dismember chance |
| tire armed for / trip chance | 5 s / 50% |
| door trip chance | 50% |
| enhanced mod interaction | on |

## Limitations

- **Needs single-player or a host.** Every trip runs server-side; on a dedicated-server client
  zombies are remote and never trip. `sb info` says so when no hit has reached the hook.
- **Crawlers are never tripped**, matching the game's own stumble path, which has nothing to play
  for a rig already on the floor.
- **A zombie that climbs rather than jumps** onto a block never opens the perch window. `sb window
  0` if you would rather it did.
- **Tires need Undead Legacy** or another mod that makes them obtainable.
- **The door trip needs DoorSlammer** 0.0.0.4 or later, which is the first build that looks for
  Stumblr.

## Building

Requires the .NET SDK; there are no NuGet dependencies. The mod builds in place inside the game
install, against the game's own assemblies.

```
dotnet build src/Stumblr/Stumblr.csproj -c Release
```

That stages a ready-to-copy `dist/Stumblr/`, which is not tracked by git.

## License

MIT — see `LICENSE`.
