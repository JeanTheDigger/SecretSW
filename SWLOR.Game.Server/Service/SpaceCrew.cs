using System;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Crew stations v1: passengers inside a flying ship's interior can man the guns and
    /// the engineering bay. FLY it = Piloting; the seats use their OWN skills - gunners
    /// fire with Ranged, engineers restore the shield rating and work the condition track
    /// with Engineering - and each seat tags its own combat points, so space kill XP
    /// routes to the skill each crew member actually used. Station terminals are module
    /// content (sweep list); until then the /turret, /shields, and /damagecontrol
    /// commands are the seats.
    /// </summary>
    public static class SpaceCrew
    {
        private const string TurretRecastVariable = "CREW_TURRET_RECAST";
        private const string ShieldsRecastVariable = "CREW_SHIELDS_RECAST";
        private const string DamageControlRecastVariable = "CREW_DC_RECAST";

        private const float TurretRecastSeconds = 6f;
        private const float ShieldsRecastSeconds = 12f;
        private const float DamageControlRecastSeconds = 18f;

        /// <summary>
        /// Resolves the flying ship a crew member is aboard: the interior area's property
        /// must be a starship, and some pilot must currently be flying it.
        /// Returns (OBJECT_INVALID, null) with a message when either fails.
        /// </summary>
        private static (uint, PlayerShip) FindShipByInterior(uint crewMember)
        {
            var area = GetArea(crewMember);
            var propertyId = Property.GetPropertyId(area);
            if (string.IsNullOrWhiteSpace(propertyId))
            {
                SendMessageToPC(crewMember, "You must be aboard a ship to man a station.");
                return (OBJECT_INVALID, null);
            }

            var dbProperty = DB.Get<WorldProperty>(propertyId);
            if (dbProperty == null || dbProperty.PropertyType != PropertyType.Starship)
            {
                SendMessageToPC(crewMember, "You must be aboard a ship to man a station.");
                return (OBJECT_INVALID, null);
            }

            var query = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyId, false);
            var dbShip = DB.Search(query).FirstOrDefault();
            if (dbShip == null)
            {
                SendMessageToPC(crewMember, "You must be aboard a ship to man a station.");
                return (OBJECT_INVALID, null);
            }

            for (var pilot = GetFirstPC(); GetIsObjectValid(pilot); pilot = GetNextPC())
            {
                if (!Space.IsPlayerInSpaceMode(pilot))
                    continue;

                var pilotId = GetObjectUUID(pilot);
                var dbPilot = DB.Get<Player>(pilotId);
                if (dbPilot.ActiveShipId == dbShip.Id)
                    return (pilot, dbShip);
            }

            SendMessageToPC(crewMember, "The ship is not underway. Stations only matter in flight.");
            return (OBJECT_INVALID, null);
        }

        private static bool IsOnRecast(uint crewMember, string variable)
        {
            var raw = GetLocalString(crewMember, variable);
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var readyAt))
                return false;

            if (DateTime.UtcNow >= readyAt)
                return false;

            var remaining = (readyAt - DateTime.UtcNow).TotalSeconds;
            SendMessageToPC(crewMember, $"That station is cycling. ({remaining:F1} seconds)");
            return true;
        }

        private static void StartRecast(uint crewMember, string variable, float seconds)
        {
            SetLocalString(crewMember, variable, DateTime.UtcNow.AddSeconds(seconds).ToString("O"));
        }

        /// <summary>
        /// Fires the ship's turret battery at the pilot's current target, using the
        /// GUNNER's stats: accuracy from Agility and Ranged, attack from Ranged and
        /// Perception. Requires a turret-class weapon (laser battery or quad laser)
        /// fitted to the ship. Gunner combat points route to Ranged.
        /// </summary>
        public static void FireTurret(uint gunner)
        {
            if (IsOnRecast(gunner, TurretRecastVariable))
                return;

            var (pilot, dbShip) = FindShipByInterior(gunner);
            if (!GetIsObjectValid(pilot))
                return;

            var hasTurret = dbShip.Status.HighPowerModules.Values.Any(module =>
            {
                var detail = Space.GetShipModuleDetailByItemTag(module.ItemTag);
                return detail.Type == ShipModuleType.LaserBattery ||
                       detail.Type == ShipModuleType.QuadLaser;
            });
            if (!hasTurret)
            {
                SendMessageToPC(gunner, "This ship has no turret battery fitted (laser battery or quad laser).");
                return;
            }

            var (target, targetShipStatus) = Space.GetCurrentTarget(pilot);
            if (!GetIsObjectValid(target) || targetShipStatus == null)
            {
                SendMessageToPC(gunner, "The pilot has no target locked.");
                return;
            }

            var gunnerId = GetObjectUUID(gunner);
            var dbGunner = DB.Get<Player>(gunnerId);
            var rangedRank = dbGunner.Skills[SkillType.Ranged].Rank;

            // The gunner's seat formulas: their reflexes, their marksmanship, the ship's targeting array.
            var agility = GetAbilityScore(gunner, AbilityType.Agility);
            var accuracy = agility * 3 + rangedRank + dbShip.Status.Accuracy - dbShip.Status.ConditionStep * 5;
            var evasion = Space.GetShipEvasion(target);
            var scaleModifier = Space.GetFrameClass(targetShipStatus) switch
            {
                ShipFrameClass.Transport => 5,
                ShipFrameClass.Capital => 10,
                _ => 0
            };
            var chanceToHit = Combat.CalculateHitRate(accuracy, evasion, scaleModifier);

            var attack = 8 + 2 * rangedRank + GetAbilityModifier(AbilityType.Perception, gunner);
            var perception = GetAbilityScore(gunner, AbilityType.Perception);
            var defense = Space.GetShipDefense(target, targetShipStatus.ThermalDefense * 2);
            var vitality = GetAbilityScore(target, AbilityType.Vitality);
            var damage = Combat.CalculateDamage(attack, 16, perception, defense, vitality, 0);

            StartRecast(gunner, TurretRecastVariable, TurretRecastSeconds);
            CombatPoint.AddCombatPoint(gunner, target, SkillType.Ranged, 2);

            var roll = Random.D100(1);
            var isHit = roll <= chanceToHit;

            AssignCommand(pilot, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Blast), target);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2), target);
            });

            if (isHit)
            {
                DelayCommand(0.3f, () =>
                {
                    Space.ApplyShipDamage(pilot, target, damage);
                });
                SendMessageToPC(gunner, $"Your turret hits {GetName(target)} for {damage} damage.");
            }
            else
            {
                SendMessageToPC(gunner, $"Your turret misses {GetName(target)}.");
            }

            Enmity.ModifyEnmity(pilot, target, isHit ? damage : 5);
        }

        /// <summary>
        /// Restores the ship's shield rating by 5 + 1 per 10 Engineering ranks - the
        /// engineer's action IS the shield recharge cycle (there is no passive regen).
        /// Engineer combat points route to Engineering.
        /// </summary>
        public static void RechargeShields(uint engineer)
        {
            if (IsOnRecast(engineer, ShieldsRecastVariable))
                return;

            var (pilot, dbShip) = FindShipByInterior(engineer);
            if (!GetIsObjectValid(pilot))
                return;

            if (dbShip.Status.Shield >= dbShip.Status.MaxShield)
            {
                SendMessageToPC(engineer, "The shield rating is already at full strength.");
                return;
            }

            var engineerId = GetObjectUUID(engineer);
            var dbEngineer = DB.Get<Player>(engineerId);
            var engineeringRank = dbEngineer.Skills[SkillType.Engineering].Rank;
            var amount = 5 + engineeringRank / 10;

            StartRecast(engineer, ShieldsRecastVariable, ShieldsRecastSeconds);

            Space.RestoreShield(pilot, dbShip.Status, amount);
            DB.Set(dbShip);

            SendMessageToPC(engineer, $"You cycle the emitters: shield rating +{amount} ({dbShip.Status.Shield}/{dbShip.Status.MaxShield}).");
            SendMessageToPC(pilot, ColorToken.Cyan($"{GetName(engineer)} recharges the shields. (+{amount})"));

            TagSupportCombatPoint(engineer, pilot);
        }

        /// <summary>
        /// Works the ship one step back up the condition track - the engineer's damage
        /// control action. Engineer combat points route to Engineering.
        /// </summary>
        public static void DamageControl(uint engineer)
        {
            if (IsOnRecast(engineer, DamageControlRecastVariable))
                return;

            var (pilot, dbShip) = FindShipByInterior(engineer);
            if (!GetIsObjectValid(pilot))
                return;

            if (dbShip.Status.ConditionStep <= 0)
            {
                SendMessageToPC(engineer, "All systems read sound. Nothing to repair.");
                return;
            }

            StartRecast(engineer, DamageControlRecastVariable, DamageControlRecastSeconds);

            Space.RecoverCondition(pilot, dbShip.Status, 1);
            DB.Set(dbShip);

            SendMessageToPC(engineer, "You reroute power and patch the damage. The ship steadies.");

            TagSupportCombatPoint(engineer, pilot);
        }

        // Support seats earn credit against whatever the pilot is fighting, routed to Engineering.
        private static void TagSupportCombatPoint(uint engineer, uint pilot)
        {
            var (target, targetShipStatus) = Space.GetCurrentTarget(pilot);
            if (GetIsObjectValid(target) && targetShipStatus != null)
            {
                CombatPoint.AddCombatPoint(engineer, target, SkillType.Engineering, 2);
            }
        }
    }
}
