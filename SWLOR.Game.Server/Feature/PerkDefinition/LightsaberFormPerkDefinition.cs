using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The seven lightsaber forms (levels 1-6). Forms are blade technique: they gate on the
    /// wielded weapon's skill (One-Handed OR Two-Handed), not the Force skill, and only
    /// function with a lightsaber or saberstaff in the main hand. One form active at a time.
    /// Levels 1-3 are the Padawan arc; levels 4-6 are the Knight arc, locked behind a
    /// holocron (event loot) via the perk unlock system, on top of Phase-2 skill gates.
    /// </summary>
    public class LightsaberFormPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BuildForm(PerkType.FormShiiCho, FeatType.FormShiiCho, "Form I: Shii-Cho",
                new[]
                {
                    "A steady, balanced stance: +5 accuracy while active.",
                    "Your Shii-Cho accuracy bonus increases to +10.",
                    "While in Shii-Cho, you also gain 5% critical chance.",
                    "Your Shii-Cho accuracy bonus increases to +15.",
                    "Your Shii-Cho critical bonus increases to 10%.",
                    "Your Shii-Cho accuracy bonus increases to +20."
                },
                15, 30, 45);

            BuildForm(PerkType.FormMakashi, FeatType.FormMakashi, "Form II: Makashi",
                new[]
                {
                    "A duelist's precision stance: +5% critical chance while active.",
                    "While in Makashi, you also gain +5 accuracy.",
                    "Your Makashi critical bonus increases to 10%.",
                    "Your Makashi critical bonus increases to 15%.",
                    "Your Makashi accuracy bonus increases to +10.",
                    "Your Makashi critical bonus increases to 20%."
                },
                15, 30, 45);

            BuildForm(PerkType.FormSoresu, FeatType.FormSoresu, "Form III: Soresu",
                new[]
                {
                    "A defensive wall: +3 physical defense while active, at -2 DMG.",
                    "Your Soresu defense increases to +6 and you gain +5% chance to deflect blaster fire.",
                    "Your Soresu defense increases to +9 and deflection to +10%.",
                    "Your Soresu defense increases to +12 and deflection to +15%.",
                    "Your Soresu defense increases to +15 and deflection to +20%.",
                    "Your Soresu defense increases to +18 and deflection to +25%."
                },
                15, 30, 45);

            BuildForm(PerkType.FormAtaru, FeatType.FormAtaru, "Form IV: Ataru",
                new[]
                {
                    "An acrobatic stance: +5 evasion while active, at -3 physical defense.",
                    "Your Ataru evasion increases to +10 and attacks deal +2 DMG.",
                    "Your Ataru evasion increases to +15 and attacks deal +4 DMG.",
                    "Your Ataru evasion increases to +20 and bonus DMG to +6.",
                    "Your Ataru evasion increases to +25 and bonus DMG to +8.",
                    "Your Ataru evasion increases to +30 and bonus DMG to +10."
                },
                15, 30, 45);

            BuildForm(PerkType.FormDjemSo, FeatType.FormDjemSo, "Form V: Djem So",
                new[]
                {
                    "A power stance: saber damage is driven by Might while active, and attacks gain DMG equal to half your MGT modifier.",
                    "Your Djem So bonus DMG increases to your full MGT modifier.",
                    "While in Djem So, you also gain +5 accuracy.",
                    "Your Djem So bonus DMG increases to 1.5x your MGT modifier.",
                    "Your Djem So accuracy bonus increases to +10.",
                    "Your Djem So bonus DMG increases to 2x your MGT modifier."
                },
                15, 30, 45);

            BuildForm(PerkType.FormNiman, FeatType.FormNiman, "Form VI: Niman",
                new[]
                {
                    "A balanced, meditative stance: +1 FP on each natural regeneration tick while active.",
                    "Your Niman FP recovery increases to +2 per tick.",
                    "While in Niman, you also gain +5 accuracy and +5 evasion.",
                    "Your Niman FP recovery increases to +3 per tick.",
                    "Your Niman FP recovery increases to +4 per tick, accuracy to +10, and evasion to +10.",
                    "Your Niman FP recovery increases to +5 per tick."
                },
                15, 30, 45);

            BuildForm(PerkType.FormJuyo, FeatType.FormJuyo, "Form VII: Juyo",
                new[]
                {
                    "A ferocious stance: +4 DMG while active, at -3 physical defense.",
                    "Your Juyo bonus increases to +8 DMG, at -5 physical defense.",
                    "Your Juyo bonus increases to +12 DMG and 5% critical chance, at -8 physical defense.",
                    "Your Juyo bonus increases to +16 DMG.",
                    "Your Juyo bonus increases to +20 DMG and 10% critical chance, at -10 physical defense.",
                    "Your Juyo bonus increases to +24 DMG and 15% critical chance."
                },
                25, 40, 50, 65, 80, 95);

            return _builder.Build();
        }

        private void BuildForm(
            PerkType perkType,
            FeatType featType,
            string name,
            string[] descriptions,
            int gate1,
            int gate2,
            int gate3,
            int gate4 = 60,
            int gate5 = 75,
            int gate6 = 90)
        {
            var (signatureFeat, signatureName) = _signatures[perkType];
            _builder.Create(PerkCategoryType.LightsaberForms, perkType)
                .Name(name)
                // A purchase or refund invalidates the cached stance level; drop the stance.
                .TriggerPurchase(Stance.Deactivate)
                .TriggerRefund(Stance.Deactivate)

                // The Padawan arc (Phase 1).
                .AddPerkLevel()
                .Description(descriptions[0])
                .Price(2)
                .RequirementAnySkill(gate1, SkillType.OneHanded, SkillType.TwoHanded)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(featType)

                .AddPerkLevel()
                .Description(descriptions[1])
                .Price(3)
                .RequirementAnySkill(gate2, SkillType.OneHanded, SkillType.TwoHanded)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description(descriptions[2])
                .Price(3)
                .RequirementAnySkill(gate3, SkillType.OneHanded, SkillType.TwoHanded)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                // The Knight arc (Phase 2): holocron-unlocked, and the skill gates sit past
                // rank 50 so they are only reachable beyond the Trials.
                .AddPerkLevel()
                .Description(descriptions[3])
                .Price(5)
                .RequirementUnlocked()
                .RequirementAnySkill(gate4, SkillType.OneHanded, SkillType.TwoHanded)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description(descriptions[4])
                .Price(5)
                .RequirementAnySkill(gate5, SkillType.OneHanded, SkillType.TwoHanded)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description($"{descriptions[5]} Grants the signature technique: {signatureName}.")
                .Price(6)
                .RequirementAnySkill(gate6, SkillType.OneHanded, SkillType.TwoHanded)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(signatureFeat);
        }

        // The level-6 capstone active of each form.
        private static readonly Dictionary<PerkType, (FeatType, string)> _signatures = new()
        {
            [PerkType.FormShiiCho] = (FeatType.SarlaccSweep, "Sarlacc Sweep"),
            [PerkType.FormMakashi] = (FeatType.DuelistsEnd, "Duelist's End"),
            [PerkType.FormSoresu] = (FeatType.CircleOfShelter, "Circle of Shelter"),
            [PerkType.FormAtaru] = (FeatType.HawkBatSwoop, "Hawk-Bat Swoop"),
            [PerkType.FormDjemSo] = (FeatType.FallingAvalanche, "Falling Avalanche"),
            [PerkType.FormNiman] = (FeatType.NimanBalance, "Balance"),
            [PerkType.FormJuyo] = (FeatType.Vaapad, "Vaapad"),
        };
    }
}
