using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The Standard combat doctrines (levels 1-6) - the class mirror of the lightsaber forms,
    /// on the same stance chassis. Each doctrine gates on its weapon family's skill and only
    /// functions with that family in the main hand. One stance active at a time, shared with
    /// the forms. Levels 1-3 are the Phase-1 arc; levels 4-6 are the veteran arc, locked
    /// behind a combat datacron (event loot) via the perk unlock system, on top of Phase-2
    /// skill gates. The staff family's pair already exists as Flurry and Crushing Style.
    /// </summary>
    public class CombatDoctrinePerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BuildDoctrine(PerkType.DoctrineDuelist, FeatType.DoctrineDuelist, "Duelist Doctrine",
                SkillType.OneHanded,
                new[]
                {
                    "A duelist's blade stance for vibroblades and finesse vibroblades: +5% critical chance while active.",
                    "While in Duelist Doctrine, you also gain +5 accuracy.",
                    "Your Duelist Doctrine critical bonus increases to 10%.",
                    "Your Duelist Doctrine critical bonus increases to 15%.",
                    "Your Duelist Doctrine accuracy bonus increases to +10.",
                    "Your Duelist Doctrine critical bonus increases to 20%."
                });

            BuildDoctrine(PerkType.DoctrineJuggernaut, FeatType.DoctrineJuggernaut, "Juggernaut",
                SkillType.TwoHanded,
                new[]
                {
                    "A crushing advance with heavy vibroblades and polearms: attacks gain DMG equal to half your MGT modifier while active.",
                    "Your Juggernaut bonus DMG increases to your full MGT modifier.",
                    "While in Juggernaut, you also gain +5 accuracy.",
                    "Your Juggernaut bonus DMG increases to 1.5x your MGT modifier.",
                    "Your Juggernaut accuracy bonus increases to +10.",
                    "Your Juggernaut bonus DMG increases to 2x your MGT modifier."
                });

            BuildDoctrine(PerkType.DoctrineTempest, FeatType.DoctrineTempest, "Tempest",
                SkillType.TwoHanded,
                new[]
                {
                    "A whirling twin-blade stance: +5 evasion while active, at -3 physical defense.",
                    "Your Tempest evasion increases to +10 and attacks deal +2 DMG.",
                    "Your Tempest evasion increases to +15 and attacks deal +4 DMG.",
                    "Your Tempest evasion increases to +20 and bonus DMG to +6.",
                    "Your Tempest evasion increases to +25 and bonus DMG to +8.",
                    "Your Tempest evasion increases to +30 and bonus DMG to +10."
                });

            BuildDoctrine(PerkType.DoctrineTerasKasi, FeatType.DoctrineTerasKasi, "Teräs Käsi",
                SkillType.MartialArts,
                new[]
                {
                    "The anti-Force martial art, practiced with katars or empty hands: +2 to all saving throws while active.",
                    "Your Teräs Käsi saving throw bonus increases to +4 and you gain +5 evasion.",
                    "Your Teräs Käsi saving throw bonus increases to +6.",
                    "Your Teräs Käsi saving throw bonus increases to +8.",
                    "Your Teräs Käsi saving throw bonus increases to +10 and evasion to +10.",
                    "Your Teräs Käsi saving throw bonus increases to +12."
                });

            BuildDoctrine(PerkType.DoctrineMarksman, FeatType.DoctrineMarksman, "Marksman Doctrine",
                SkillType.Ranged,
                new[]
                {
                    "An aimed stance for pistols, rifles, and throwing weapons: +5 accuracy while active.",
                    "Your Marksman Doctrine accuracy bonus increases to +10.",
                    "While in Marksman Doctrine, you also gain 5% critical chance.",
                    "Your Marksman Doctrine accuracy bonus increases to +15.",
                    "Your Marksman Doctrine critical bonus increases to 10%.",
                    "Your Marksman Doctrine accuracy bonus increases to +20."
                });

            return _builder.Build();
        }

        private void BuildDoctrine(
            PerkType perkType,
            FeatType featType,
            string name,
            SkillType gateSkill,
            string[] descriptions)
        {
            _builder.Create(PerkCategoryType.CombatDoctrines, perkType)
                .Name(name)
                // A purchase or refund invalidates the cached stance level; drop the stance.
                .TriggerPurchase(Stance.Deactivate)
                .TriggerRefund(Stance.Deactivate)

                // The Phase-1 arc.
                .AddPerkLevel()
                .Description(descriptions[0])
                .Price(2)
                .RequirementSkill(gateSkill, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(featType)

                .AddPerkLevel()
                .Description(descriptions[1])
                .Price(3)
                .RequirementSkill(gateSkill, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description(descriptions[2])
                .Price(3)
                .RequirementSkill(gateSkill, 45)
                .RequirementCharacterType(CharacterType.Standard)

                // The veteran arc (Phase 2): datacron-unlocked, and the skill gates sit past
                // rank 50 so they are only reachable beyond the Trials.
                .AddPerkLevel()
                .Description(descriptions[3])
                .Price(5)
                .RequirementUnlocked()
                .RequirementSkill(gateSkill, 60)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description(descriptions[4])
                .Price(5)
                .RequirementSkill(gateSkill, 75)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description(descriptions[5])
                .Price(6)
                .RequirementSkill(gateSkill, 90)
                .RequirementCharacterType(CharacterType.Standard);
        }
    }
}
