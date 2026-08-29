# Stumblr

A 7 Days To Die mod. Jumping a fence, railing or guardrail carries a tiny chance of catching a foot
on it — **1%** for the player, **2%** for a zombie.

A chest-high railing is currently not an obstacle. Everything clears it, every time, and a horde
pours over one in a tidy line. Now it occasionally doesn't.

**Undead Legacy is not required** and nothing of UL's is modified. Tested against
**UL 2.7.15 - 2.7.19**.

## Installing

Copy `dist/Stumblr/` (checked into this repo, so no build needed) into the game's `Mods/`:

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
  sb chance {p} {z} : 1% player / 2% zombie
  sb floor {p}      : Athletics cuts the player to 0.1%
  sb ground {n}     : only within 3 blocks of ground level
  sb hurt           : [ >on< | off ]
  sb shake          : [ >on< | off ]
  sb jolt           : [ >on< | off ]
  sb catch {pct}    : 50% caught before the jump, rest on landing
  sb zombie         : [ off | >stumble< | ragdoll ]
  sb blocks         : 11 patterns, 12 of 40 blocks seen so far
```

`sb blocks` prints the block-name substrings in full; `sb add` and `sb drop` edit them. A setter
called with no arguments prints its usage and current value; changes last until the game restarts.

Two more: `sb info` prints the same block with the patch state and counters added, and `sb reset`
zeroes those counters.

## What a trip does

**The player gets feedback and nothing else** — no damage, no stamina cost, no buff, and no change
to speed, control or aim — and always clears the obstacle. A trip happens on the landing; being left
on the wrong side of a fence with a horde behind you is the most lethal thing this mod could do.

Three channels, because they fail differently and any one alone is easy to miss while sprinting: a
grunt, a **lurch** as the horizon tips and rights itself, and a **jolt** to your held item and hands.
The lurch is roll only, so it tips the view without moving where you are aiming; the jolt runs on the
spring gun recoil uses and touches nothing about the camera, so it still reads if view effects are
turned down. The grunt is your own character's `SoundHurtSmall`, which unlike Door Slammer's and
Fletch Wounds' sounds is audible to zombies, exactly as taking a hit is.

Your odds fall from 1% to 0.1% as **Athletics** levels — a UL perk, falling back to vanilla's
`perkParkour` without it, and unscaled with neither. `sb info` shows which it found.

**A zombie goes down**, half the time caught before it jumps so it never leaves the ground, half the
time cleared over and taken out on the landing — `sb catch` sets the split. Either way it plays one
of the game's own reactions: `stumble` staggers it for a second, `ragdoll` knocks it over. Neither
deals damage, grants XP, sets a revenge target or triggers rage, and no skill scales it.

**Only near the ground** — within 3 blocks of terrain height, so a yard fence counts and a
fifth-floor catwalk railing does not. Zombies included, which also stops them tumbling off high
walkways.

Trippable blocks are matched by name substring, because the game ships no fence or railing tag. Both
the block's name and its **shape's** are checked — a picket fence built from the shape menu is a
`woodShapes` block whose fence-ness lives entirely in the `fencePicket` shape, and matching only the
block name misses every fence a player actually builds. Anything missed takes an `sb add`.

## Defaults

All tunable in `Settings.cs`, and all settable in-game:

| Setting | Default |
|---|---|
| player chance, Athletics 1 | 1% |
| player chance, Athletics cap | 0.1% |
| zombie chance | 2% |
| caught before the jump | 50% of trips |
| ground band | 3 blocks |
| trip grunt | on |
| camera lurch | on, roll force 0.8 |
| weapon jolt | on |
| zombie reaction | stumble |
| zombie stun | 1 s |

## Limitations

- **Zombie trips need single-player or a host.** The move helper runs server-side. Player trips are
  purely local feedback and work fine as a dedicated-server client.
- **Crawlers are never caught**, matching the game's own stumble path, which has nothing to play for
  a rig already on the floor.
- **UL's double jump** can raise the landing hook twice for one crossing; a half-second cooldown
  keeps that to one grunt.

## Building

Requires the .NET SDK; there are no NuGet dependencies. The mod builds in place inside the game
install, against the game's own assemblies.

```
dotnet build src/Stumblr/Stumblr.csproj -c Release
```

That restages `dist/Stumblr/`. Rebuild before committing so `dist/` matches `src/`.

## License

MIT — see `LICENSE`.
