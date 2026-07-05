using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    /// <summary>
    /// The Point-Defense Cluster: low damage, high tracking, exists to hit FIGHTERS -
    /// the anti-fighter screen main batteries cannot be. Its degradation cascade kills
    /// loiterers in under a minute, but a committed 15-second torpedo run survives it:
    /// trench-run timing, enforced. Flak beats armor builds; missiles beat emitter
    /// builds (see the SAM battery) - anti-fighter defense is its own rock-paper.
    /// </summary>
    public class PointDefenseClusterModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            PointDefenseCluster("pd_cluster_1", "Point-Defense Cluster I", "PD Cluster I", "Rapid-cycling flak turret: deals 12 thermal DMG to a FIGHTER-class target with +10% tracking. Cannot target larger hulls.", 2, 12, 2);
            PointDefenseCluster("pd_cluster_2", "Point-Defense Cluster II", "PD Cluster II", "Rapid-cycling flak turret: deals 18 thermal DMG to a FIGHTER-class target with +10% tracking. Cannot target larger hulls.", 3, 18, 3);
            PointDefenseCluster("pd_cluster_3", "Point-Defense Cluster III", "PD Cluster III", "Rapid-cycling flak turret: deals 24 thermal DMG to a FIGHTER-class target with +10% tracking. Cannot target larger hulls.", 4, 24, 4);

            return _builder.Build();
        }

        private void PointDefenseCluster(
            string itemTag,
            string name,
            string shortName,
            string description,
            int requiredLevel,
            int dmg,
            int capacitor)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.LaserBattery)
                .Texture("iit_ess_004")
                .Description(description)
                .MaxDistance(25f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(3f)
                .Capacitor(capacitor)
                .ValidationAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    if (targetShipStatus == null)
                        return "Invalid target.";

                    return Space.GetFrameClass(targetShipStatus) != ShipFrameClass.Fighter
                        ? "The point-defense cluster only tracks fighter-class targets."
                        : string.Empty;
                })
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var attackBonus = activatorShipStatus.ThermalDamage;
                    var attackerStat = Space.GetAttackStat(activator);
                    var attack = Space.GetShipAttack(activator, attackBonus);
                    var defenseBonus = targetShipStatus.ThermalDefense * 2;
                    var defense = Space.GetShipDefense(target, defenseBonus);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var moduleDamage = dmg + moduleBonus / 3;
                    var damage = Combat.CalculateDamage(
                        attack,
                        moduleDamage,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    // High tracking: +10 percentage points against the fighters it exists for.
                    var chanceToHit = Math.Clamp(Space.CalculateChanceToHit(activator, target) + 10, 20, 95);
                    var roll = Random.D100(1);
                    var isHit = roll <= chanceToHit;

                    AssignCommand(activator, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Blast), target);
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2), target);
                    });

                    if (isHit)
                    {
                        DelayCommand(0.3f, () =>
                        {
                            Space.ApplyShipDamage(activator, target, damage);
                        });
                    }

                    var attackId = isHit ? 1 : 4;
                    var combatLogMessage = Combat.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                    Messaging.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);

                    Enmity.ModifyEnmity(activator, target, damage);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);
                });
        }
    }
}
