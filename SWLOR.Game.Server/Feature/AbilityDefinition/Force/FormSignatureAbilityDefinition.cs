using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
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

            return builder.Build();
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
