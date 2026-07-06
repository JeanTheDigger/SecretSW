using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    /// <summary>
    /// The Phase-1 Force kit gap fills: Force Barrier (absorb), Force Breach (the game's
    /// first dispel), Affliction (creeping damage), and Force Choke (the grip). Each is
    /// one feat whose potency scales with the perk level; FP costs scale per level and
    /// are validated and consumed manually (respecting Force Attunement's zero-cost rule).
    /// All numbers are Willpower-modifier-scaled per the Option-A regime.
    /// </summary>
    public class ForceKitAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceBarrier(builder);
            ForceBreach(builder);
            ForceAffliction(builder);
            ForceChoke(builder);

            return builder.Build();
        }

        private static AbilityCustomValidationAction RequireFP(int[] costs)
        {
            return (activator, target, level, targetLocation) =>
            {
                var index = level < 1 ? 0 : level > costs.Length ? costs.Length - 1 : level - 1;
                return Stat.GetCurrentFP(activator) < costs[index]
                    ? $"Not enough FP. (Required: {costs[index]})"
                    : string.Empty;
            };
        }

        private static void ConsumeFP(uint activator, int[] costs, int level)
        {
            if (GetIsDM(activator))
                return;
            if (StatusEffect.HasStatusEffect(activator, StatusEffectType.ForceAttunement))
                return;

            var index = level < 1 ? 0 : level > costs.Length ? costs.Length - 1 : level - 1;
            Stat.ReduceFP(activator, costs[index]);
        }

        // Force Barrier: an absorb shield. The Force takes the blows the body cannot.
        private static void ForceBarrier(AbilityBuilder builder)
        {
            var costs = new[] { 3, 4, 5 };

            builder.Create(FeatType.ForceBarrier, PerkType.ForceBarrier)
                .Name("Force Barrier")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceBarrier, 30f)
                .HasActivationDelay(1f)
                .IsCastedAbility()
                .HasCustomValidation(RequireFP(costs))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ConsumeFP(activator, costs, level);

                    var willpower = GetAbilityModifier(AbilityType.Willpower, activator);
                    if (willpower < 0)
                        willpower = 0;

                    var amount = level switch
                    {
                        1 => 15 + willpower * 2,
                        2 => 30 + willpower * 3,
                        _ => 45 + willpower * 4
                    };

                    ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(amount), activator, 60f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_White), activator);
                    FloatingTextStringOnCreature($"A barrier of the Force surrounds you. ({amount})", activator, false);

                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 2);
                });
        }

        // Force Breach: tear the target's protections away - the game's first dispel.
        private static void ForceBreach(AbilityBuilder builder)
        {
            var costs = new[] { 4, 6 };

            // The beneficial effect types Breach strips, in strip order.
            var breachableTypes = new[]
            {
                EffectTypeScript.TemporaryHitpoints,
                EffectTypeScript.DamageIncrease,
                EffectTypeScript.AttackIncrease,
                EffectTypeScript.ACIncrease,
                EffectTypeScript.SavingThrowIncrease,
                EffectTypeScript.Regenerate,
                EffectTypeScript.Concealment,
                EffectTypeScript.Haste,
                EffectTypeScript.MovementSpeedIncrease,
                EffectTypeScript.AbilityIncrease
            };

            builder.Create(FeatType.ForceBreach, PerkType.ForceBreach)
                .Name("Force Breach")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceBreach, 30f)
                .HasActivationDelay(1f)
                .HasMaxRange(15f)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation(RequireFP(costs))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ConsumeFP(activator, costs, level);

                    var toRemove = level >= 2 ? 4 : 2;
                    var removed = 0;

                    for (var effect = GetFirstEffect(target); GetIsEffectValid(effect) && removed < toRemove; effect = GetNextEffect(target))
                    {
                        var effectType = GetEffectType(effect);
                        foreach (var breachable in breachableTypes)
                        {
                            if (effectType == breachable)
                            {
                                RemoveEffect(target, effect);
                                removed++;
                                break;
                            }
                        }
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Breach), target);
                    Messaging.SendMessageNearbyToPlayers(activator,
                        removed > 0
                            ? $"{GetName(activator)} breaches {GetName(target)}'s defenses! ({removed} effects stripped)"
                            : $"{GetName(activator)} breaches {GetName(target)}, but finds nothing to strip.");

                    CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);
                    Enmity.ModifyEnmity(activator, target, 100 + removed * 50);
                });
        }

        // Affliction: a creeping sickness that gnaws for as long as the will behind it holds.
        private static void ForceAffliction(AbilityBuilder builder)
        {
            var costs = new[] { 3, 5, 7 };

            builder.Create(FeatType.ForceAffliction, PerkType.ForceAffliction)
                .Name("Affliction")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceAffliction, 24f)
                .HasActivationDelay(1f)
                .HasMaxRange(15f)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation(RequireFP(costs))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ConsumeFP(activator, costs, level);

                    var duration = 12f + level * 6f;
                    StatusEffect.Apply(activator, target, StatusEffectType.ForceAffliction, duration, level);

                    // At its peak, the sickness also drags the body down (soft CC only).
                    if (level >= 3)
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectSlow(), target, 6f);
                    }

                    CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);
                    Enmity.ModifyEnmity(activator, target, 150);
                });
        }

        // Force Choke: the grip. Damage and a strangled stagger - never a hard stun
        // (the CC doctrine holds: no-break stuns are executions under perma-death).
        private static void ForceChoke(AbilityBuilder builder)
        {
            var costs = new[] { 6, 8, 10 };

            builder.Create(FeatType.ForceChoke, PerkType.ForceChoke)
                .Name("Force Choke")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceChoke, 30f)
                .HasActivationDelay(1f)
                .HasMaxRange(10f)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation(RequireFP(costs))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    ConsumeFP(activator, costs, level);

                    var willpower = GetAbilityModifier(AbilityType.Willpower, activator);
                    if (willpower < 0)
                        willpower = 0;

                    var dmg = level switch
                    {
                        1 => 10 + willpower * 2,
                        2 => 20 + willpower * 3,
                        _ => 30 + willpower * 4
                    };
                    dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Force);

                    var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
                    var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
                    var defense = Stat.GetDefense(target, CombatDamageType.Force, AbilityType.Vitality);
                    var vitality = GetAbilityModifier(AbilityType.Vitality, target);
                    var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, vitality, 0);

                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Negative), target);
                    ApplyEffectToObject(DurationType.Temporary, EffectSlow(), target, 4f + level);
                    ApplyEffectToObject(DurationType.Temporary, EffectAccuracyDecrease(5 * level), target, 4f + level);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Reduce_Ability_Score), target);

                    AssignCommand(activator, () => ActionPlayAnimation(Animation.LoopingConjure1, 1f, 1.5f));

                    CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);
                    Enmity.ModifyEnmity(activator, target, 150 + damage);
                });
        }
    }
}
