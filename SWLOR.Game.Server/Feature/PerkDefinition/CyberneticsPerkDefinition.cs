using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// Cybernetic implants (Standard-only; canon: cybernetics erode the Force connection,
    /// so FS characters cannot install them). Seven passive six-level lines gated on total
    /// SP rather than a single skill - implants are money-and-surgery, not technique. A
    /// character supports two installed lines (three after the Trials); swapping rides the
    /// perk refund machinery. Levels 4-6 are the prototype arc, locked behind an
    /// event-looted prototype schematic on top of Phase-2 total-SP gates.
    /// </summary>
    public class CyberneticsPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BuildImplant(PerkType.ImplantNeural, "Neural Processor",
                new[]
                {
                    "Reflex-prediction wetware: +2 evasion.",
                    "Your Neural Processor evasion increases to +4.",
                    "Your Neural Processor evasion increases to +6.",
                    "Your Neural Processor evasion increases to +8.",
                    "Your Neural Processor evasion increases to +10.",
                    "Your Neural Processor evasion increases to +12."
                });

            BuildImplant(PerkType.ImplantOcular, "Ocular Targeting",
                new[]
                {
                    "A targeting overlay etched onto the retina: +2 accuracy.",
                    "Your Ocular Targeting accuracy increases to +4.",
                    "Your Ocular Targeting accuracy increases to +6.",
                    "Your Ocular Targeting accuracy increases to +8.",
                    "Your Ocular Targeting accuracy increases to +10.",
                    "Your Ocular Targeting accuracy increases to +12 and you gain 5% critical chance."
                });

            BuildImplant(PerkType.ImplantDermal, "Dermal Plating",
                new[]
                {
                    "Subdermal armor weave: +2 physical defense.",
                    "Your Dermal Plating defense increases to +4.",
                    "Your Dermal Plating defense increases to +6.",
                    "Your Dermal Plating defense increases to +8.",
                    "Your Dermal Plating defense increases to +10.",
                    "Your Dermal Plating defense increases to +12."
                });

            BuildImplant(PerkType.ImplantSkeletal, "Skeletal Reinforcement",
                new[]
                {
                    "Alloy-laced bone and myomer bundles: +1 Might.",
                    "Your Skeletal Reinforcement is refined (still +1 Might, improved fittings).",
                    "Your Skeletal Reinforcement Might bonus increases to +2.",
                    "Your Skeletal Reinforcement is hardened (still +2 Might, improved fittings).",
                    "Your Skeletal Reinforcement Might bonus increases to +3.",
                    "Your Skeletal Reinforcement is perfected (+3 Might, prototype fittings)."
                });

            BuildImplant(PerkType.ImplantCardio, "Cardio Regulator",
                new[]
                {
                    "A regulated second heart: +1 stamina on each natural regeneration tick.",
                    "Your Cardio Regulator recovery increases to +2 per tick.",
                    "Your Cardio Regulator recovery increases to +3 per tick.",
                    "Your Cardio Regulator recovery increases to +4 per tick.",
                    "Your Cardio Regulator recovery increases to +5 per tick.",
                    "Your Cardio Regulator recovery increases to +6 per tick."
                });

            BuildImplant(PerkType.ImplantServo, "Servo Actuators",
                new[]
                {
                    "Servo-assisted joints: +3% movement speed.",
                    "Your Servo Actuators speed increases to +6%.",
                    "Your Servo Actuators speed increases to +9%.",
                    "Your Servo Actuators speed increases to +12%.",
                    "Your Servo Actuators speed increases to +15%.",
                    "Your Servo Actuators speed increases to +18%."
                });

            BuildImplant(PerkType.ImplantCortical, "Cortical Shield",
                new[]
                {
                    "A faraday mesh around the mind: +1 to all saving throws.",
                    "Your Cortical Shield bonus increases to +2.",
                    "Your Cortical Shield bonus increases to +3.",
                    "Your Cortical Shield bonus increases to +4.",
                    "Your Cortical Shield bonus increases to +5.",
                    "Your Cortical Shield bonus increases to +6."
                });

            return _builder.Build();
        }

        private void BuildImplant(PerkType perkType, string name, string[] descriptions)
        {
            _builder.Create(PerkCategoryType.Cybernetics, perkType)
                .Name(name)
                .TriggerPurchase(Implant.Recalculate)
                .TriggerRefund(Implant.Recalculate)

                // The market arc (Phase 1): gated on total SP - implants are surgery, not technique.
                .AddPerkLevel()
                .Description(descriptions[0])
                .Price(2)
                .RequirementImplantSlot()
                .RequirementTotalSP(75)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description(descriptions[1])
                .Price(3)
                .RequirementTotalSP(175)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description(descriptions[2])
                .Price(3)
                .RequirementTotalSP(275)
                .RequirementCharacterType(CharacterType.Standard)

                // The prototype arc (Phase 2): schematic-unlocked event tech.
                .AddPerkLevel()
                .Description(descriptions[3])
                .Price(5)
                .RequirementUnlocked()
                .RequirementTotalSP(400)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description(descriptions[4])
                .Price(5)
                .RequirementTotalSP(500)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description(descriptions[5])
                .Price(6)
                .RequirementTotalSP(600)
                .RequirementCharacterType(CharacterType.Standard);
        }
    }
}
