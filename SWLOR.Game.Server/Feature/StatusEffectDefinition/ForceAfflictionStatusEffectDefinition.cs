using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Affliction - the dark side's creeping sickness. Ticks Willpower-modifier-scaled
    /// damage (Option-A scaling); the effectData carries the perk level.
    /// </summary>
    public class ForceAfflictionStatusEffectDefinition : IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            _builder.Create(StatusEffectType.ForceAffliction)
                .Name("Affliction")
                .EffectIcon(EffectIconType.Disease)
                .TickAction((source, target, effectData) =>
                {
                    var level = effectData == null ? 1 : (int)effectData;
                    if (level < 1)
                        level = 1;

                    var willpower = GetAbilityModifier(AbilityType.Willpower, source);
                    if (willpower < 0)
                        willpower = 0;

                    var damage = EffectDamage(2 + willpower * level, DamageType.Negative);
                    ApplyEffectToObject(DurationType.Instant, damage, target);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Disease_S), target);
                });

            return _builder.Build();
        }
    }
}
