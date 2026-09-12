# Stumblr

A 7 Days To Die mod. A zombie that has just scrambled onto a fence, a railing, a pole or any other
**narrow** block has not found its balance. Hit it in the **leg** as it lands, half a second either
side, and it can go down in one of the game's own stumble animations.

A zombie perched on your fence is currently as sure-footed as one on the ground. Now it isn't, for a
moment, if you're quick.

**Undead Legacy is not required** and nothing of UL's is modified, but the mod is built with UL in
mind: the trip chance grows with the weapon skills UL levels by use. Tested against **UL 2.7.15 -
2.7.30**.

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
  sb chance {mult}  : trip chance = swing's dismember chance x2
  sb window {s}     : leg hit within 0.5s either side of landing
  sb narrow {w} {h} : thinner than 0.4 wide, at least 0.5 tall
  sb ground {n}     : only within 3 blocks of ground level
  sb zombie         : [ off | >stumble< | ragdoll ]
  sb blocks         : 11 name overrides, 3 of 9 blocks seen were narrow
```

`sb blocks` prints the name overrides in full; `sb add` and `sb drop` edit them. A setter called
with no arguments prints its usage and current value; changes last until the game restarts.

`sb probe` prints the block under your crosshair: its name, shape, rotation, every bounding box the
game measured for it, and whether the mod calls it narrow and why. This is how you find out whether
the fence in front of you counts, and how to tune `sb narrow` if it doesn't.

`sb info` prints the menu with the patch state and counters added; `sb reset` zeroes the counters.

## What a trip does

**A zombie goes down.** It plays one of the game's own reactions: `stumble` staggers it for a
second, `ragdoll` knocks it over. Neither deals damage, grants XP, sets a revenge target or
triggers rage. Being knocked off a fence is, of course, its own consequence.

**Only on a leg hit**, using the same body-part lookup the game uses for dismemberment, so what the
game would call a leg shot the mod does too. Melee and ranged both count.

**Only around the landing.** The landing hook times every zombie jump; a leg hit within half a
second after the feet touch counts, later ones do not. A hit within half a second *before* — a
player quick enough to swing while the zombie is still coming down — is held and judged when it
lands, on whatever it lands on, so the stumble plays the moment it touches the fence. `sb window 0`
drops the check, so any zombie standing on a narrow block can be tripped at any time.

**Only near the ground** — within 3 blocks of terrain height, so a yard fence counts and a
fifth-floor catwalk railing does not.

## The chance

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

## What counts as narrow

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

## Defaults

All tunable in `Settings.cs`, and all settable in-game:

| Setting | Default |
|---|---|
| chance multiplier | ×2 on the swing's dismember chance |
| window either side of landing | 0.5 s |
| narrow width / min height | 0.4 / 0.5 blocks |
| ground band | 3 blocks |
| zombie reaction | stumble |
| zombie stun | 1 s |

## Limitations

- **Needs single-player or a host.** The stun runs server-side; on a dedicated-server client
  zombies are remote and never trip. `sb info` says so when no hit has reached the hook.
- **Crawlers are never tripped**, matching the game's own stumble path, which has nothing to play
  for a rig already on the floor.
- **A zombie that climbs rather than jumps** onto a block never opens the window. `sb window 0` if
  you would rather it did.

## Building

Requires the .NET SDK; there are no NuGet dependencies. The mod builds in place inside the game
install, against the game's own assemblies.

```
dotnet build src/Stumblr/Stumblr.csproj -c Release
```

That stages a ready-to-copy `dist/Stumblr/`, which is not tracked by git.

## License

MIT — see `LICENSE`.
