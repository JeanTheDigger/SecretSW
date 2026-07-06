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

            // The L4/L5 signatures - two per doctrine, completing each line's trio.
            MeasuredCut(builder);
            PerfectParry(builder);
            BreachingAdvance(builder);
            Unstoppable(builder);
            TwinFeint(builder);
            Bladestorm(builder);
            MeridianStrike(builder);
            IronBody(builder);
            CalledShotLegs(builder);
            PenetratingRound(builder);

            return builder.Build();
        }

        private static void BuildSignatureShell(
            AbilityBuilder builder,
            FeatType feat,
            PerkType stance,
            string stanceName,
            string abilityName,
            int level,
            float recast,
            int stmCost,
            bool hostile,
            AbilityImpactAction impact)
        {
            var ability = builder.Create(feat, stance)
                .Name(abilityName)
                .Level(level)
                .HasRecastDelay(RecastGroup.StanceSignature, recast)
                .HasActivationDelay(0.5f)
                .RequirementStamina(stmCost)
                .IsCastedAbility()
                .HasCustomValidation(RequireActiveStance(stance, stanceName))
                .HasImpactAction(impact);

            if (hostile)
            {
                ability.IsHostileAbility().BreaksStealth();
            }
        }

        // Duelist L4: precision that compounds.
        private static void MeasuredCut(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.MeasuredCut, PerkType.DoctrineDuelist, "Duelist Doctrine",
                "Measured Cut", 4, 30f, 6, true,
                (activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 30, AbilityType.Might);
                    ApplyEffectToObject(DurationType.Temporary, EffectAccuracyIncrease(5), activator, 6f);
                });
        }

        // Duelist L5: eight seconds where nothing gets through.
        private static void PerfectParry(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.PerfectParry, PerkType.DoctrineDuelist, "Duelist Doctrine",
                "Perfect Parry", 5, 45f, 7, false,
                (activator, target, level, targetLocation) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectConcealment(20, MissChanceType.Melee), activator, 8f);
                    FloatingTextStringOnCreature("Your blade finds every angle before their eyes do.", activator, false);
                });
        }

        // Juggernaut L4: hit them where the armor gives.
        private static void BreachingAdvance(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.BreachingAdvance, PerkType.DoctrineJuggernaut, "Juggernaut",
                "Breaching Advance", 4, 30f, 6, true,
                (activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 30, AbilityType.Might);
                    ApplyEffectToObject(DurationType.Temporary, EffectACDecrease(3), target, 8f);
                });
        }

        // Juggernaut L5: nothing stops the advance.
        private static void Unstoppable(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.Unstoppable, PerkType.DoctrineJuggernaut, "Juggernaut",
                "Unstoppable", 5, 45f, 7, false,
                (activator, target, level, targetLocation) =>
                {
                    for (var effect = GetFirstEffect(activator); GetIsEffectValid(effect); effect = GetNextEffect(activator))
                    {
                        var type = GetEffectType(effect);
                        if (type == EffectTypeScript.Slow || type == EffectTypeScript.MovementSpeedDecrease ||
                            type == EffectTypeScript.CutsceneImmobilize || type == EffectTypeScript.Entangle)
                        {
                            RemoveEffect(activator, effect);
                        }
                    }

                    const float Duration = 8f;
                    Ability.ApplyTemporaryImmunity(activator, Duration, ImmunityType.Knockdown);
                    ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(10), activator, Duration);
                    FloatingTextStringOnCreature("You cannot be stopped.", activator, false);
                });
        }

        // Tempest L4: the second blade was always the real one.
        private static void TwinFeint(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.TwinFeint, PerkType.DoctrineTempest, "Tempest",
                "Twin Feint", 4, 30f, 6, true,
                (activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 30, AbilityType.Might);
                    ApplyEffectToObject(DurationType.Temporary, EffectConcealment(10), activator, 6f);
                });
        }

        // Tempest L5: both blades through everyone (the Whirlwind mirror).
        private static void Bladestorm(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.Bladestorm, PerkType.DoctrineTempest, "Tempest",
                "Bladestorm", 5, 30f, 7, true,
                (activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 25, AbilityType.Might);

                    var location = GetLocation(activator);
                    var creature = GetFirstObjectInShape(Shape.Sphere, 4.0f, location, true, ObjectType.Creature);
                    while (GetIsObjectValid(creature))
                    {
                        if (creature != activator && creature != target &&
                            GetIsReactionTypeHostile(creature, activator))
                        {
                            DealDamage(activator, creature, 25, AbilityType.Might);
                        }

                        creature = GetNextObjectInShape(Shape.Sphere, 4.0f, location, true, ObjectType.Creature);
                    }
                });
        }

        // Teräs Käsi L4: a strike to the channels the Force flows through.
        private static void MeridianStrike(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.MeridianStrike, PerkType.DoctrineTerasKasi, "Teräs Käsi",
                "Meridian Strike", 4, 30f, 6, true,
                (activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 25, AbilityType.Might);
                    Stat.ReduceFP(target, 8);
                    FloatingTextStringOnCreature("Your strike scatters their focus.", activator, false);
                });
        }

        // Teräs Käsi L5: the body becomes the temple wall.
        private static void IronBody(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.IronBody, PerkType.DoctrineTerasKasi, "Teräs Käsi",
                "Iron Body", 5, 45f, 7, false,
                (activator, target, level, targetLocation) =>
                {
                    ApplyEffectToObject(DurationType.Temporary,
                        EffectSavingThrowIncrease((int)SavingThrow.All, 4), activator, 12f);
                    ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(15), activator, 12f);
                    FloatingTextStringOnCreature("Your body is stone. Your mind is stiller.", activator, false);
                });
        }

        // Marksman L4: shoot the legs - nobody outruns a bullet's argument.
        private static void CalledShotLegs(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.CalledShotLegs, PerkType.DoctrineMarksman, "Marksman Doctrine",
                "Called Shot: Legs", 4, 30f, 6, true,
                (activator, target, level, targetLocation) =>
                {
                    var damage = RollDamage(activator, target, 25, AbilityType.Perception);
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                    ApplyEffectToObject(DurationType.Temporary, EffectSlow(), target, 4f);
                    Enmity.ModifyEnmity(activator, target, 150 + damage);
                });
        }

        // Marksman L5: the round that does not care what they are wearing.
        private static void PenetratingRound(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.PenetratingRound, PerkType.DoctrineMarksman, "Marksman Doctrine",
                "Penetrating Round", 5, 45f, 7, true,
                (activator, target, level, targetLocation) =>
                {
                    var skill = ResolveWeaponSkill(activator);
                    var dmg = 35 +
                              Combat.GetAbilityDamageBonus(activator, skill) +
                              GetAbilityModifier(AbilityType.Perception, activator);

                    CombatPoint.AddCombatPoint(activator, target, skill, 3);

                    var attackerStat = GetAbilityScore(activator, AbilityType.Perception);
                    var attack = Stat.GetAttack(activator, AbilityType.Perception, skill);

                    // The round punches through: only HALF the target's defense counts.
                    var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality) / 2;
                    var vitality = GetAbilityModifier(AbilityType.Vitality, target);
                    var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, vitality, 0);

                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                    Enmity.ModifyEnmity(activator, target, 150 + damage);
                });
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
