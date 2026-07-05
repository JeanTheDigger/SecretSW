# Map-Phase Handoff — Everything the Toolset Side Must Provide

The code side of the progression rework is complete (see `TESTING_PLAYBOOK.md` for
verification). Every system below is live in C# and **degrades gracefully** when its
module content is missing — nothing blocks server boot — but each needs the listed
toolset work to be fully playable. Exact tags, variables, and IDs are authoritative:
the code reads precisely these strings and numbers.

## 1. Waypoints and areas (required)

| What | Exact value | Notes |
|---|---|---|
| Perma-death limbo waypoint | tag `PERMADEATH_LIMBO` | Place in a new isolated, exit-less limbo area. Without it, perma-dead characters are held in place instead (error logged). |
| Ring-2 contested-lane space areas | area local int `SPACE_RING` = `2` | New space areas. Open ship PvP + module-loss economy + Phase-2 SP. |
| Ring-3 deep-space areas | area local int `SPACE_RING` = `3` | New space areas. Frame loss for flagged pilots. Gate transitions with a stakes-confirmation door/dialog if desired — code already announces on entry and protects unflagged pilots mechanically. |
| Existing 7 orbits | leave `SPACE_RING` unset | Unset = ring 1 (safe). **Sweep their spawn tables: capital rosters OUT of starter orbits.** |

Event-zone areas need nothing: `IS_EVENT_ZONE` / `EVENT_TYPE` / `EVENT_BRACKET` are set
at runtime. The automatic rotation currently targets these existing area tags —
`DantooineKinrathCaves`, `Ossuswastes`, `MonCaladungeon`, `DathomirCaveRuins`,
`KorribanValleyoftheDarkLords`, `DathGrottoCaverns` — edit the table in
`Feature/WorldEventRotation.cs` if the map plan prefers different sites.

## 2. Creature-scoped local variables (set on blueprints or spawns)

| Variable | Type | Meaning |
|---|---|---|
| `EVENT_SP_REWARD` | int | Endgame SP paid to each contributor when this creature dies inside an active event zone. Set on event bosses (1–3 typical). |
| `EVENT_UNLOCK_DROP_CHANCE` | int | % chance the boss also drops a holocron/datacron/schematic/flight recorder. Unset = 25. Negative = never. |
| `QUEST_NPC_GROUP_ID` | int | Quest kill-credit group. **67 = Trials Guardian**, **68 = Space Pirates**, **69 = Raider Ace**. |

Needed blueprints: a Trials Guardian ceremony creature (any suitable boss, group 67);
pirate ship NPCs (group 68) and a named raider ace (group 69) registered with the space
spawn system and seeded through ring-1/2 lanes for the patrol contracts.

## 3. NPCs, dialogs, terminals (standard quest snippets)

- **Trials trainers** (one per ceremony flavor if desired — Jedi/Sith/military are skins
  over ONE quest): dialog with `action-accept-quest` → quest id `trials_knighthood`.
  Until then, `/trialsbegin` (DM) starts it and `/grantknighthood` bypasses it.
- **Starport contract terminals**: dialog offering `patrol_pirate_cull`,
  `patrol_lane_sweep`, `patrol_raider_ace` (all repeatable, gold + Piloting XP).
- **Ship interior station placeables** (optional polish): gunnery + engineering consoles
  whose use scripts call the existing `/turret`, `/shields`, `/damagecontrol` code paths
  (`SpaceCrew.FireTurret/RechargeShields/DamageControl`). The commands already work.

## 4. Item blueprints (.uti)

- **Ship modules (required to be obtainable):** `pd_cluster_1/2/3` (Point-Defense
  Cluster I–III), `sam_battery_1` (SAM Battery) — create module items with these tags,
  then add them to loot/recipes as desired. Behavior is fully code-registered.
- **Unlock item art (cosmetic):** holocrons / combat datacrons / prototype schematics /
  flight recorders are fabricated at runtime on the `recipe_trnsabers` blueprint and
  renamed. Dedicated .uti art can replace `StanceUnlockBaseResref` in
  `Service/WorldEvent.cs` once made.
- **Gear track (future content):** disruptor weapons (event loot; gate behind a Ranged
  certification perk when itemized), cortosis-weave/beskar/crystal recipe lines — these
  are map/item-phase by nature and have no code yet by design.

## 5. HAK / 2da sweep (cosmetic but player-facing)

Reused feat.2da rows need renamed labels/descriptions/icons. Full mapping:

| Rows | Now used for |
|---|---|
| 1838, 1839 | Form V (Djem So), Form VI (Niman) toggles |
| 1887, 1891, 1892, 1893, 1897 | Forms I, II, III, IV, VII toggles |
| 1888, 1889, 1890, 1894, 1895 | Duelist, Juggernaut, Tempest, Teräs Käsi, Marksman toggles |
| 1913–1917 | Sarlacc Sweep, Duelist's End, Circle of Shelter, Hawk-Bat Swoop, Falling Avalanche |
| 1943–1947 | Balance, Vaapad, Riposte, Staggering Advance, Twin Cyclone |
| 1908, 1909 | Force Lock, Execution Shot |
| 1863, 1864 | Jump-Jet, Overclock |
| 1860, 1865, 1869, 1896 | Force Barrier, Force Breach, Affliction, Force Choke |
| 1415, 1416, 1417 | Carbonite Projector, Combat Jetpack, Orbital Strike |

Also sweep remaining beast NPCs/quests from the world (Beast Mastery is removed), and
the server-identity items from the original standup track (module name, `swlor.env`,
welcome message).

## 6. What needs NO map work

Daily caps, the Trials flag mechanics, brackets/rotation/drops, perma-death rules,
all 12 stance lines + signatures, implants + capstones, Second Wind, the Force kit,
Capital Command gating (deeds retagged in code), scale/speed/rings/crew/flight stances,
titles, Signature Weapon (/attune), and both migrations. All live at boot.

## 7. Deliberately deferred (code side, future arcs)

Command doctrines (need the capital-orders system), Mk I–III refits, hostile boarding
and salvage channels, sensor/stealth rules, the humanity cost (needs a verified NWNX
heal hook), droid construction Phase-2, Slicing/Stealth as new skills, mounts, swoop
racing, per-hull frame catalog re-authoring, and additional per-line signature actives
(design currently: one capstone per line).
