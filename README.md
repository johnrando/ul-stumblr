# Stumblr

A 7 Days To Die mod. Jumping a fence, railing or guardrail carries a tiny chance of catching a foot
on it — **1%** for the player, **2%** for a zombie.

A chest-high railing is currently not an obstacle. Everything clears it, every time, and a horde
pours over one in a tidy line. Now it occasionally doesn't.

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
  sb zombie         : [ off | >stumble< | ragdoll ]
  sb blocks         : 8 patterns, 12 of 40 blocks seen so far
```

`sb blocks` prints the block-name substrings in full; `sb add` and `sb drop` edit them. A setter
called with no arguments prints its usage and current value; changes last until the game restarts.

Two more: `sb info` prints the same block with the patch state and counters added, and `sb reset`
zeroes those counters.

## What a trip does

**To the player — nothing but feedback.** A grunt and a jolt of the view. No damage, no stamina
cost, no buff, and no change to speed, control or aim; the shake does not move where you are
pointing.

**And you always get over.** A trip happens on the landing, never as a failure to clear the
obstacle — being left on the wrong side of a fence is the most lethal thing this mod could do. A
stun, a slowdown or a stagger all mean the same thing with a horde behind you.

**To a zombie — it goes down.** The jump is cancelled, so it stays on your side of the fence, and it
plays one of the game's own reactions: `stumble` staggers it for a second, `ragdoll` knocks it over
properly. Neither deals damage, grants XP, sets a revenge target or triggers rage.

**Only near the ground.** Trips are limited to within 3 blocks of ground level, measured against the
terrain height rather than whatever is built on it — so a yard fence counts and a fifth-floor catwalk
railing does not. Zombies included, which also stops them tumbling off high walkways.

## Athletics

The player's chance falls from 1% at Athletics 1 to 0.1% at the perk's cap, in a straight line. A
tenfold reduction that never quite reaches zero.

Athletics is an **Undead Legacy** perk (`actionPerkAthletics`, capped at 100 and levelled by walking
and running). Vanilla has no such thing — its `skillAgilityAthletics` is an empty category header —
so without UL the mod falls back to `perkParkour`, and with neither the chance is not scaled at all.
`sb info` reports which one it found.

Zombies have no skill to read, so `sb chance`'s second number means exactly what it says.

## Sound

Nothing is bundled: a trip plays your own character's `SoundHurtSmall`, the small-pain grunt named
by its entity class — `player1painsm` for a male character, `player2painsm` for a female one. So the
voice always matches the character, and a custom character with its own pain sounds gets its own.

**Unlike Door Slammer and Fletch Wounds, this one is audible to zombies.** Those two play
unattributed, making no AI noise; a grunt can't, because it has to come from the player's own audio
source to sound like the player. It carries noise exactly as taking a hit does.

## Which blocks

The game ships no fence or railing tag, so blocks are matched by name substring: `fence`,
`railing`, `guardrail`, `handrail`, `barbedwire`, `barbwire`, `planthedge`, `plantshrub`, minus fence
doors, gates and the never-placed `*Helper` blocks.

That covers 169 of vanilla's 6299 blocks and all 28 of Undead Legacy's. A mod naming its fences some
other way needs an `sb add`.

## Defaults

All tunable in `Settings.cs`, and all settable in-game:

| Setting | Default |
|---|---|
| player chance, Athletics 1 | 1% |
| player chance, Athletics cap | 0.1% |
| zombie chance | 2% |
| ground band | 3 blocks |
| trip grunt | on |
| camera shake | on, vanilla's "Tiny" for 0.3 s |
| zombie reaction | stumble |
| zombie stun | 1 s |

## Undead Legacy

**Not required** — the mod works on a plain install, and modifies nothing of UL's. Tested against
**UL 2.7.18**. Two things about UL are worth knowing:

**Athletics comes from UL.** See above; without it the scaling falls back to a vanilla perk.

**The zombie hook targets `EntityMoveHelper.StartJump`, not `UpdateMoveHelper`.** UL replaces
`UpdateMoveHelper` wholesale with a prefix that never returns true, so a patch there would be dead
code under UL. It doesn't touch `StartJump`, which its replacement calls.

UL's double jump can raise the landing hook twice for one crossing; a half-second cooldown keeps that
to one grunt.

## Limitations

- **Zombie trips need single-player or a host.** The move helper runs server-side. Player trips are
  purely local feedback and work fine as a dedicated-server client.
- **Crawlers are never caught**, matching the game's own stumble path, which has nothing to play for
  a rig already on the floor.
- **New fences from other mods need adding** with `sb add` — see "Which blocks".

## Building

Requires the .NET SDK; there are no NuGet dependencies. The mod builds in place inside the game
install, against the game's own assemblies.

```
dotnet build src/Stumblr/Stumblr.csproj -c Release
```

That restages `dist/Stumblr/`. Rebuild before committing so `dist/` matches `src/`.

## License

MIT — see `LICENSE`.
