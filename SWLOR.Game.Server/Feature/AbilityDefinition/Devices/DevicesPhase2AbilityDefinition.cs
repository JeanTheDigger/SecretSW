using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    /// <summary>
    /// The Devices Phase-2 tech: Carbonite Projector (the marquee tech hold, priced under
    /// the CC rules - short, immunity-windowed, on a long cycle), Combat Jetpack (mobility
    /// as equipment, the Standard cousin of Force Leap's freedom), and Orbital Strike
    /// (mark a position, and six seconds later the sky answers).
    /// </summary>
    public class DevicesPhase2AbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            CarboniteProjector(builder);
            CombatJetpack(builder);
            OrbitalStrike(builder);

            return builder.Build();
        }

        private static void CarboniteProjector(AbilityBuilder builder)
        {
            builder.Create(FeatType.CarboniteProjector, PerkType.CarboniteProjector)
                .Name("Carbonite Projector")
                .Level(1)
                .HasRecastDelay(RecastGroup.CarboniteProjector, 60f)
                .HasActivationDelay(1f)
                .HasMaxRange(10f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    // Short and immunity-windowed: the CC doctrine survives even the
                    // marquee tech hold. Level 2 lengthens the freeze to 3 seconds.
                    var duration = level >= 2 ? 3f : 2f;

                    ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, duration);
                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Iceskin), target, duration);
                    Ability.ApplyTemporaryImmunity(target, duration + 12f, ImmunityType.Paralysis);

                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} flash-freezes {GetName(target)} in carbonite!");

                    CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);
                    Enmity.ModifyEnmity(activator, target, 200);
                });
        }

        private static void CombatJetpack(AbilityBuilder builder)
        {
            builder.Create(FeatType.CombatJetpack, PerkType.CombatJetpack)
                .Name("Combat Jetpack")
                .Level(1)
                .HasRecastDelay(RecastGroup.CombatJetpack, 30f)
                .HasActivationDelay(0.5f)
                .HasMaxRange(20f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .BreaksStealth()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var destination = GetIsObjectValid(target) ? GetLocation(target) : targetLocation;

                    AssignCommand(activator, () =>
                    {
                        PlaySound("plr_force_flip");
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Mirv_Flame), activator);
                        ActionJumpToLocation(destination);
                    });

                    // Level 2: the burn carries you onward - a burst of speed on landing.
                    if (level >= 2)
                    {
                        DelayCommand(0.8f, () =>
                        {
                            ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedIncrease(25), activator, 6f);
                        });
                    }
                });
        }

        private static void OrbitalStrike(AbilityBuilder builder)
        {
            builder.Create(FeatType.OrbitalStrike, PerkType.OrbitalStrike)
                .Name("Orbital Strike")
                .Level(1)
                .HasRecastDelay(RecastGroup.OrbitalStrike, 300f)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.LoopingTalkForceful)
                .HasMaxRange(25f)
                .RequirementStamina(10)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var strikeLocation = GetIsObjectValid(target) ? GetLocation(target) : targetLocation;

                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} paints a target for orbital bombardment!");
                    ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Dispel), strikeLocation);

                    // Six seconds of red light on the ground: the counterplay is leaving.
                    DelayCommand(6f, () =>
                    {
                        ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), strikeLocation);
                        ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake), strikeLocation);

                        var perception = GetAbilityModifier(AbilityType.Perception, activator);
                        if (perception < 0)
                            perception = 0;

                        var creature = GetFirstObjectInShape(Shape.Sphere, RadiusSize.Large, strikeLocation, true, ObjectType.Creature);
                        while (GetIsObjectValid(creature))
                        {
                            // Friendly-fire areas sweep allies into the blast too; open world stays enemy-only.
                            if (creature != activator && (Ability.IsFriendlyFireArea(activator) || GetIsReactionTypeHostile(creature, activator)))
                            {
                                var dmg = 60 + perception * 4 + Combat.GetAbilityDamageBonus(activator, SkillType.Devices);
                                var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
                                var attack = Stat.GetAttack(activator, AbilityType.Perception, SkillType.Devices);
                                var defense = Stat.GetDefense(creature, CombatDamageType.Fire, AbilityType.Vitality);
                                var vitality = GetAbilityModifier(AbilityType.Vitality, creature);
                                var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, vitality, 0);

                                var damageTarget = creature;
                                DelayCommand(0.1f, () =>
                                {
                                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), damageTarget);
                                });

                                // Enmity / combat points only for genuine enemies — no ally retaliation or farming.
                                if (GetIsReactionTypeHostile(creature, activator))
                                {
                                    CombatPoint.AddCombatPoint(activator, creature, SkillType.Devices, 3);
                                    Enmity.ModifyEnmity(activator, creature, 150 + damage);
                                }
                            }

                            creature = GetNextObjectInShape(Shape.Sphere, RadiusSize.Large, strikeLocation, true, ObjectType.Creature);
                        }
                    });
                });
        }
    }
}
