using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    /// <summary>
    /// The seven form signature actives - the level-6 capstones of the lightsaber forms,
    /// unlocked by holocrons. Each requires its form to be the ACTIVE stance, and all
    /// signatures (forms and doctrines alike) share one recast group, so switching stances
    /// mid-fight cannot weave multiple capstones.
    /// </summary>
    public class FormSignatureAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            SarlaccSweep(builder);
            DuelistsEnd(builder);
            CircleOfShelter(builder);
            HawkBatSwoop(builder);
            FallingAvalanche(builder);
            NimanBalance(builder);
            Vaapad(builder);

            // The L4/L5 signatures - two per form, completing each line's trio.
            DisarmingSlash(builder);
            Determination(builder);
            Contention(builder);
            MakashiRiposte(builder);
            BlasterReflection(builder);
            TheResilience(builder);
            SaberBarrier(builder);
            Whirlwind(builder);
            Counterforce(builder);
            Dominance(builder);
            ForceSynergy(builder);
            DrawCloser(builder);
            Ferocity(builder);
            VornskrsFury(builder);

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
            int fpCost,
            bool hostile,
            AbilityImpactAction impact)
        {
            var ability = builder.Create(feat, stance)
                .Name(abilityName)
                .Level(level)
                .HasRecastDelay(RecastGroup.StanceSignature, recast)
                .HasActivationDelay(0.5f)
                .RequirementFP(fpCost)
                .IsCastedAbility()
                .HasCustomValidation(RequireActiveStance(stance, stanceName))
                .HasImpactAction(impact);

            if (hostile)
            {
                ability.IsHostileAbility().BreaksStealth();
            }
        }

        // Shii-Cho L4: a cut across the wrists - the grip falters.
        private static void DisarmingSlash(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.DisarmingSlash, PerkType.FormShiiCho, "Form I: Shii-Cho",
                "Disarming Slash", 4, 30f, 6, true,
                (activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 25, AbilityType.Might);
                    ApplyEffectToObject(DurationType.Temporary, EffectAccuracyDecrease(10), target, 6f);
                });
        }

        // Shii-Cho L5: the training form's stubbornness - shake off what slows you.
        private static void Determination(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.Determination, PerkType.FormShiiCho, "Form I: Shii-Cho",
                "Determination", 5, 45f, 6, false,
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

                    ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(20), activator, 12f);
                    FloatingTextStringOnCreature("Nothing holds you. You press on.", activator, false);
                });
        }

        // Makashi L4: a probing exchange that sharpens your line.
        private static void Contention(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.Contention, PerkType.FormMakashi, "Form II: Makashi",
                "Contention", 4, 30f, 6, true,
                (activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 35, AbilityType.Might);
                    ApplyEffectToObject(DurationType.Temporary, EffectAccuracyIncrease(5), activator, 6f);
                });
        }

        // Makashi L5: turn their blade and answer.
        private static void MakashiRiposte(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.MakashiRiposte, PerkType.FormMakashi, "Form II: Makashi",
                "Riposte", 5, 30f, 7, true,
                (activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 30, AbilityType.Might);
                    ApplyEffectToObject(DurationType.Temporary, EffectAccuracyDecrease(10), target, 6f);
                });
        }

        // Soresu L4: the blade becomes a wall of light against blaster fire.
        private static void BlasterReflection(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.BlasterReflection, PerkType.FormSoresu, "Form III: Soresu",
                "Blaster Reflection", 4, 45f, 6, false,
                (activator, target, level, targetLocation) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectConcealment(20, MissChanceType.Ranged), activator, 12f);
                    FloatingTextStringOnCreature("Your guard becomes a wall of light.", activator, false);
                });
        }

        // Soresu L5: outlast everything.
        private static void TheResilience(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.TheResilience, PerkType.FormSoresu, "Form III: Soresu",
                "The Resilience", 5, 45f, 7, false,
                (activator, target, level, targetLocation) =>
                {
                    var willpower = GetAbilityModifier(AbilityType.Willpower, activator);
                    if (willpower < 0)
                        willpower = 0;

                    ApplyEffectToObject(DurationType.Instant, EffectHeal(20 + willpower * 3), activator);

                    // Shrug off one suppressing effect.
                    for (var effect = GetFirstEffect(activator); GetIsEffectValid(effect); effect = GetNextEffect(activator))
                    {
                        var type = GetEffectType(effect);
                        if (type == EffectTypeScript.AttackDecrease ||
                            type == EffectTypeScript.Slow)
                        {
                            RemoveEffect(activator, effect);
                            break;
                        }
                    }
                });
        }

        // Ataru L4: a spinning screen of plasma.
        private static void SaberBarrier(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.SaberBarrier, PerkType.FormAtaru, "Form IV: Ataru",
                "Saber Barrier", 4, 45f, 6, false,
                (activator, target, level, targetLocation) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectConcealment(15), activator, 6f);
                    FloatingTextStringOnCreature("Your blade blurs into a spinning screen.", activator, false);
                });
        }

        // Ataru L5: the whole form in one rotation.
        private static void Whirlwind(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.Whirlwind, PerkType.FormAtaru, "Form IV: Ataru",
                "Whirlwind", 5, 30f, 7, true,
                (activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 25, AbilityType.Agility);

                    var location = GetLocation(activator);
                    var creature = GetFirstObjectInShape(Shape.Sphere, 4.0f, location, true, ObjectType.Creature);
                    while (GetIsObjectValid(creature))
                    {
                        if (creature != activator && creature != target &&
                            GetIsReactionTypeHostile(creature, activator))
                        {
                            DealDamage(activator, creature, 25, AbilityType.Agility);
                        }

                        creature = GetNextObjectInShape(Shape.Sphere, 4.0f, location, true, ObjectType.Creature);
                    }
                });
        }

        // Djem So L4: absorb the blow, return it with interest.
        private static void Counterforce(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.Counterforce, PerkType.FormDjemSo, "Form V: Djem So",
                "Counterforce", 4, 30f, 6, true,
                (activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 30, AbilityType.Might);
                    ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(10), activator, 8f);
                });
        }

        // Djem So L5: they yield ground or they yield everything.
        private static void Dominance(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.Dominance, PerkType.FormDjemSo, "Form V: Djem So",
                "Dominance", 5, 45f, 8, true,
                (activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 40, AbilityType.Might);
                    ApplyEffectToObject(DurationType.Temporary, EffectAccuracyDecrease(10), target, 8f);
                    ApplyEffectToObject(DurationType.Temporary, EffectACDecrease(2), target, 8f);
                });
        }

        // Niman L4: the weave - saber in one hand, the Force free in the other.
        private static void ForceSynergy(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.ForceSynergy, PerkType.FormNiman, "Form VI: Niman",
                "Force Synergy", 4, 60f, 4, false,
                (activator, target, level, targetLocation) =>
                {
                    StatusEffect.Apply(activator, activator, StatusEffectType.ForceAttunement, 6f);
                    FloatingTextStringOnCreature("The Force flows freely: your next powers cost nothing for 6 seconds.", activator, false);
                });
        }

        // Niman L5: the Force closes the distance for you.
        private static void DrawCloser(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.DrawCloser, PerkType.FormNiman, "Form VI: Niman",
                "Draw Closer", 5, 30f, 7, true,
                (activator, target, level, targetLocation) =>
                {
                    var damage = RollDamage(activator, target, 15, AbilityType.Willpower);
                    var pullTo = GetLocation(activator);

                    AssignCommand(target, () => ActionJumpToLocation(pullTo));
                    DelayCommand(0.5f, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
                        ApplyEffectToObject(DurationType.Temporary, EffectSlow(), target, 2f);
                    });

                    Enmity.ModifyEnmity(activator, target, 150 + damage);
                });
        }

        // Juyo L4: burn hotter.
        private static void Ferocity(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.Ferocity, PerkType.FormJuyo, "Form VII: Juyo",
                "Ferocity", 4, 45f, 6, false,
                (activator, target, level, targetLocation) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectAccuracyIncrease(10), activator, 12f);
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(5), activator);
                    FloatingTextStringOnCreature("The fire takes you.", activator, false);
                });
        }

        // Juyo L5: the dark habit - what you tear away feeds you.
        private static void VornskrsFury(AbilityBuilder builder)
        {
            BuildSignatureShell(builder, FeatType.VornskrsFury, PerkType.FormJuyo, "Form VII: Juyo",
                "Vornskr's Fury", 5, 30f, 7, true,
                (activator, target, level, targetLocation) =>
                {
                    var damage = RollDamage(activator, target, 35, AbilityType.Might);
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(damage / 2), activator);
                    Enmity.ModifyEnmity(activator, target, 150 + damage);
                });
        }

        private static AbilityCustomValidationAction RequireActiveStance(PerkType stance, string formName)
        {
            return (activator, target, level, targetLocation) =>
                Stance.GetActiveStanceType(activator) == stance
                    ? string.Empty
                    : $"You must be in {formName} to use this technique.";
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

        // Form I capstone: one wide cut through everything in reach.
        private static void SarlaccSweep(AbilityBuilder builder)
        {
            builder.Create(FeatType.SarlaccSweep, PerkType.FormShiiCho)
                .Name("Sarlacc Sweep")
                .Level(6)
                .HasRecastDelay(RecastGroup.StanceSignature, 30f)
                .HasActivationDelay(0.5f)
                .RequirementFP(6)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation(RequireActiveStance(PerkType.FormShiiCho, "Form I: Shii-Cho"))
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

        // Form II capstone: the duel-ending thrust. Strongest against a lone opponent.
        private static void DuelistsEnd(AbilityBuilder builder)
        {
            builder.Create(FeatType.DuelistsEnd, PerkType.FormMakashi)
                .Name("Duelist's End")
                .Level(6)
                .HasRecastDelay(RecastGroup.StanceSignature, 30f)
                .HasActivationDelay(0.5f)
                .RequirementFP(6)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation(RequireActiveStance(PerkType.FormMakashi, "Form II: Makashi"))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var damage = RollDamage(activator, target, 40, AbilityType.Might);

                    // In single combat - no other enemy near the target - the thrust lands half again as hard.
                    if (!HasOtherNearbyEnemy(activator, target))
                        damage += damage / 2;

                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                    Enmity.ModifyEnmity(activator, target, 150 + damage);
                });
        }

        private static bool HasOtherNearbyEnemy(uint activator, uint target)
        {
            var location = GetLocation(target);
            var creature = GetFirstObjectInShape(Shape.Sphere, 5.0f, location, true, ObjectType.Creature);
            while (GetIsObjectValid(creature))
            {
                if (creature != target && creature != activator &&
                    GetIsReactionTypeHostile(creature, activator))
                {
                    return true;
                }

                creature = GetNextObjectInShape(Shape.Sphere, 5.0f, location, true, ObjectType.Creature);
            }

            return false;
        }

        // Form III capstone: a guard so complete that blows simply fail to land.
        private static void CircleOfShelter(AbilityBuilder builder)
        {
            builder.Create(FeatType.CircleOfShelter, PerkType.FormSoresu)
                .Name("Circle of Shelter")
                .Level(6)
                .HasRecastDelay(RecastGroup.StanceSignature, 60f)
                .HasActivationDelay(0.5f)
                .RequirementFP(6)
                .IsCastedAbility()
                .HasCustomValidation(RequireActiveStance(PerkType.FormSoresu, "Form III: Soresu"))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var amount = 40 + GetAbilityModifier(AbilityType.Willpower, activator) * 4;
                    ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(amount), activator, 30f);
                    FloatingTextStringOnCreature("You settle behind an impenetrable guard.", activator, false);
                });
        }

        // Form IV capstone: close the gap in a single leap and strike on landing.
        private static void HawkBatSwoop(AbilityBuilder builder)
        {
            builder.Create(FeatType.HawkBatSwoop, PerkType.FormAtaru)
                .Name("Hawk-Bat Swoop")
                .Level(6)
                .HasRecastDelay(RecastGroup.StanceSignature, 30f)
                .HasActivationDelay(0.5f)
                .HasMaxRange(15f)
                .RequirementFP(6)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation(RequireActiveStance(PerkType.FormAtaru, "Form IV: Ataru"))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var damage = RollDamage(activator, target, 30, AbilityType.Agility);

                    AssignCommand(activator, () =>
                    {
                        PlaySound("plr_force_flip");
                        ActionJumpToObject(target);
                    });
                    DelayCommand(0.8f, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Slashing), target);
                    });

                    Enmity.ModifyEnmity(activator, target, 150 + damage);
                });
        }

        // Form V capstone: a blow that brings the opponent to the ground.
        private static void FallingAvalanche(AbilityBuilder builder)
        {
            builder.Create(FeatType.FallingAvalanche, PerkType.FormDjemSo)
                .Name("Falling Avalanche")
                .Level(6)
                .HasRecastDelay(RecastGroup.StanceSignature, 45f)
                .HasActivationDelay(0.5f)
                .RequirementFP(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation(RequireActiveStance(PerkType.FormDjemSo, "Form V: Djem So"))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var baseDMG = 55 + GetAbilityModifier(AbilityType.Might, activator);
                    DealDamage(activator, target, baseDMG, AbilityType.Might);

                    const float Duration = 2f;
                    ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), target, Duration);
                    Ability.ApplyTemporaryImmunity(target, Duration + 6f, ImmunityType.Knockdown);
                });
        }

        // Form VI capstone: a moment of stillness that refills the well.
        private static void NimanBalance(AbilityBuilder builder)
        {
            builder.Create(FeatType.NimanBalance, PerkType.FormNiman)
                .Name("Balance")
                .Level(6)
                .HasRecastDelay(RecastGroup.StanceSignature, 60f)
                .HasActivationDelay(0.5f)
                .IsCastedAbility()
                .HasCustomValidation(RequireActiveStance(PerkType.FormNiman, "Form VI: Niman"))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var amount = 12 + GetAbilityModifier(AbilityType.Willpower, activator) * 2;
                    Stat.RestoreFP(activator, amount);
                    FloatingTextStringOnCreature($"You recover {amount} FP.", activator, false);
                });
        }

        // Form VII capstone: everything behind one strike, including your own footing.
        private static void Vaapad(AbilityBuilder builder)
        {
            builder.Create(FeatType.Vaapad, PerkType.FormJuyo)
                .Name("Vaapad")
                .Level(6)
                .HasRecastDelay(RecastGroup.StanceSignature, 45f)
                .HasActivationDelay(0.5f)
                .RequirementFP(8)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasCustomValidation(RequireActiveStance(PerkType.FormJuyo, "Form VII: Juyo"))
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    DealDamage(activator, target, 60, AbilityType.Might);

                    // The form's price: a tenth of your own remaining vitality.
                    var selfDamage = GetCurrentHitPoints(activator) / 10;
                    if (selfDamage > 0)
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(selfDamage), activator);
                    }
                });
        }
    }
}
