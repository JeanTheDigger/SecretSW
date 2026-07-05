using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The seven lightsaber forms (Phase-1 levels 1-3). Forms are blade technique: they gate
    /// on the wielded weapon's skill (One-Handed OR Two-Handed), not the Force skill, and only
    /// function with a lightsaber or saberstaff in the main hand. One form active at a time.
    /// Levels 4-6 (Knight signatures) arrive with the holocron event content.
    /// </summary>
    public class LightsaberFormPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BuildForm(PerkType.FormShiiCho, FeatType.FormShiiCho, "Form I: Shii-Cho",
                "A steady, balanced stance: +5 accuracy while active.",
                "Your Shii-Cho accuracy bonus increases to +10.",
                "While in Shii-Cho, you also gain 5% critical chance.",
                15, 30, 45);

            BuildForm(PerkType.FormMakashi, FeatType.FormMakashi, "Form II: Makashi",
                "A duelist's precision stance: +5% critical chance while active.",
                "While in Makashi, you also gain +5 accuracy.",
                "Your Makashi critical bonus increases to 10%.",
                15, 30, 45);

            BuildForm(PerkType.FormSoresu, FeatType.FormSoresu, "Form III: Soresu",
                "A defensive wall: +3 physical defense while active, at -2 DMG.",
                "Your Soresu defense increases to +6 and you gain +5% chance to deflect blaster fire.",
                "Your Soresu defense increases to +9 and deflection to +10%.",
                15, 30, 45);

            BuildForm(PerkType.FormAtaru, FeatType.FormAtaru, "Form IV: Ataru",
                "An acrobatic stance: +5 evasion while active, at -3 physical defense.",
                "Your Ataru evasion increases to +10 and attacks deal +2 DMG.",
                "Your Ataru evasion increases to +15 and attacks deal +4 DMG.",
                15, 30, 45);

            BuildForm(PerkType.FormDjemSo, FeatType.FormDjemSo, "Form V: Djem So",
                "A power stance: saber damage is driven by Might while active, and attacks gain DMG equal to half your MGT modifier.",
                "Your Djem So bonus DMG increases to your full MGT modifier.",
                "While in Djem So, you also gain +5 accuracy.",
                15, 30, 45);

            BuildForm(PerkType.FormNiman, FeatType.FormNiman, "Form VI: Niman",
                "A balanced, meditative stance: +1 FP on each natural regeneration tick while active.",
                "Your Niman FP recovery increases to +2 per tick.",
                "While in Niman, you also gain +5 accuracy and +5 evasion.",
                15, 30, 45);

            BuildForm(PerkType.FormJuyo, FeatType.FormJuyo, "Form VII: Juyo",
                "A ferocious stance: +4 DMG while active, at -3 physical defense.",
                "Your Juyo bonus increases to +8 DMG, at -5 physical defense.",
                "Your Juyo bonus increases to +12 DMG and 5% critical chance, at -8 physical defense.",
                25, 40, 50);

            return _builder.Build();
        }

        private void BuildForm(
            PerkType perkType,
            FeatType featType,
            string name,
            string description1,
            string description2,
            string description3,
            int gate1,
            int gate2,
            int gate3)
        {
            _builder.Create(PerkCategoryType.LightsaberForms, perkType)
                .Name(name)
                // A purchase or refund invalidates the cached stance level; drop the stance.
                .TriggerPurchase(Stance.Deactivate)
                .TriggerRefund(Stance.Deactivate)

                .AddPerkLevel()
                .Description(description1)
                .Price(2)
                .RequirementAnySkill(gate1, SkillType.OneHanded, SkillType.TwoHanded)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(featType)

                .AddPerkLevel()
                .Description(description2)
                .Price(3)
                .RequirementAnySkill(gate2, SkillType.OneHanded, SkillType.TwoHanded)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description(description3)
                .Price(3)
                .RequirementAnySkill(gate3, SkillType.OneHanded, SkillType.TwoHanded)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }
    }
}
