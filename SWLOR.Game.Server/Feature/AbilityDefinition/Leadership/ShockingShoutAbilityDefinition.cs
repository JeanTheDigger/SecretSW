using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public class ShockingShoutAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ShockingShout();

            return _builder.Build();
        }

        private void ShockingShout()
        {
            _builder.Create(FeatType.ShockingShout, PerkType.ShockingShout)
                .Name("Shocking Shout")
                .Level(1)
                .HasRecastDelay(RecastGroup.ShockingShout, 120f)
                .RequirementStamina(5)
                .HasActivationDelay(0.5f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasImpactAction((activator, target, level, location) =>
                {
                    const float RangeMeters = 10f;
                    const int MaxTargets = 6;
                    var nth = 1;
                    var count = 0;

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst), activator);

                    var nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, nth);
                    while (GetIsObjectValid(nearest))
                    {

                        if (GetDistanceBetween(activator, nearest) > RangeMeters)
                        {
                            break;
                        }

                        // Friendly-fire areas sweep allies into the shout too; open world stays enemy-only.
                        if (Ability.IsFriendlyFireArea(activator) || GetIsReactionTypeHostile(nearest, activator))
                        {
                            count++;

                            var socMod = GetAbilityModifier(AbilityType.Social, activator);
                            var dc = 22 + socMod * 2 / 3;
                            const float BaseDuration = 2f;
                            const float MaxDuration = 4f;
                            var bonusDuration = GetAbilityModifier(AbilityType.Social, activator) * 0.25f;
                            var duration = Math.Min(BaseDuration + bonusDuration, MaxDuration);

                            var checkResult = WillSave(nearest, dc, SavingThrowType.None, activator);
                            if (checkResult == SavingThrowResultType.Failed)
                            {
                                ApplyEffectToObject(DurationType.Temporary, EffectStunned(), nearest, duration);
                                Ability.ApplyTemporaryImmunity(nearest, duration, ImmunityType.Stun);
                                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Sonic), nearest);
                            }

                            // Enmity / combat points only for genuine enemies — a friendly-fired ally won't retaliate.
                            if (GetIsReactionTypeHostile(nearest, activator))
                            {
                                CombatPoint.AddCombatPoint(activator, nearest, SkillType.Leadership, 3);
                                Enmity.ModifyEnmity(activator, nearest, 650);
                            }
                        }

                        if (count >= MaxTargets)
                        {
                            break;
                        }

                        nth++;
                        nearest = GetNearestCreature(CreatureType.IsAlive, 1, activator, nth);
                    }

                });
        }
    }
}
