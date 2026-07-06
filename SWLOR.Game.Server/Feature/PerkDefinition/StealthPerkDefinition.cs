using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// Stealth starter perks (Stage 7g). Stealth is a new cap-contributing skill riding
    /// the existing engine stealth machinery (Hide/Move Silently + stealth mode).
    /// </summary>
    public class StealthPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Ambush();
            Concealment();

            return _builder.Build();
        }

        private void Ambush()
        {
            _builder.Create(PerkCategoryType.Stealth, PerkType.Ambush)
                .Name("Ambush")

                .AddPerkLevel()
                .Description("Strike from hiding: Agility-scaled bonus damage. Usable only while in stealth mode. Grants Stealth XP.")
                .Price(1)
                .RequirementSkill(SkillType.Stealth, 5)
                .GrantsFeat(FeatType.Ambush)

                .AddPerkLevel()
                .Description("A deadlier opening: significantly more damage.")
                .Price(2)
                .RequirementSkill(SkillType.Stealth, 25)

                .AddPerkLevel()
                .Description("The killing stroke: heavy damage from the shadows.")
                .Price(3)
                .RequirementSkill(SkillType.Stealth, 45);
        }

        private void Concealment()
        {
            _builder.Create(PerkCategoryType.Stealth, PerkType.Concealment)
                .Name("Concealment")

                .AddPerkLevel()
                .Description("You blend into your surroundings: +3 Hide and Move Silently.")
                .Price(2)
                .RequirementSkill(SkillType.Stealth, 15)
                .TriggerPurchase(StealthConcealment.Recalculate)
                .TriggerRefund(StealthConcealment.Recalculate)

                .AddPerkLevel()
                .Description("You are a whisper: +6 Hide and Move Silently.")
                .Price(3)
                .RequirementSkill(SkillType.Stealth, 35)
                .TriggerPurchase(StealthConcealment.Recalculate)
                .TriggerRefund(StealthConcealment.Recalculate);
        }
    }
}
