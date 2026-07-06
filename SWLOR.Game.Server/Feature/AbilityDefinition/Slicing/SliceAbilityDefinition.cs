using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Slicing
{
    /// <summary>
    /// Slice - the Slicing skill's combat active: an intrusion spike against droids,
    /// turrets, and other mechanical systems. Grants Slicing XP on use.
    /// </summary>
    public class SliceAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder.Create(FeatType.Slice, PerkType.Slice)
                .Name("Slice")
                .Level(1)
                .HasRecastDelay(RecastGroup.Slice, 12f)
                .HasActivationDelay(1f)
                .HasMaxRange(10f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation((activator, target, level, targetLocation) =>
                {
                    if (GetRacialType(target) != RacialType.Robot)
                        return "Only droids and mechanical systems can be sliced.";

                    return string.Empty;
                })
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var perceptionMod = GetAbilityModifier(AbilityType.Perception, activator);
                    var damage = 8 + level * 8 + perceptionMod * 2;
                    var dazeDuration = level >= 3 ? 3f : 2f;

                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Electrical), target);
                    ApplyEffectToObject(DurationType.Temporary, EffectDazed(), target, dazeDuration);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Lightning_S), target);

                    CombatPoint.AddCombatPoint(activator, target, SkillType.Slicing, 3);
                    Enmity.ModifyEnmity(activator, target, 150);
                });

            return builder.Build();
        }
    }
}
