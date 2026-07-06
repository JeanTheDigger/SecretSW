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
    /// The SAM Battery - a turret-mounted concussion missile rack that only tracks
    /// fighters. Ordnance-family: it half-bypasses and degrades shield ratings, and its
    /// raw hit staggers fighter thresholds - the answer to emitter-refitted, PD-immune
    /// fighters. Flak beats armor builds, missiles beat emitter builds; mounting both
    /// takes gunner seats.
    /// </summary>
    public class SAMBatteryModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            SAMBattery("sam_battery_1", "SAM Battery", "SAM Battery", "Turret-mounted concussion rack: deals 35 explosive DMG to a FIGHTER-class target, half-bypassing its shield rating and rattling its frame. Cannot target larger hulls.", 4, 35, 5);

            return _builder.Build();
        }

        private void SAMBattery(
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
                .Type(ShipModuleType.Missile)
                .Texture("iit_ess_094")
                .Description(description)
                .MaxDistance(35f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(12f)
                .Capacitor(capacitor)
                .ValidationAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    if (targetShipStatus == null)
                        return "Invalid target.";

                    return Space.GetFrameClass(targetShipStatus) != ShipFrameClass.Fighter
                        ? "The SAM battery only locks onto fighter-class targets."
                        : string.Empty;
                })
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var attackBonus = activatorShipStatus.ExplosiveDamage;
                    var attackerStat = Space.GetAttackStat(activator);
                    var attack = Space.GetShipAttack(activator, attackBonus);
                    var defenseBonus = targetShipStatus.ExplosiveDefense * 2;
                    var defense = Space.GetShipDefense(target, defenseBonus);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var moduleDamage = dmg + moduleBonus / 2 + Space.GetStrikeOrdnanceBonus(activator);
                    var damage = Combat.CalculateDamage(
                        attack,
                        moduleDamage,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var chanceToHit = Space.CalculateChanceToHit(activator, target);
                    var roll = Random.D100(1);
                    var isHit = roll <= chanceToHit;

                    AssignCommand(activator, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Trp), activator);
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Mirv_Torpedo, !isHit), target);
                    });

                    if (isHit)
                    {
                        DelayCommand(0.5f, () =>
                        {
                            Space.ApplyShipDamage(activator, target, damage, ShipDamageFamily.Ordnance);
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
