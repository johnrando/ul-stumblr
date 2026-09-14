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
obtainable through UL's pick-up. Tested against **UL 2.7.15 - 2.7.32**.

## Installing

Download the zip from Releases and extract it into the game's `Mods/`. The mod folder is the root
of the archive, so it lands as:

```
Mods/Stumblr/
â”œâ”€â”€ ModInfo.xml
â””â”€â”€ Stumblr.dll
```

Load order does not matter.

## Console commands

`sb` prints the menu and changes nothing â€” `stumblr` is an alias. `sb on` and `sb off` are the
master switch. Every line names the command that changes it, so the menu is also the reference:

```
Stumblr is ON
  sb on|off           : [ >on< | off ] - take a zombie's legs out from under it
  sb chance {mult}    : perch trip chance = swing's dismember chance x2
  sb window {s}       : leg hit within 0.5s either side of landing
  sb narrow {w} {h}   : thinner than 0.4 wide, at least 0.5 tall
  sb ground {n}       : only within 3 blocks of ground level
  sb zombie {weights} : stumble 14% kneel 14% prone 29% ragdoll 14% shove 29%
  sb shove {force}    : impulse 60, backward with a random lean
  sb arrow {mult}     : arrow to a running zombie's leg trips at dismember chance x2
  sb tire {s} {pct}   : a placed tire trips 50% of zombies stepping in for 5s
  sb tires {k} {x} {n} : small x0.75 up to 1, single x1 up to 2, pile x1.5 up to 3, stack off
  sb door {pct}       : a door slammed on a zombie trips it 50% of the time
  sb flavor ds        : [ >on< | off ] - DoorSlammer: a slammed door can trip the zombie
  sb blocks           : 11 name overrides, 3 of 9 blocks seen were narrow
```

`sb blocks` prints the name overrides in full; `sb add` and `sb drop` edit them. A setter called
with no arguments prints its usage and current value. **Changes are saved** â€” see
[Settings file](#settings-file).

`sb probe` prints the block under your crosshair: its name, shape, rotation, every bounding box the
game measured for it, and whether the mod calls it narrow and why. This is how you find out whether
the fence in front of you counts, and how to tune `sb narrow` if it doesn't.

`sb info` prints the menu with the patch state and counters added; `sb reset` zeroes the counters.

## What a trip does

**A zombie goes down.** Each trip draws one of five reactions from a weighted table, and every one
is an animation the game already plays on every zombie rig:

- `stumble` — the break-through stagger. It lurches forward and recovers on its feet after a second.
- `ragdoll` — the same lurch carried through to the floor, ending when the body settles.
- `kneel` — the knockdown to one knee a heavy hit deals, to a random side or backward.
- `prone` — the knockdown flat, likewise to a random side or backward, for the zombie's own
  knockdown duration from entityclasses.xml (half a second to nearly two, for vanilla zombies).
- `shove` — a physics ragdoll with an impulse, backward along the zombie's facing with a random
  lean. It is the push a critical bashing hit gives; `sb shove {force}` sets how hard, and the game
  caps it at eight times the zombie's mass.

`sb zombie {stumble} {kneel} {prone} {ragdoll} {shove}` sets the weights, for every trigger at once;
a weight of 0 leaves a reaction out, and `sb zombie off` zeroes them all. The two break-through
reactions always fall *toward* the player, which on a flat floor is almost a favour to the zombie,
so the defaults lean on the other three. None deals damage, grants XP, sets a revenge target or
triggers rage. Being knocked off a fence is, of course, its own consequence.

**Crawlers, corpses and zombies already stunned are never tripped**, whatever the trigger, matching
the game's own stumble path, which has nothing to play for a rig already on the floor.

## The perch trip

**Only on a leg hit**, using the same body-part lookup the game uses for dismemberment, so what the
game would call a leg shot the mod does too. Melee and ranged both count.

**Only around the landing.** The landing hook times every zombie jump; a leg hit within half a
second after the feet touch counts, later ones do not. A hit within half a second *before* â€” a
player quick enough to swing while the zombie is still coming down â€” is held and judged when it
lands, on whatever it lands on, so the stumble plays the moment it touches the fence. `sb window 0`
drops the check, so any zombie standing on a narrow block can be tripped at any time.

**Only near the ground** â€” within 3 blocks of terrain height, so a yard fence counts and a
fifth-floor catwalk railing does not.

### The chance

The trip chance is the swing's own **dismember chance**, times 2. That number is filled in by the
game for every hit with every perk already applied, so nothing here has to know about perks:

| Weapon skill (UL) | Dismember | Trip chance |
|---|---|---|
| level 1 | 0.25% | 0.5% |
| level 50 | 12.5% | 25% |
| level 100 | 25% | 50% |

Under Undead Legacy that is the action skill for the weapon in hand â€” Clubs, Blades, Sledgehammers,
Spears, Axes, Brawler and the rest â€” each levelled from 1 to 100 by using it, plus whatever its
skill books add. Without UL, vanilla's own dismember perks feed the same number. `sb chance` sets
the multiplier; `sb info` shows the last roll.

### What counts as narrow

Nothing in the game's config says "this is a fence". The names only sometimes do, and the geometry
lives in the models. But when the game loads a shape it measures a bounding box from its mesh, per
rotation, and that is what the mod reads: a block is **narrow** when the thinner of its two
horizontal extents is at most **0.4** blocks and the box is at least **0.5** tall. A pole measures
0.2 wide, a fence or railing 0.1 to 0.3, a full block 1. Where a shape has several boxes, every box
that reaches its top has to be narrow â€” a railing on a full-width step is a step.

The zombie also has to actually be *on* it: its feet within 0.3 of the block's measured top, not
walking along the base with the fence in the same column.

Two name lists override the measurement. **Exclusions** win: a fence door measures narrow but is
opened, not perched on. **Inclusions** then accept things whose box is wide but whose footing is not,
like a hedge or a coil of barbed wire. Both lists match against the block's name and its shape's,
because a picket fence built from the shape menu is a `woodShapes` block whose fence-ness lives
entirely in the `fencePicket` shape.

## The arrow trip

An **arrow or bolt to the leg of a running zombie** trips it, no fence needed. The chance is the
shot's dismember chance times its own multiplier, 2 by default â€” under UL that is the Archery skill,
so the table above applies. `sb arrow` sets the multiplier; 0 switches the rule off.

A shot is told apart by what fired it: the game hands the hit the bow or crossbow as the damaging
item, so anything that launches counts, UL's bows and modded ones included. Bullets do not.

**Running** is two things at once: the zombie is one the game currently has running â€” feral,
night, blood moon, or the ZombieMove setting â€” and it is actually moving. A runner standing still
is not running. `sb info`'s `last arrow` line shows both for the most recent shot, so you can see
why one did or did not qualify.

A shot that qualifies but misses its roll falls through to the perch rule, so a runner on a fence
still gets that chance too.

## The tire

**Put a tire down and it is a trip hazard for 5 seconds.** A zombie that steps into it in that time
trips half the time. `sb tire {s} {pct}` sets both; 0 seconds switches tires off.

Only tires **a player places** arm â€” the game says who placed a block, and generated tires in a car
park say nobody. Picking a tire back up, or its destruction, disarms it. One roll per zombie per
tire, so a zombie standing in one is not re-rolled every tick. There is no requirement that the
zombie be moving: dropping a tire at a zombie's feet is the intended throw.

**Which tire matters.** Every tire is one of four kinds, each with its own multiplier on the chance
and its own cap on how many zombies one tire can trip before it is spent. `sb tires {kind} {x} {n}`
sets one kind; 0 for either number switches that kind off.

| Kind | What it is | Default |
|---|---|---|
| `small` | the donut spare: `decoCarTireSmallFlat` and UL's `_S` tires | ×0.75, one zombie |
| `single` | one full-size tire lying flat | ×1, two zombies |
| `pile` | a heap: `decoCarTirePile`, two blocks wide, both armed; UL's `Tires2` | ×1.5, three zombies |
| `stack` | a vertical column: `decoCarTireStack`, UL's two-block-tall `Tires4` | off |

The small flat tire is the only one **zombies walk through**, so their feet share its block and it
fires as they pass. The full-size tire and the pile are solid: a zombie walks around one, or steps
up onto it when it is in its path — and the block under its feet is then the tire, which is the
other way in. Put a solid one where the zombie has to go over it.

Any block whose name contains `tire` counts, which covers every vanilla and UL tire. **Under Undead
Legacy every tire can be picked up**: UL gives them its `ULM_Decor` class, which is pickable by
default. Vanilla without UL has no way to obtain one, so the rule is never reached there.

## The door

With **DoorSlammer** installed and the pair's flavor switch on, a zombie caught in a slammed door
is handed over and trips half the time. `sb door {pct}` sets the chance; 0 switches it off. The
slam's own damage still lands, and if FletchWounds is also installed its arrow proc runs first.

Neither mod references the other; each notices whether the other is there. The flavor switch is
a pair: `sb flavor ds` and `ds flavor sb` are the same switch, toggling either sets both sides,
and nothing else moves, so DoorSlammer's interaction with FletchWounds is unaffected. `sb flavor`
alone lists the switches; `sb flavor on|off` sets them all.

## Settings file

Every setting survives a restart. A change made with `sb` is written straight out to:

```
%APPDATA%/7DaysToDie/Stumblr/settings.txt
```

â€” the game's own user data folder, next to `Saves`, rather than `Mods/Stumblr/`, so updating the
mod does not take your settings with it. `sb info` prints the full path and whether the last read
or write worked.

It is plain `key = value` text, one line per setting, each naming the command that sets it:

```
enabled            = on       # sb on|off
chance             = 2        # sb chance {mult}
window             = 0.5      # sb window {s}
narrow.width       = 0.4      # sb narrow {w} {h}
narrow.height      = 0.5      # sb narrow {w} {h}
ground             = 3        # sb ground {n}
zombie             = 1 1 2 1 2 # sb zombie {stumble} {kneel} {prone} {ragdoll} {shove} - weights
shove.force        = 60       # sb shove {force}
stun               = 1        # stumble stun seconds (no command)
arrow              = 2        # sb arrow {mult}
tire.seconds       = 5        # sb tire {s} {pct}
tire.chance        = 50       # sb tire {s} {pct}
tire.small         = 0.75 1   # sb tires small {x} {n} - chance multiplier, zombies per tire
tire.single        = 1 2      # sb tires single {x} {n}
tire.pile          = 1.5 3    # sb tires pile {x} {n}
tire.stack         = 0 0      # sb tires stack {x} {n}
door.chance        = 50       # sb door {pct}
flavor.doorslammer = on       # sb flavor ds
include            = fence,railing,...  # sb add / sb drop
```

Edit it by hand with the game closed â€” it is rewritten whenever an `sb` command changes something.
A line that will not parse is logged and ignored rather than fatal, and deleting the file brings
back the defaults below (which live in `Settings.cs`).

## Defaults

| Setting | Default |
|---|---|
| perch chance multiplier | Ã—2 on the swing's dismember chance |
| window either side of landing | 0.5 s |
| narrow width / min height | 0.4 / 0.5 blocks |
| ground band | 3 blocks |
| zombie reaction weights | stumble 1, kneel 1, prone 2, ragdoll 1, shove 2 |
| shove impulse | 60 |
| zombie stun | 1 s (stumble only) |
| arrow chance multiplier | Ã—2 on the shot's dismember chance |
| tire armed for / trip chance | 5 s / 50% |
| tire kinds: chance multiplier / zombies per tire | small ×0.75 / 1, single ×1 / 2, pile ×1.5 / 3, stack off |
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

That restages `dist/Stumblr/`, ready to copy into `Mods/`. To also build the release archive:

```
dotnet build src/Stumblr/Stumblr.csproj -c Release -t:Package
```

That writes `release/Stumblr-v<version>-<date>.zip`, taking the version from `ModInfo.xml`.
Neither `dist/` nor `release/` is tracked - the zip is published as a Release instead.

## License

MIT â€” see `LICENSE`.
