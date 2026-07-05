using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The Standard combat doctrines (Phase-1 levels 1-3) - the class mirror of the lightsaber
    /// forms, on the same stance chassis. Each doctrine gates on its weapon family's skill and
    /// only functions with that family in the main hand. One stance active at a time, shared
    /// with the forms. Levels 4-6 (veteran signatures) arrive with the datacron event content.
    /// The staff family's pair already exists as Flurry and Crushing Style.
    /// </summary>
    public class CombatDoctrinePerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BuildDoctrine(PerkType.DoctrineDuelist, FeatType.DoctrineDuelist, "Duelist Doctrine",
                SkillType.OneHanded,
                "A duelist's blade stance for vibroblades and finesse vibroblades: +5% critical chance while active.",
                "While in Duelist Doctrine, you also gain +5 accuracy.",
                "Your Duelist Doctrine critical bonus increases to 10%.");

            BuildDoctrine(PerkType.DoctrineJuggernaut, FeatType.DoctrineJuggernaut, "Juggernaut",
                SkillType.TwoHanded,
                "A crushing advance with heavy vibroblades and polearms: attacks gain DMG equal to half your MGT modifier while active.",
                "Your Juggernaut bonus DMG increases to your full MGT modifier.",
                "While in Juggernaut, you also gain +5 accuracy.");

            BuildDoctrine(PerkType.DoctrineTempest, FeatType.DoctrineTempest, "Tempest",
                SkillType.TwoHanded,
                "A whirling twin-blade stance: +5 evasion while active, at -3 physical defense.",
                "Your Tempest evasion increases to +10 and attacks deal +2 DMG.",
                "Your Tempest evasion increases to +15 and attacks deal +4 DMG.");

            BuildDoctrine(PerkType.DoctrineTerasKasi, FeatType.DoctrineTerasKasi, "Teräs Käsi",
                SkillType.MartialArts,
                "The anti-Force martial art, practiced with katars or empty hands: +2 to all saving throws while active.",
                "Your Teräs Käsi saving throw bonus increases to +4 and you gain +5 evasion.",
                "Your Teräs Käsi saving throw bonus increases to +6.");

            BuildDoctrine(PerkType.DoctrineMarksman, FeatType.DoctrineMarksman, "Marksman Doctrine",
                SkillType.Ranged,
                "An aimed stance for pistols, rifles, and throwing weapons: +5 accuracy while active.",
                "Your Marksman Doctrine accuracy bonus increases to +10.",
                "While in Marksman Doctrine, you also gain 5% critical chance.");

            return _builder.Build();
        }

        private void BuildDoctrine(
            PerkType perkType,
            FeatType featType,
            string name,
            SkillType gateSkill,
            string description1,
            string description2,
            string description3)
        {
            _builder.Create(PerkCategoryType.CombatDoctrines, perkType)
                .Name(name)
                // A purchase or refund invalidates the cached stance level; drop the stance.
                .TriggerPurchase(Stance.Deactivate)
                .TriggerRefund(Stance.Deactivate)

                .AddPerkLevel()
                .Description(description1)
                .Price(2)
                .RequirementSkill(gateSkill, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(featType)

                .AddPerkLevel()
                .Description(description2)
                .Price(3)
                .RequirementSkill(gateSkill, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description(description3)
                .Price(3)
                .RequirementSkill(gateSkill, 45)
                .RequirementCharacterType(CharacterType.Standard);
        }
    }
}
