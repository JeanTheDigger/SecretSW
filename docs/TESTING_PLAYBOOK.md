# Two-Phase Progression Rework — End-to-End Testing Playbook

Every system on this branch has been verified by **compilation and logic review only**.
This playbook is the scripted pass that burns down the remaining risk on a live dev
server before any of it meets players. Run it top to bottom — later sections assume
state created by earlier ones (a flagged Knight, an unflagged Padawan, a fitted ship).

## 0. Environment setup

1. Build: `dotnet build SWLOR.Game.Server.sln` (must be 0 errors).
2. Start the dev stack from `debugserver/` (`docker-compose up --build`), with HAKs/TLK
   in place per the README. Set `SWLOR_ENVIRONMENT=test` on the game container —
   several DM commands are open to all players on a test environment, and none of this
   playbook works without DM access otherwise.
3. **Migrations fire on first boot.** Watch the logs for:
   - Player migration 13 (shields/beasts/Strong Style refunds) on first login of any
     pre-existing character.
   - Server migration **18 (Saga durability conversion)**: every persisted `PlayerShip`
     re-derives its stats. Spot-check one in Redis: `Status.MaxShield` should be
     pool/5 (clamped 5–60), `Status.DamageThreshold` set (15/30/50), `ConditionStep` 0.
4. Create four test characters: **PADAWAN-A** (Force-Sensitive, fresh), **TROOPER-B**
   (Standard, fresh), **KNIGHT-C** (FS, to be pushed to 350+), **VET-D** (Standard, 350+).
   Use `/givexp`-style DM tooling / `SetXPBonus` to accelerate; a DM can also target
   skills directly through the rebuild tooling.

Record results per line: ☐ pass ☐ fail + note.

## 1. Phase-1 skill economy (Stage 1)

- ☐ Grind combat XP on PADAWAN-A: rank-ups stop after **5 ranks in a day, across ALL
  skills combined** (not per skill).
- ☐ Keep earning XP past the cap: no rank-up, **no XP loss** — check the skill's XP bar
  keeps filling (banked). Advance server date past midnight UTC (or wait): banked XP
  converts on the next gain.
- ☐ No cap-contributing skill will pass **rank 50** while total SP < 350.
- ☐ Languages (non-cap skills) ignore all of the above.
- ☐ 1 AP lands per 10 total SP; at the character sheet, a single attribute accepts up
  to **12** upgrades.
- ☐ At 350 total SP, activity XP grants **zero** further ranks (no decay fires either —
  gaining a skill never drops another).

## 2. The Trials gate (Stages 5b/5c)

- ☐ Push KNIGHT-C to exactly 350 SP. Confirm: per-skill cap still 50, event SP still
  refused (see §3), perma-death does NOT apply (see §4).
- ☐ `/trialsbegin` on KNIGHT-C → quest accepted (journal: "The Trials"). On a character
  below 350: refused with prerequisite feedback.
- ☐ Spawn any creature, `/setlocalvariable` its `QUEST_NPC_GROUP_ID` = **67**
  (Trials Guardian). KNIGHT-C kills it → quest completes, per-class ceremony line +
  server-wide announcement, `HasCompletedTrials` set.
- ☐ Post-flag: per-skill ceiling opens to 100 (skill window), event SP flows, AP keeps
  pace to 70 total at 700 SP.
- ☐ `/grantknighthood` on VET-D (350+) works as the admin fallback; on a sub-350 target
  it refuses with the SP count. `/revokeknighthood` reverses cleanly.

## 3. World events, brackets, drops (Stages 2/5c)

- ☐ `/eventopen pve 10 knight` in a test area: server-wide broadcast names the bracket.
  `/eventlist` shows area/type/bracket/close time. `/eventclose` ends it.
- ☐ **Bracket enforcement:** PADAWAN-A (sub-350) entering any event area → bounced to
  home point with a message. KNIGHT-C (350–499, flagged) enters Knight events, is
  bounced from a `master` event. An unflagged 350+ character enters ONLY `open` events.
  Open the event on an occupied zone: out-of-bracket occupants are swept out.
- ☐ **PvE SP:** spawn a creature, set `EVENT_SP_REWARD` = 2. Kill it with KNIGHT-C
  inside the event: +2 ranks in the skill actually used, and roughly 1 in 4 kills
  fabricates a class-matched unlock item to a contributor (`EVENT_UNLOCK_DROP_CHANCE`
  local overrides; negative disables). Outside an event zone the same creature pays
  nothing.
- ☐ **PvP SP:** open `pvp`; KNIGHT-C kills VET-D inside → +1 endgame SP to the killer's
  weapon skill; killing the same victim again within an hour pays nothing ("too
  recently"). On `/eventclose`, the top killer receives one unlock item with a
  server-wide victor line.
- ☐ **Rotation:** within ~3 hours of boot an automatic 45-minute event opens at one of
  the rotation sites (Kinrath Caves / Ossus Wastes / Mon Cala caves / Dathomir ruins /
  Valley of the Dark Lords / Grotto Caverns), alternating Knight/Master. (Log-watch is
  acceptable; a temporary interval reduction is fine for the test build.)

## 4. Perma-death and limbo (Stages 3/5b)

- ☐ KNIGHT-C (flagged) dies inside an active event zone → no respawn popup; flagged
  perma-dead, teleported to `PERMADEATH_LIMBO`, immobilized; **relog lands back in
  limbo**; the `.bic` still exists on disk.
- ☐ `/permarestore` clears the flag, removes the hold, returns them to their home point.
- ☐ An **unflagged** 350+ character dying in an `open` event takes an ORDINARY death
  (respawn popup, XP debt) — the Order protects learners.
- ☐ Non-event death anywhere: XP-debt only, unchanged.

## 5. Stances — forms, doctrines, Phase-2 arcs, signatures (Stages 5a–5d)

- ☐ KNIGHT-C buys Form I–III (gates 15/30/45 on One-/Two-Handed): toggles appear; one
  active at a time; switching within 6s refused (shared FormSwitch recast); activating
  with no saber refused; unequipping the saber mid-stance drops it with a message.
- ☐ VET-D buys Duelist/Teräs Käsi/Marksman doctrines: same rules per weapon family.
  **Teräs Käsi specifically:** +saves effect appears on activation (character sheet),
  is stripped on deactivation AND on swapping to a rifle; empty-handed activation works.
- ☐ Combat math spot-checks: Marksman +5 accuracy visible in ranged combat logs; Djem So
  re-maps saber damage to Might; Soresu −2 damage / + defense; stance crit contributes
  (and total crit bonus never exceeds the 75 ceiling with Improved Critical + Precision
  Aim stacked).
- ☐ **Phase-2 levels:** L4 purchase refused pre-unlock ("Perk must be unlocked"). Use a
  dropped **Holocron** (FS) → that form's L4 purchasable at rank 60+. A Standard
  character using a holocron is refused (and vice versa for datacrons). Using a second
  copy of the same unlock is refused.
- ☐ **Signatures at L6:** each grants its named active; firing it OUTSIDE its stance is
  refused; any two capstones share one recast (swap stances and try — blocked).
  Vaapad costs ~10% of current HP. Falling Avalanche knocks down once, then the target
  is briefly immune. Force Lock blocks the victim's Force abilities for ~4s
  ("disrupted!") and cannot be chained. Execution Shot doubles below 30% HP but not
  against a Tranquilizer-slept target.

## 6. Cybernetic implants (Stage 5e)

- ☐ VET-D buys two implant lines at TotalSP gates (75/175/275): passives apply
  immediately (evasion/accuracy in combat logs; Might/saves/speed as visible effects).
  A THIRD line is refused ("supports at most 2") until the Trials flag, then allowed.
- ☐ FS characters cannot buy any line; a **Prototype Schematic** used by an FS character
  is refused with the canon message.
- ☐ Refund an implant line: effects strip instantly; the slot frees.
- ☐ Relog: passives re-apply (cache rebuild on enter).
- ☐ Cardio line: stamina regen tick increases by the listed amount.

## 7. Space — durability model (Stage 6a)

Fit two ships (attacker with Combat Laser + Missile Launcher; defender anything).

- ☐ Laser hit **below** the target's shield rating: zero damage, "deflect" visual,
  SR unchanged. Laser hit **at/above** SR: SR drops by 5, remainder hits hull.
- ☐ Missile: always drops SR by 5, only half of SR subtracts from damage.
- ☐ **No passive shield regen** — SR sits where it fell until an action restores it.
- ☐ Threshold: a raw missile hit ≥ the frame's threshold slides the condition track
  (nearby message names the step); each step visibly costs the victim accuracy; a hit
  ≥ 2× threshold slides two steps.
- ☐ At step 5 (Disabled): a PLAYER ship cannot fire weapons ("disabled!") but CAN use
  hull/shield repairers; a hull repair climbs one step and re-arms at step <5. An NPC
  ship at step 5 breaks apart (v1 dial — not a bug).
- ☐ Ion Cannon vs a shieldless target: condition slides on small damage numbers
  (2× pressure), movement/AGI debuffs apply; vs a capital: never slides.
- ☐ Death is hull-zero alone (a ship with SR intact still dies at hull 0).

## 8. Space — rings (Stage 6b)

Set `SPACE_RING` on test space areas via `/setlocalvariable` (1/2/3; unset = 1).

- ☐ Entering ring 2/3 prints the stakes warning; ring 1 prints nothing.
- ☐ **Ring 1 death:** modules still fitted afterward, interior passengers alive
  ("towed"), ship at last dock, hull 1. PC-on-PC weapon fire in ring 1 is refused
  ("defense grid"); repair modules still work on friendlies.
- ☐ **Ring 2 death:** modules drop at the kill site (~65%), ALL modules gone from the
  ship, passengers killed, ship at dock.
- ☐ **Ring 3 death (flagged pilot):** all modules drop, passengers moved to home points
  with pod messaging (NOT killed), and after ~6s the ship record AND property are gone
  (ship list, Redis). **Unflagged pilot in ring 3:** ring-2 outcome, ship survives.
- ☐ Ring 2/3 kills pay endgame SP: PC kill → killer Piloting +1/+2 (hourly same-victim
  window); NPC ship kill → each contributor +1/+2 in their tagged skill. Ring-1 kills
  pay zero endgame SP.

## 9. Space — scale, speed, crew, stances (Stages 6c–6e)

- ☐ **Scale:** a Turbolaser vs a fighter hits ~20% (engine floor); the same battery
  vs a capital hits normally. A Combat Laser vs a capital gains ~+10 points of hit.
- ☐ **Speed classes:** transports visibly slower than fighters; capitals slowest
  (0.85/0.6 factors), and speed resets to normal on exiting space mode.
- ☐ **Crew seats** (second player inside the flying ship's interior):
  `/turret` — refused without a laser battery/quad laser fitted; with one, fires at
  the pilot's target on a 6s cycle, damage scaling with the gunner's Ranged/Perception.
  `/shields` — +5 (+1 per 10 Engineering) SR, 12s cycle, pilot notified.
  `/damagecontrol` — one condition step, 18s cycle. All three refused when the ship
  isn't underway or the user isn't aboard a starship.
- ☐ Crew XP routing: after an NPC ship kill in ring 2+, the gunner's SP lands in
  **Ranged**, the engineer's in **Engineering**, the pilot's in **Piloting**.
- ☐ **Flight stances:** `/flightmode attack` refused without the perk; with it (1 SP,
  Piloting 5), accuracy/evasion shift ±10 in combat logs; stance clears on docking.

## 10. Known v1 dials and deferred items (do NOT file as bugs)

- Disabled NPC ships break apart (boarding/salvage window: future ring-economy arc).
- Holocrons/datacrons/schematics render on the recipe-disk model (HAK art pending);
  feat rows carry legacy 2da names until the HAK rebuild renames them.
- Trials trainer-NPC dialogs, ring-2/3 space areas, station terminal placeables, the
  limbo area itself, and event-zone entry doors are module content (the sweep list).
- Implant humanity cost (−healing per line) deferred pending a verified NWNX heal hook.
- Capital command (Leadership), flight/command doctrine perk lines, refits, and patrol
  contracts are future arcs.
- Condition-track speed penalties (ship movement) not yet wired; accuracy penalties are.

**Sign-off:** all sections pass on a clean dev stack, twice (once fresh, once after a
server restart mid-state to catch persistence bugs: stances, implants, condition steps,
event state, and daily-cap counters should all survive or cleanly reset as designed).
