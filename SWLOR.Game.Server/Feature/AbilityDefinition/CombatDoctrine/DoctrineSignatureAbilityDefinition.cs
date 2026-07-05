using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.CombatDoctrine
{
    /// <summary>
    /// The five doctrine signature actives - the level-6 capstones of the Standard combat
    /// doctrines, unlocked by combat datacrons. Each requires its doctrine to be the ACTIVE
    /// stance, and all signatures (forms and doctrines alike) share one recast group, so
    /// switching stances mid-fight cannot weave multiple capstones.
    /// </summary>
    public class DoctrineSignatureAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            Riposte(builder);
            StaggeringAdvance(builder);
            TwinCyclone(builder);
            ForceLock(builder);
            ExecutionShot(builder);

            return builder.Build();
        }

        private static AbilityCustomValidationAction RequireActiveStance(PerkType stance, string doctrineName)
        {
            return (activator, target, level, targetLocation) =>
                Stance.GetActiveStanceType(activator) == stance
                    ? string.Empty
                    : $"You must be in {doctrineName} to use this technique.";
        }

        private static SkillType ResolveWeaponSkill(uint activator)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
            var skill = Skill.GetSkillTypeByBaseItem(GetBaseItemType(weapon));

            return skill == SkillType.Invalid ? SkillType.MartialArts : skill;
        }

        private static int RollDamage(uint activator, uint target, int baseDMG, AbilityType attackerAbility)
        {
            var skill = ResolveWeaponSkill(activator);
            var dmg = baseDMG +
                      Combat.GetAbilityDamageBonus(activator, skill) +
                      GetAbilityModifier(attackerAbility, activator);

            CombatPoint.AddCombatPoint(activator, target, skill, 3);

            var attackerStat = GetAbilityScore(activator, attackerAbility);
            var attack = Stat.GetAttack(activator, attackerAbility, skill);
            var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
            var vitality = GetAbilityModifier(AbilityType.Vitality, target);

            return Combat.CalculateDamage(attack, dmg, attackerStat, defense, vitality, 0);
        }

        private static void DealDamage(uint activator, uint target, int baseDMG, AbilityType attackerAbility)
        {
            var damage = RollDamage(activator, target, baseDMG, attackerAbility);
            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
            Enmity.ModifyEnmity(activator, target, 150 + damage);
        }

        // Duelist capstone: turn their blade aside and answer - the opponent's aim suffers.
        private static void Riposte(AbilityBuilder builder)
        {
            builder.Create(FeatType.Riposte, PerkType.DoctrineDuelist)
                .Name("Riposte")
                .Level(6)
                .HasRecastDelay(RecastGroup.StanceSignature, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation(RequireActiveStance(PerkType.DoctrineDuelist, "Duelist Doctrine"))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 40, AbilityType.Might);
                    ApplyEffectToObject(DurationType.Temporary, EffectAccuracyDecrease(10), target, 6f);
                });
        }

        // Juggernaut capstone: a blow that brings the opponent to the ground (Falling Avalanche's mirror).
        private static void StaggeringAdvance(AbilityBuilder builder)
        {
            builder.Create(FeatType.StaggeringAdvance, PerkType.DoctrineJuggernaut)
                .Name("Staggering Advance")
                .Level(6)
                .HasRecastDelay(RecastGroup.StanceSignature, 45f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation(RequireActiveStance(PerkType.DoctrineJuggernaut, "Juggernaut"))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var baseDMG = 55 + GetAbilityModifier(AbilityType.Might, activator);
                    DealDamage(activator, target, baseDMG, AbilityType.Might);

                    const float Duration = 2f;
                    ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, Duration);
                    Ability.ApplyTemporaryImmunity(target, Duration + 6f, ImmunityType.Knockdown);
                });
        }

        // Tempest capstone: both blades through everything in reach (Sarlacc Sweep's mirror).
        private static void TwinCyclone(AbilityBuilder builder)
        {
            builder.Create(FeatType.TwinCyclone, PerkType.DoctrineTempest)
                .Name("Twin Cyclone")
                .Level(6)
                .HasRecastDelay(RecastGroup.StanceSignature, 30f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation(RequireActiveStance(PerkType.DoctrineTempest, "Tempest"))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 30, AbilityType.Might);

                    var location = GetLocation(activator);
                    var creature = GetFirstObjectInShape(Shape.Sphere, 4.0f, location, true, ObjectType.Creature);
                    while (GetIsObjectValid(creature))
                    {
                        if (creature != activator && creature != target &&
                            GetIsReactionTypeHostile(creature, activator))
                        {
                            DealDamage(activator, creature, 30, AbilityType.Might);
                        }

                        creature = GetNextObjectInShape(Shape.Sphere, 4.0f, location, true, ObjectType.Creature);
                    }
                });
        }

        // Teräs Käsi capstone: a strike to the meridians that severs the target's connection
        // to the Force. Deliberately short, on the shared long recast - it cannot be chained.
        private static void ForceLock(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceLock, PerkType.DoctrineTerasKasi)
                .Name("Force Lock")
                .Level(6)
                .HasRecastDelay(RecastGroup.StanceSignature, 60f)
                .HasActivationDelay(0.5f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation(RequireActiveStance(PerkType.DoctrineTerasKasi, "Teräs Käsi"))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 25, AbilityType.Might);
                    StatusEffect.Apply(activator, target, StatusEffectType.ForceLock, 4f);
                });
        }

        // Marksman capstone: the shot that ends it. Far deadlier against a faltering target,
        // but a sleeping one cannot be lined up for the kill (no Tranquilizer combo).
        private static void ExecutionShot(AbilityBuilder builder)
        {
            builder.Create(FeatType.ExecutionShot, PerkType.DoctrineMarksman)
                .Name("Execution Shot")
                .Level(6)
                .HasRecastDelay(RecastGroup.StanceSignature, 45f)
                .HasActivationDelay(0.5f)
                .HasMaxRange(30f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation(RequireActiveStance(PerkType.DoctrineMarksman, "Marksman Doctrine"))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var damage = RollDamage(activator, target, 35, AbilityType.Perception);

                    if (GetCurrentHitPoints(target) < GetMaxHitPoints(target) * 3 / 10 &&
                        !IsAsleep(target))
                    {
                        damage *= 2;
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                    Enmity.ModifyEnmity(activator, target, 150 + damage);
                });
        }

        private static bool IsAsleep(uint creature)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectType(effect) == EffectTypeScript.Sleep)
                    return true;
            }

            return false;
        }
    }
}
