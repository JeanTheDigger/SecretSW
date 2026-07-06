using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    /// <summary>
    /// Toxin Vials - the toxin warfare kit's combat face (crafted vial items join the
    /// map-phase recipe pass). Hurls a flask that poisons the target; the poison ticks
    /// on the existing Perception-scaled status machinery.
    /// </summary>
    public class ToxinFlaskAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder.Create(FeatType.ToxinFlask, PerkType.ToxinVials)
                .Name("Toxin Flask")
                .Level(1)
                .HasRecastDelay(RecastGroup.FragGrenade, 24f)
                .HasActivationDelay(1f)
                .HasMaxRange(15f)
                .UsesAnimation(Animation.ThrowGrenade)
                .RequirementStamina(3)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var duration = level >= 2 ? 30f : 18f;
                    StatusEffect.Apply(activator, target, StatusEffectType.Poison, duration);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Poison_S), target);

                    CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
                    Enmity.ModifyEnmity(activator, target, 120);
                });

            return builder.Build();
        }
    }
}
