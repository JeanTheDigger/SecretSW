using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Stealth
{
    /// <summary>
    /// Ambush - the Stealth skill's opener: a strike from hiding. Only usable in
    /// stealth mode (validated before the activation stealth-break fires) and grants
    /// Stealth XP through the combat-point pipeline, so stealth-opened kills level the
    /// skill.
    /// </summary>
    public class AmbushAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder.Create(FeatType.Ambush, PerkType.Ambush)
                .Name("Ambush")
                .Level(1)
                .HasRecastDelay(RecastGroup.Ambush, 20f)
                .HasMaxRange(2.5f)
                .RequirementStamina(4)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation((activator, target, level, targetLocation) =>
                {
                    if (!GetActionMode(activator, ActionMode.Stealth))
                        return "You must be hidden to ambush.";

                    return string.Empty;
                })
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var agilityMod = GetAbilityModifier(AbilityType.Agility, activator);
                    var damage = 10 + level * 10 + agilityMod * 3;

                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Blood_Reg_Red), target);

                    CombatPoint.AddCombatPoint(activator, target, SkillType.Stealth, 3);
                    Enmity.ModifyEnmity(activator, target, 200);
                });

            return builder.Build();
        }
    }
}
