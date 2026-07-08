# Crafting & Gathering Removal — Handoff

This document records the removal of crafting, gathering, and food from the
game, and the remaining work that must be done in a build-equipped session or
with the module toolchain. It is the work order for finishing the effort.

## Design decisions (as implemented)

- **Removed:** hand-crafting of weapons/armor (Smithery), structures/furniture
  (Fabrication), cooking (Agriculture); the whole gathering loop
  (harvesting/refining/scavenging) and asteroid/space mining; the Research
  system; and the food-buff system. Gear and materials are now bought, earned
  through factions/DM events, or found in dungeons.
- **Kept:** Engineering (starships, droids, modules) and the enhancement /
  blueprint layer that rides on it.
- **New:** a **Mod Kit** so looted/bought weapons and armor can still take
  enhancements, decoupled from crafting.
- **Save-data safety:** every retired skill, perk, perk-category, feat,
  ship-module-type, and recipe **enum value is retained** (set inactive in
  place, BeastMastery-style). Nothing is renumbered, so existing player
  records stay valid.

## What shipped (branch `claude/last-work-summary-2bgga2`)

| Commit | Increment | Summary |
|---|---|---|
| 1a | ground gathering | harvest/refine/scavenge, Refinery, gathering perks + `Gathering` skill retired |
| 1b | space mining | asteroids, mining ship modules + recipes, mining piloting perks |
| 2a | recipe trees | Smithery/Fabrication/Cooking recipes deleted; 3 skills retired |
| — | char-sheet fix | fixed a crash 2a would have caused (unconditional Control/Craftsmanship calls) |
| 2b | food | food-buff system disabled at its source |
| 2c | perks + research | crafting perk trees deleted; Research shut off at its entry point |
| 3 | materials | 17 space-only Engineering materials added to tiered ground loot |
| 4 | mod station | `ModKitItemDefinition` (item tag `mod_kit`) |

**Not build-verified:** the working environment had no .NET SDK (install is
blocked by egress policy), so every change is grep/reference-verified only.
**Build `SWLOR.Game.Server.sln` and confirm before deploying.** Each commit is
self-contained for easy bis/revert.

---

## Remaining work

### A. Build-verify first
Compile the branch. Expected soft spots (grep-verified, but a compiler sees
more): unused `using`s in `Service/Craft.cs` after the research entry point was
removed (warnings only — no `TreatWarningsAsErrors`), and the new
`ModKitItemDefinition.cs`.

### B. Known legacy-data crash to fix (needs compiler + testing)
`Craft.GetRecipe(RecipeType)` does an unguarded `_recipes[recipeType]` lookup
that throws `KeyNotFoundException` for a now-undefined recipe. Normal play is
safe (retired skills are not selectable in the recipe UI, which enumerates the
recipe cache). **But** an existing player who **activates a stale Smithery /
Fabrication / Cooking blueprint item** opens the craft window on a deleted
recipe and crashes. Fix one of:
- Guard the blueprint/craft entry point with `Craft.RecipeExists(recipe)` and
  refuse gracefully; or
- Run a one-time player migration that strips `UnlockedRecipes` entries and
  destroys blueprint items whose `RecipeType` no longer resolves.

### C. Inert dead code to excise (defer to a build-verified pass — this is the
riskiest surgery without a compiler)
All of this is currently unreachable but still compiled:
- **Research system internals:** `Feature/GuiDefinition/ResearchDefinition.cs`,
  `ViewModel/ResearchViewModel.cs`, `Payload/ResearchPayload.cs`;
  `Entity/ResearchJob.cs`; the research helpers/fields still in `Craft.cs`
  (`MaxResearchLevel`, `_researchableRecipes*`, `IsResearchableRecipe`,
  `GetAllResearchableRecipes*`, `CanPlayerResearchRecipe`,
  `CalculateBlueprintResearchCreditCost/Seconds`, `OnRemoveProperty`); and the
  `RecipesUIMode.Research` branches interleaved through `RecipesViewModel.cs`.
  **Keep `CalculateResearchCost`** — the surviving Engineering blueprint-craft
  cost path (`CalculateBlueprintCraftCreditCost`) still calls it.
- **Food:** `FoodEffectData.cs` (still referenced by ~15 null-guarded reads in
  `Stat.cs`, `Recast.cs`, `Skill.cs`, `NaturalRegeneration.cs`,
  `RestStatusEffectDefinition.cs`, `CharacterSheetViewModel.cs`); remove those
  reads and the class together. Retire `StatusEffectType.Food`,
  `EffectIconType.Food`, `ItemPropertyType.FoodBonus`.
- **CraftViewModel** dead `switch` cases mapping `recipe.Skill` for
  Smithery/Fabrication/Cooking to their quality perks (lines ~443–485).

### D. Module-data / toolchain tasks (cannot be done in the C# assembly)
- **`iprp_skill.2da`:** mirror the retired-skill changes (Smithery, Fabrication,
  Gathering, Agriculture) per the note at the top of `SkillType.cs`.
- **Mod Kit item:** create an activatable `.uti` blueprint with tag `mod_kit`
  and make it purchasable/lootable. (Behavior is already in
  `ModKitItemDefinition.cs`.)
- **Enhancement (mod) items:** ensure weapon/armor enhancement items are
  available from vendors/faction/loot now that Smithery no longer makes them.
- **Material vendors:** optionally stock refined ores / metals / etc. on gold
  vendors (`.utm`) and the Engineering guild stores (`gp_eng_*.utm`). The C#
  guild rank-gating and `OpenStore` path already work; only the stock lists
  need editing. (Ground loot already covers all 44 materials — see Increment 3.)
- **Gathering quests:** the CZ-220 scavenging/ore/refinery quests and the
  Dantooine herb quest were intentionally left in place (they become completable
  once materials are vendor/loot-sourced). If undesired, remove/repoint them and
  their offering NPC dialogs (module data).

### E. Fishing — removed
Fishing was removed (it was built on the now-inactive `Agriculture` skill and
fed cooking): deleted `Service/Fishing.cs`, `Service/FishingService/`,
`Feature/FishingLocationDefinition/`, `FishingSpawnPointDefinition.cs`, and
`FishingRodItemDefinition.cs`; removed the fishing-rod/bait branch from
`PlayerMarket.cs` and retired `MarketCategoryType.Fishing`. Retained enum values
(save-data): `PerkType.FishingRods`, `ActivityStatusType.Fishing`,
`MarketCategoryType.Fishing`. Leftover module-data cleanup: the fishing quests
(`MonCalaQuestDefinition.FishingGuildQuests`, the "Catch" tasks in
`AgricultureGuildQuestDefinition`) and their guild NPC dialogs, plus the fishing
rod/bait `.uti` items — see section D.

### F. Player skill points
Per the BeastMastery precedent, ranks spent in the four retired skills are left
**inert (no refund)**. If a refund/reallocation is desired, add a migration.
