using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// Slicing starter perks (Stage 7g). Slicing is a new cap-contributing skill: the
    /// anti-droid/electronics combat trade. Both classes may learn it.
    /// </summary>
    public class SlicingPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Slice();
            SalvageProtocols();

            return _builder.Build();
        }

        private void Slice()
        {
            _builder.Create(PerkCategoryType.Slicing, PerkType.Slice)
                .Name("Slice")

                .AddPerkLevel()
                .Description("Slices a droid or turret's systems: Perception-scaled electrical damage and a 2 second system daze. Grants Slicing XP.")
                .Price(1)
                .RequirementSkill(SkillType.Slicing, 5)
                .GrantsFeat(FeatType.Slice)

                .AddPerkLevel()
                .Description("Deeper intrusion: significantly more damage.")
                .Price(2)
                .RequirementSkill(SkillType.Slicing, 25)

                .AddPerkLevel()
                .Description("Root access: heavy damage and a 3 second system daze.")
                .Price(3)
                .RequirementSkill(SkillType.Slicing, 45);
        }

        private void SalvageProtocols()
        {
            _builder.Create(PerkCategoryType.Slicing, PerkType.SalvageProtocols)
                .Name("Salvage Protocols")

                .AddPerkLevel()
                .Description("Destroyed droids yield more credits and a 10% better chance at rare components.")
                .Price(2)
                .RequirementSkill(SkillType.Slicing, 15)

                .AddPerkLevel()
                .Description("Destroyed droids yield even more credits and a 20% better chance at rare components.")
                .Price(3)
                .RequirementSkill(SkillType.Slicing, 35);
        }
    }
}
