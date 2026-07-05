using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StanceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The combat stance system behind the seven lightsaber forms.
    /// A player knows any number of forms (perks) but has at most ONE active. The active
    /// form's stat package is resolved and cached at toggle time; combat hot paths read the
    /// cache and treat the stance as inactive whenever the wielded weapon stops being a
    /// lightsaber or saberstaff. Design rule: only a form may re-map a weapon's stats.
    /// </summary>
    public static class Stance
    {
        private static readonly Dictionary<PerkType, Dictionary<int, StanceDetail>> _forms = new();
        private static readonly Dictionary<uint, PerkType> _activeForms = new();
        private static readonly Dictionary<uint, StanceDetail> _activeStances = new();

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterForms()
        {
            // Form I: Shii-Cho - the steady training form.
            Register(PerkType.FormShiiCho,
                new StanceDetail { Name = "Form I: Shii-Cho", AccuracyMod = 5 },
                new StanceDetail { Name = "Form I: Shii-Cho", AccuracyMod = 10 },
                new StanceDetail { Name = "Form I: Shii-Cho", AccuracyMod = 10, CritMod = 5 });

            // Form II: Makashi - duelist precision.
            Register(PerkType.FormMakashi,
                new StanceDetail { Name = "Form II: Makashi", CritMod = 5 },
                new StanceDetail { Name = "Form II: Makashi", CritMod = 5, AccuracyMod = 5 },
                new StanceDetail { Name = "Form II: Makashi", CritMod = 10, AccuracyMod = 5 });

            // Form III: Soresu - the wall. Trades damage for defense and deflection.
            Register(PerkType.FormSoresu,
                new StanceDetail { Name = "Form III: Soresu", DefensePhysicalMod = 3, DamagePenalty = 2 },
                new StanceDetail { Name = "Form III: Soresu", DefensePhysicalMod = 6, DeflectMod = 5, DamagePenalty = 2 },
                new StanceDetail { Name = "Form III: Soresu", DefensePhysicalMod = 9, DeflectMod = 10, DamagePenalty = 2 });

            // Form IV: Ataru - mobility. Evasion at the cost of standing defense.
            Register(PerkType.FormAtaru,
                new StanceDetail { Name = "Form IV: Ataru", EvasionMod = 5, DefensePhysicalMod = -3 },
                new StanceDetail { Name = "Form IV: Ataru", EvasionMod = 10, FlatDMG = 2, DefensePhysicalMod = -3 },
                new StanceDetail { Name = "Form IV: Ataru", EvasionMod = 15, FlatDMG = 4, DefensePhysicalMod = -3 });

            // Form V: Shien / Djem So - power. The ONLY form that re-maps the damage stat (to Might).
            Register(PerkType.FormDjemSo,
                new StanceDetail { Name = "Form V: Djem So", DamageStatOverride = AbilityType.Might, MgtModDMGHalves = 1 },
                new StanceDetail { Name = "Form V: Djem So", DamageStatOverride = AbilityType.Might, MgtModDMGHalves = 2 },
                new StanceDetail { Name = "Form V: Djem So", DamageStatOverride = AbilityType.Might, MgtModDMGHalves = 2, AccuracyMod = 5 });

            // Form VI: Niman - balance and Force economy.
            Register(PerkType.FormNiman,
                new StanceDetail { Name = "Form VI: Niman", FPRegenPerTick = 1 },
                new StanceDetail { Name = "Form VI: Niman", FPRegenPerTick = 2 },
                new StanceDetail { Name = "Form VI: Niman", FPRegenPerTick = 2, AccuracyMod = 5, EvasionMod = 5 });

            // Form VII: Juyo - ferocity with self-risk.
            Register(PerkType.FormJuyo,
                new StanceDetail { Name = "Form VII: Juyo", FlatDMG = 4, DefensePhysicalMod = -3 },
                new StanceDetail { Name = "Form VII: Juyo", FlatDMG = 8, DefensePhysicalMod = -5 },
                new StanceDetail { Name = "Form VII: Juyo", FlatDMG = 12, CritMod = 5, DefensePhysicalMod = -8 });
        }

        private static void Register(PerkType form, StanceDetail level1, StanceDetail level2, StanceDetail level3)
        {
            _forms[form] = new Dictionary<int, StanceDetail>
            {
                [1] = level1,
                [2] = level2,
                [3] = level3
            };
        }

        /// <summary>
        /// Determines whether a lightsaber-class weapon (lightsaber or saberstaff) is in the main hand.
        /// Forms only function with these weapons.
        /// </summary>
        public static bool IsSaberEquipped(uint creature)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, creature);
            var itemType = GetBaseItemType(weapon);

            return Item.LightsaberBaseItemTypes.Contains(itemType) ||
                   Item.SaberstaffBaseItemTypes.Contains(itemType);
        }

        /// <summary>
        /// Retrieves the form a creature currently has active, or Invalid if none.
        /// </summary>
        public static PerkType GetActiveForm(uint creature)
        {
            return _activeForms.TryGetValue(creature, out var form) ? form : PerkType.Invalid;
        }

        /// <summary>
        /// Retrieves the active stance package for a creature, or null when no form is active
        /// or the wielded weapon is not a lightsaber/saberstaff. Hot-path safe: one dictionary
        /// lookup plus a main-hand check.
        /// </summary>
        public static StanceDetail GetActiveStance(uint creature)
        {
            if (!_activeStances.TryGetValue(creature, out var stance))
                return null;

            return IsSaberEquipped(creature) ? stance : null;
        }

        /// <summary>
        /// Toggles a form on or off. Turning one form on turns any other off (one active form).
        /// The perk level is resolved here and cached; purchases and refunds deactivate the
        /// stance so the cache can never go stale.
        /// </summary>
        public static void ToggleForm(uint player, PerkType form)
        {
            if (GetActiveForm(player) == form)
            {
                var name = _activeStances.TryGetValue(player, out var current) ? current.Name : "form";
                Deactivate(player);
                FloatingTextStringOnCreature($"You relax your stance. ({name} deactivated)", player, false);
                return;
            }

            if (!IsSaberEquipped(player))
            {
                SendMessageToPC(player, "A lightsaber or saberstaff must be equipped to take a form.");
                return;
            }

            var level = Perk.GetPerkLevel(player, form);
            if (level <= 0 || !_forms.ContainsKey(form))
                return;

            if (level > 3)
                level = 3;

            var stance = _forms[form][level];
            _activeForms[player] = form;
            _activeStances[player] = stance;

            FloatingTextStringOnCreature($"You settle into {stance.Name}.", player, false);
        }

        /// <summary>
        /// Deactivates a creature's active form, if any.
        /// </summary>
        public static void Deactivate(uint creature)
        {
            _activeForms.Remove(creature);
            _activeStances.Remove(creature);
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
