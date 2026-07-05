using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Cybernetics
{
    /// <summary>
    /// The implant capstone actives, granted at each line's prototype peak (level 6):
    /// - Jump-Jet (Servo Actuators): the Standard answer to Force Leap.
    /// - Overclock (Neural Processor): every cooldown wiped, paid for with an overheat daze.
    /// Second Wind (Cardio Regulator) is a passive death intercept and lives in the
    /// Death service, not here.
    /// </summary>
    public class ImplantCapstoneAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            JumpJet(builder);
            Overclock(builder);

            return builder.Build();
        }

        private static void JumpJet(AbilityBuilder builder)
        {
            builder.Create(FeatType.JumpJet, PerkType.ImplantServo)
                .Name("Jump-Jet")
                .Level(6)
                .HasRecastDelay(RecastGroup.JumpJet, 30f)
                .HasActivationDelay(0.5f)
                .HasMaxRange(15f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var weapon = GetItemInSlot(InventorySlot.RightHand, activator);
                    var skill = Skill.GetSkillTypeByBaseItem(GetBaseItemType(weapon));
                    if (skill == SkillType.Invalid)
                        skill = SkillType.MartialArts;

                    var dmg = 20 +
                              Combat.GetAbilityDamageBonus(activator, skill) +
                              GetAbilityModifier(AbilityType.Might, activator);

                    CombatPoint.AddCombatPoint(activator, target, skill, 3);

                    var might = GetAbilityScore(activator, AbilityType.Might);
                    var attack = Stat.GetAttack(activator, AbilityType.Might, skill);
                    var defense = Stat.GetDefense(target, CombatDamageType.Physical, AbilityType.Vitality);
                    var vitality = GetAbilityModifier(AbilityType.Vitality, target);
                    var damage = Combat.CalculateDamage(attack, dmg, might, defense, vitality, 0);

                    AssignCommand(activator, () =>
                    {
                        PlaySound("plr_force_flip");
                        ActionJumpToObject(target);
                    });
                    DelayCommand(0.8f, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Bludgeoning), target);
                    });

                    Enmity.ModifyEnmity(activator, target, 150 + damage);
                });
        }

        private static void Overclock(AbilityBuilder builder)
        {
            builder.Create(FeatType.Overclock, PerkType.ImplantNeural)
                .Name("Overclock")
                .Level(6)
                .HasRecastDelay(RecastGroup.Overclock, 300f)
                .HasActivationDelay(0.5f)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var playerId = GetObjectUUID(activator);
                    var dbPlayer = DB.Get<Player>(playerId);
                    dbPlayer.RecastTimes.Clear();
                    DB.Set(dbPlayer);

                    // The overheat: two seconds of white-hot nothing.
                    const float Duration = 2f;
                    ApplyEffectToObject(DurationType.Temporary, EffectDazed(), activator, Duration);
                    Ability.ApplyTemporaryImmunity(activator, Duration + 6f, ImmunityType.Dazed);

                    FloatingTextStringOnCreature("Your neural processor OVERCLOCKS - every system cycles fresh as the implant burns hot.", activator, false);
                    // The ability system applies Overclock's own recast after this impact,
                    // so the wipe never includes itself.
                });
        }
    }
}
