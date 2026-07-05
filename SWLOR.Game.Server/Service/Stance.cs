using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StanceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The combat stance system behind the seven lightsaber forms (Force-Sensitive) and the
    /// Standard combat doctrines. A player knows any number of stances (perks) but has at most
    /// ONE active — forms and doctrines share the slot; a character fights in one tradition at
    /// a time. The active stance's stat package is resolved and cached at toggle time; combat
    /// hot paths read the cache and treat the stance as inactive whenever the wielded weapon
    /// leaves the stance's weapon family. Design rule: only a form may re-map a weapon's stats.
    /// </summary>
    public static class Stance
    {
        private const string SavingThrowEffectTag = "STANCE_SAVING_THROWS";

        private static readonly Dictionary<PerkType, Dictionary<int, StanceDetail>> _stances = new();
        private static readonly Dictionary<PerkType, HashSet<BaseItem>> _stanceWeapons = new();
        private static readonly Dictionary<PerkType, string> _weaponRequirements = new();
        private static readonly Dictionary<uint, PerkType> _activeTypes = new();
        private static readonly Dictionary<uint, StanceDetail> _activeStances = new();

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterStances()
        {
            var sabers = Weapons("A lightsaber or saberstaff must be equipped to take a form.",
                Item.LightsaberBaseItemTypes, Item.SaberstaffBaseItemTypes);

            // Form I: Shii-Cho - the steady training form.
            Register(PerkType.FormShiiCho, sabers,
                new StanceDetail { Name = "Form I: Shii-Cho", AccuracyMod = 5 },
                new StanceDetail { Name = "Form I: Shii-Cho", AccuracyMod = 10 },
                new StanceDetail { Name = "Form I: Shii-Cho", AccuracyMod = 10, CritMod = 5 });

            // Form II: Makashi - duelist precision.
            Register(PerkType.FormMakashi, sabers,
                new StanceDetail { Name = "Form II: Makashi", CritMod = 5 },
                new StanceDetail { Name = "Form II: Makashi", CritMod = 5, AccuracyMod = 5 },
                new StanceDetail { Name = "Form II: Makashi", CritMod = 10, AccuracyMod = 5 });

            // Form III: Soresu - the wall. Trades damage for defense and deflection.
            Register(PerkType.FormSoresu, sabers,
                new StanceDetail { Name = "Form III: Soresu", DefensePhysicalMod = 3, DamagePenalty = 2 },
                new StanceDetail { Name = "Form III: Soresu", DefensePhysicalMod = 6, DeflectMod = 5, DamagePenalty = 2 },
                new StanceDetail { Name = "Form III: Soresu", DefensePhysicalMod = 9, DeflectMod = 10, DamagePenalty = 2 });

            // Form IV: Ataru - mobility. Evasion at the cost of standing defense.
            Register(PerkType.FormAtaru, sabers,
                new StanceDetail { Name = "Form IV: Ataru", EvasionMod = 5, DefensePhysicalMod = -3 },
                new StanceDetail { Name = "Form IV: Ataru", EvasionMod = 10, FlatDMG = 2, DefensePhysicalMod = -3 },
                new StanceDetail { Name = "Form IV: Ataru", EvasionMod = 15, FlatDMG = 4, DefensePhysicalMod = -3 });

            // Form V: Shien / Djem So - power. The ONLY form that re-maps the damage stat (to Might).
            Register(PerkType.FormDjemSo, sabers,
                new StanceDetail { Name = "Form V: Djem So", DamageStatOverride = AbilityType.Might, MgtModDMGHalves = 1 },
                new StanceDetail { Name = "Form V: Djem So", DamageStatOverride = AbilityType.Might, MgtModDMGHalves = 2 },
                new StanceDetail { Name = "Form V: Djem So", DamageStatOverride = AbilityType.Might, MgtModDMGHalves = 2, AccuracyMod = 5 });

            // Form VI: Niman - balance and Force economy.
            Register(PerkType.FormNiman, sabers,
                new StanceDetail { Name = "Form VI: Niman", FPRegenPerTick = 1 },
                new StanceDetail { Name = "Form VI: Niman", FPRegenPerTick = 2 },
                new StanceDetail { Name = "Form VI: Niman", FPRegenPerTick = 2, AccuracyMod = 5, EvasionMod = 5 });

            // Form VII: Juyo - ferocity with self-risk.
            Register(PerkType.FormJuyo, sabers,
                new StanceDetail { Name = "Form VII: Juyo", FlatDMG = 4, DefensePhysicalMod = -3 },
                new StanceDetail { Name = "Form VII: Juyo", FlatDMG = 8, DefensePhysicalMod = -5 },
                new StanceDetail { Name = "Form VII: Juyo", FlatDMG = 12, CritMod = 5, DefensePhysicalMod = -8 });

            // Duelist Doctrine - single-combat precision with a blade (Makashi's mirror).
            Register(PerkType.DoctrineDuelist,
                Weapons("A vibroblade or finesse vibroblade must be equipped to take this stance.",
                    Item.VibrobladeBaseItemTypes, Item.FinesseVibrobladeBaseItemTypes),
                new StanceDetail { Name = "Duelist Doctrine", CritMod = 5 },
                new StanceDetail { Name = "Duelist Doctrine", CritMod = 5, AccuracyMod = 5 },
                new StanceDetail { Name = "Duelist Doctrine", CritMod = 10, AccuracyMod = 5 });

            // Juggernaut - overwhelming force behind heavy steel (Djem So's mirror; these
            // weapons already run on Might, so no stat re-map is needed).
            Register(PerkType.DoctrineJuggernaut,
                Weapons("A heavy vibroblade or polearm must be equipped to take this stance.",
                    Item.HeavyVibrobladeBaseItemTypes, Item.PolearmBaseItemTypes),
                new StanceDetail { Name = "Juggernaut", MgtModDMGHalves = 1 },
                new StanceDetail { Name = "Juggernaut", MgtModDMGHalves = 2 },
                new StanceDetail { Name = "Juggernaut", MgtModDMGHalves = 2, AccuracyMod = 5 });

            // Tempest - twin-blade rhythm and constant motion (Ataru's mirror).
            Register(PerkType.DoctrineTempest,
                Weapons("A twin blade must be equipped to take this stance.",
                    Item.TwinBladeBaseItemTypes),
                new StanceDetail { Name = "Tempest", EvasionMod = 5, DefensePhysicalMod = -3 },
                new StanceDetail { Name = "Tempest", EvasionMod = 10, FlatDMG = 2, DefensePhysicalMod = -3 },
                new StanceDetail { Name = "Tempest", EvasionMod = 15, FlatDMG = 4, DefensePhysicalMod = -3 });

            // Teräs Käsi - the anti-Force martial art. Discipline hardens the mind and body
            // against powers; the anti-Force signatures arrive with the Phase-2 levels.
            Register(PerkType.DoctrineTerasKasi,
                Weapons("Katars or empty hands are required to take this stance.",
                    Item.KatarBaseItemTypes, new List<BaseItem> { BaseItem.Invalid }),
                new StanceDetail { Name = "Teräs Käsi", SavingThrowMod = 2 },
                new StanceDetail { Name = "Teräs Käsi", SavingThrowMod = 4, EvasionMod = 5 },
                new StanceDetail { Name = "Teräs Käsi", SavingThrowMod = 6, EvasionMod = 5 });

            // Marksman Doctrine - the aimed stance. Breath control and trigger discipline.
            Register(PerkType.DoctrineMarksman,
                Weapons("A pistol, rifle, or throwing weapon must be equipped to take this stance.",
                    Item.PistolBaseItemTypes, Item.RifleBaseItemTypes, Item.ThrowingWeaponBaseItemTypes),
                new StanceDetail { Name = "Marksman Doctrine", AccuracyMod = 5 },
                new StanceDetail { Name = "Marksman Doctrine", AccuracyMod = 10 },
                new StanceDetail { Name = "Marksman Doctrine", AccuracyMod = 10, CritMod = 5 });
        }

        private static (HashSet<BaseItem>, string) Weapons(string requirementMessage, params List<BaseItem>[] families)
        {
            return (families.SelectMany(f => f).ToHashSet(), requirementMessage);
        }

        private static void Register(
            PerkType stanceType,
            (HashSet<BaseItem>, string) weapons,
            StanceDetail level1,
            StanceDetail level2,
            StanceDetail level3)
        {
            _stances[stanceType] = new Dictionary<int, StanceDetail>
            {
                [1] = level1,
                [2] = level2,
                [3] = level3
            };
            _stanceWeapons[stanceType] = weapons.Item1;
            _weaponRequirements[stanceType] = weapons.Item2;
        }

        /// <summary>
        /// Determines whether the creature's main hand holds a weapon the given stance
        /// functions with. An empty main hand counts as the unarmed "weapon".
        /// </summary>
        public static bool IsStanceWeaponEquipped(uint creature, PerkType stanceType)
        {
            if (!_stanceWeapons.TryGetValue(stanceType, out var validWeapons))
                return false;

            var weapon = GetItemInSlot(InventorySlot.RightHand, creature);
            var itemType = GetIsObjectValid(weapon) ? GetBaseItemType(weapon) : BaseItem.Invalid;

            return validWeapons.Contains(itemType);
        }

        /// <summary>
        /// Returns an empty string when the creature's wielded weapon suits the stance,
        /// otherwise the stance's weapon requirement message (for ability validation).
        /// </summary>
        public static string ValidateStanceWeapon(uint creature, PerkType stanceType)
        {
            if (IsStanceWeaponEquipped(creature, stanceType))
                return string.Empty;

            return _weaponRequirements.TryGetValue(stanceType, out var message)
                ? message
                : "The required weapon is not equipped.";
        }

        /// <summary>
        /// Retrieves the stance a creature currently has active, or Invalid if none.
        /// </summary>
        public static PerkType GetActiveStanceType(uint creature)
        {
            return _activeTypes.TryGetValue(creature, out var stanceType) ? stanceType : PerkType.Invalid;
        }

        /// <summary>
        /// Retrieves the active stance package for a creature, or null when no stance is active
        /// or the wielded weapon has left the stance's weapon family. Hot-path safe: dictionary
        /// lookups plus a main-hand check.
        /// </summary>
        public static StanceDetail GetActiveStance(uint creature)
        {
            if (!_activeTypes.TryGetValue(creature, out var stanceType))
                return null;

            return IsStanceWeaponEquipped(creature, stanceType)
                ? _activeStances[creature]
                : null;
        }

        /// <summary>
        /// Toggles a stance on or off. Turning one stance on turns any other off (one active
        /// stance, forms and doctrines alike). The perk level is resolved here and cached;
        /// purchases and refunds deactivate the stance so the cache can never go stale.
        /// </summary>
        public static void ToggleStance(uint player, PerkType stanceType)
        {
            if (GetActiveStanceType(player) == stanceType)
            {
                var name = _activeStances.TryGetValue(player, out var current) ? current.Name : "stance";
                Deactivate(player);
                FloatingTextStringOnCreature($"You relax your stance. ({name} deactivated)", player, false);
                return;
            }

            var weaponError = ValidateStanceWeapon(player, stanceType);
            if (!string.IsNullOrEmpty(weaponError))
            {
                SendMessageToPC(player, weaponError);
                return;
            }

            var level = Perk.GetPerkLevel(player, stanceType);
            if (level <= 0 || !_stances.ContainsKey(stanceType))
                return;

            if (level > 3)
                level = 3;

            var stance = _stances[stanceType][level];
            Deactivate(player);
            _activeTypes[player] = stanceType;
            _activeStances[player] = stance;

            if (stance.SavingThrowMod != 0)
            {
                var savingThrows = TagEffect(
                    SupernaturalEffect(EffectSavingThrowIncrease((int)SavingThrow.All, stance.SavingThrowMod)),
                    SavingThrowEffectTag);
                ApplyEffectToObject(DurationType.Permanent, savingThrows, player);
            }

            FloatingTextStringOnCreature($"You settle into {stance.Name}.", player, false);
        }

        /// <summary>
        /// Deactivates a creature's active stance, if any, and strips its tagged effects.
        /// </summary>
        public static void Deactivate(uint creature)
        {
            _activeTypes.Remove(creature);
            _activeStances.Remove(creature);
            RemoveEffectByTag(creature, SavingThrowEffectTag);
        }

        /// <summary>
        /// Drops the active stance when the main hand stops matching its weapon family.
        /// The combat mods already zero out via the cache's weapon check; this keeps the
        /// toggle state honest and prevents carrying tagged effects onto other weapons.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void EnforceOnEquip()
        {
            EnforceWeaponMatch(GetPCItemLastEquippedBy());
        }

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void EnforceOnUnequip()
        {
            EnforceWeaponMatch(GetPCItemLastUnequippedBy());
        }

        private static void EnforceWeaponMatch(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;
            if (!_activeTypes.TryGetValue(creature, out var stanceType))
                return;
            if (IsStanceWeaponEquipped(creature, stanceType))
                return;

            var name = _activeStances[creature].Name;
            Deactivate(creature);
            FloatingTextStringOnCreature($"You relax your stance. ({name} deactivated)", creature, false);
        }

        /// <summary>
        /// Clears stance state when a player leaves the server.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearOnExit()
        {
            Deactivate(GetExitingObject());
        }
    }
}
