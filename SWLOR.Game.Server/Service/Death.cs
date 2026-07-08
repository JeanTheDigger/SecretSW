using System;
using System.Globalization;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public class Death
    {
        /// <summary>
        /// When a player starts dying, instantly kill them.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDying)]
        public static void OnPlayerDying()
        {
            ApplyEffectToObject(DurationType.Instant, EffectDeath(), GetLastPlayerDying());
        }

        /// <summary>
        /// Handles resetting a player's standard faction reputations and displaying the respawn pop-up menu.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnPlayerDeath()
        {
            var player = GetLastPlayerDied();
            var hostile = GetLastHostileActor(player);

            // Two-side PvP match intercept — runs BEFORE the reputation reset below (which would wipe the
            // match's per-side personal reputation) and before the medcenter/XP-debt death path. Consults the
            // Side match's life-state: a player with a life/ticket left respawns in the arena; an eliminated
            // player falls through to the normal death handling (which removes them from the instance).
            var deathArea = GetArea(player);
            if (Side.HasMatch(deathArea) && Side.GetPlayerSide(deathArea, player) != null)
            {
                var result = Side.RegisterDeath(deathArea, player);
                if (result == SideDeathResult.Respawn)
                {
                    ApplyEffectToObject(DurationType.Instant, EffectResurrection(), player);
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(player)), player);
                    DelayCommand(0.1f, () => Ability.ReapplyAuraEffectsForCreature(player));

                    if (Side.TryGetRespawnLocation(deathArea, player, out var respawn))
                        AssignCommand(player, () => ActionJumpToLocation(respawn));

                    return; // stay in the instance; skip rep reset, medcenter, XP debt, death GUI
                }

                // Eliminated. In a LETHAL match this is the permadeath moment — but ONLY inside a turn-based
                // encounter (real-time combat is never permanent, by design), only on a genuine PC kill, and
                // never when the finishing blow was a subdual "spare". Any of those failing falls through to the
                // ordinary elimination path (subdual is caught by the Subdual branch below; non-lethal
                // eliminations take the normal death handling that removes them from the instance).
                if (result == SideDeathResult.Eliminated &&
                    Side.IsLethalMatch(deathArea) &&
                    TurnBased.HasEncounter(deathArea) &&
                    GetIsPC(hostile) && !GetIsDM(hostile) && !GetIsDMPossessed(hostile) &&
                    DB.Get<Player>(GetObjectUUID(hostile))?.Settings.IsSubdualModeEnabled != true)
                {
                    ProcessPermaDeath(player);
                    WriteAudit(player);
                    return;
                }
                // Eliminated / out — fall through to the normal death path.
            }

            SetStandardFactionReputation(StandardFaction.Commoner, 100, player);
            SetStandardFactionReputation(StandardFaction.Merchant, 100, player);
            SetStandardFactionReputation(StandardFaction.Defender, 100, player);

            var factionMember = GetFirstFactionMember(hostile, false);
            while (GetIsObjectValid(factionMember))
            {
                ClearPersonalReputation(player, factionMember);
                factionMember = GetNextFactionMember(hostile, false);
            }

            if (GetIsPC(hostile) && !GetIsDM(hostile) && !GetIsDMPossessed(hostile))
            {
                var hostilePlayerId = GetObjectUUID(hostile);
                var dbHostilePlayer = DB.Get<Player>(hostilePlayerId);
                if (dbHostilePlayer != null && dbHostilePlayer.Settings.IsSubdualModeEnabled)
                {
                    SendMessageToPC(player, "You have been subdued.");
                    Messaging.SendMessageNearbyToPlayers(player, $"{GetName(player)} has been subdued by {GetName(hostile)}.");
                    ApplyEffectToObject(DurationType.Instant, EffectResurrection(), player);
                    DelayCommand(0.1f, () => Ability.ReapplyAuraEffectsForCreature(player));
                    ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), player, 60f);
                    ApplyEffectToObject(DurationType.Temporary, EffectSlow(), player, 300f);
                    ApplyEffectToObject(DurationType.Temporary, EffectACDecrease(10), player, 300f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAccuracyDecrease(10), player, 300f);
                }
            }
            else
            {
                // Second Wind (Cardio Regulator prototype tier): a death to anything
                // other than a player restarts the heart - once per 30 minutes.
                if (!GetIsPC(hostile) && TrySecondWind(player))
                    return;

                // Lethal kills inside PvP event zones feed the Phase-2 endgame SP economy.
                WorldEvent.ProcessPvPKill(hostile, player);

                // Perma-death: dying inside an active event zone removes the character from play.
                // Only those who have passed the Trials wager their life - the Order protects
                // learners; an unflagged character in an event zone takes an ordinary death.
                // The character is moved to limbo for admin review - never deleted automatically.
                if (WorldEvent.IsEventZone(GetArea(player)) && HasCompletedTrials(player))
                {
                    ProcessPermaDeath(player);
                    WriteAudit(player);
                    return;
                }

                const string RespawnMessage = "You have died. Wait for another player to revive you or respawn to go to your registered medical center.";
                PopUpDeathGUIPanel(player, true, true, 0, RespawnMessage);

                WriteAudit(player);
            }
        }

        /// <summary>
        /// Handles setting player's HP, FP, and STM to half of maximum,
        /// applies penalties for death, and teleports him or her to their home point.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleRespawn)]
        public static void OnPlayerRespawn()
        {
            var player = GetLastRespawnButtonPresser();
            var maxHP = GetMaxHitPoints(player);

            var amount = maxHP / 2;
            ApplyEffectToObject(DurationType.Instant, EffectResurrection(), player);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), player);

            SendToHomePoint(player);
            var xpLost = ApplyPenalties(player);

            WriteAudit(player, xpLost);
        }

        private const string SecondWindCooldownVariable = "SECOND_WIND_READY_AT";
        private const int SecondWindCooldownMinutes = 30;

        /// <summary>
        /// The Cardio Regulator's prototype-tier passive: a fatal PvE blow restarts the
        /// heart at a quarter strength, once per 30 minutes. Never fires on player kills
        /// (the perma-death wager cannot be implanted away) - the caller enforces that.
        /// </summary>
        private static bool TrySecondWind(uint player)
        {
            if (Perk.GetPerkLevel(player, PerkType.ImplantCardio) < 6)
                return false;

            var raw = GetLocalString(player, SecondWindCooldownVariable);
            if (!string.IsNullOrWhiteSpace(raw) &&
                DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var readyAt) &&
                DateTime.UtcNow < readyAt)
            {
                return false;
            }

            SetLocalString(player, SecondWindCooldownVariable,
                DateTime.UtcNow.AddMinutes(SecondWindCooldownMinutes).ToString("O"));

            ApplyEffectToObject(DurationType.Instant, EffectResurrection(), player);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(player) / 4), player);
            DelayCommand(0.1f, () => Ability.ReapplyAuraEffectsForCreature(player));

            FloatingTextStringOnCreature(ColorToken.Green("Your cardio regulator slams your heart back into rhythm. SECOND WIND!"), player, false);
            Messaging.SendMessageNearbyToPlayers(player, $"{GetName(player)} gets back up!");

            return true;
        }

        /// <summary>
        /// The waypoint tag of the out-of-play holding area for perma-dead characters.
        /// </summary>
        public const string PermaDeathLimboWaypoint = "PERMADEATH_LIMBO";

        /// <summary>
        /// Determines whether a character has passed the Trials and is therefore exposed
        /// to perma-death inside event zones.
        /// </summary>
        private static bool HasCompletedTrials(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            return dbPlayer != null && dbPlayer.HasCompletedTrials;
        }

        /// <summary>
        /// Flags a character as perma-dead and moves them to the limbo area.
        /// The character file is never destroyed by the game - an admin either restores them
        /// (bug escape hatch) or deletes them manually after review.
        /// </summary>
        private static void ProcessPermaDeath(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            dbPlayer.IsPermaDead = true;
            DB.Set(dbPlayer);

            // The signature weapon outlives its wielder: it drops as a lootable heirloom.
            SignatureWeapon.DropHeirloom(player);

            ApplyEffectToObject(DurationType.Instant, EffectResurrection(), player);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(player)), player);
            SendToLimbo(player);

            SendMessageToPC(player, ColorToken.Red("You have fallen. Your story ends here, pending review by the staff."));
            Log.Write(LogGroup.Death, $"PERMADEATH: {GetName(player)} ({playerId})");
        }

        /// <summary>
        /// Moves a character to the perma-death limbo area and holds them there.
        /// If the limbo waypoint is missing from the module, the character is held in place instead.
        /// </summary>
        private static void SendToLimbo(uint player)
        {
            var waypoint = GetWaypointByTag(PermaDeathLimboWaypoint);

            if (GetIsObjectValid(waypoint))
            {
                var location = GetLocation(waypoint);
                AssignCommand(player, () => ActionJumpToLocation(location));
            }
            else
            {
                Log.Write(LogGroup.Error, $"Perma-death limbo waypoint '{PermaDeathLimboWaypoint}' is missing from the module.");
            }

            DelayCommand(2f, () =>
            {
                ApplyEffectToObject(DurationType.Permanent, EffectCutsceneImmobilize(), player);
            });
        }

        /// <summary>
        /// Perma-dead characters who log in are routed straight back to limbo.
        /// They remain available for admin review but never re-enter play.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void EnforcePermaDeath()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null || !dbPlayer.IsPermaDead) return;

            SendMessageToPC(player, ColorToken.Red("This character has fallen and awaits staff review."));
            DelayCommand(1f, () => SendToLimbo(player));
        }

        /// <summary>
        /// Clears a character's perma-death state and returns them to their home point.
        /// Used by the DM restore tooling.
        /// </summary>
        public static void RestorePermaDeadCharacter(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            dbPlayer.IsPermaDead = false;
            DB.Set(dbPlayer);

            for (var effect = GetFirstEffect(player); GetIsEffectValid(effect); effect = GetNextEffect(player))
            {
                if (GetEffectType(effect) == EffectTypeScript.CutsceneImmobilize)
                    RemoveEffect(player, effect);
            }

            SendToHomePoint(player);
            SendMessageToPC(player, "You have been restored by the staff. Welcome back.");
            Log.Write(LogGroup.Death, $"PERMADEATH RESTORE: {GetName(player)} ({playerId})");
        }

        /// <summary>
        /// Handles setting a player's respawn point if they don't have one set already.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void InitializeRespawnPoint()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player(playerId);

            // Already have a respawn point, no need to set the default one.
            if (!string.IsNullOrWhiteSpace(dbPlayer.RespawnAreaResref)) return;

            var waypoint = GetWaypointByTag("DEATH_DEFAULT_RESPAWN_POINT");
            var position = GetPosition(waypoint);
            var areaResref = GetResRef(GetArea(waypoint));
            var facing = GetFacing(waypoint);

            dbPlayer.RespawnLocationX = position.X;
            dbPlayer.RespawnLocationY = position.Y;
            dbPlayer.RespawnLocationZ = position.Z;
            dbPlayer.RespawnAreaResref = areaResref;
            dbPlayer.RespawnLocationOrientation = facing;

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Write an audit entry with details of this death.
        /// </summary>
        /// <param name="player">The player who died</param>
        private static void WriteAudit(uint player)
        {
            var name = GetName(player);
            var area = GetArea(player);
            var areaName = GetName(area);
            var areaTag = GetTag(area);
            var areaResref = GetResRef(area);
            var hostile = GetLastHostileActor(player);
            var hostileName = GetName(hostile);

            var log = $"DEATH: {name} - {areaName} - {areaTag} - {areaResref} Killed by: {hostileName}";
            Log.Write(LogGroup.Death, log);
        }


        /// <summary>
        /// Teleports player to his or her last home point.
        /// </summary>
        /// <param name="player">The player to teleport</param>
        public static void SendToHomePoint(uint player)
        {
            var playerId = GetObjectUUID(player);
            var entity = DB.Get<Player>(playerId);
            var area = Area.GetAreaByResref(entity.RespawnAreaResref);
            var position = Vector3(
                entity.RespawnLocationX,
                entity.RespawnLocationY,
                entity.RespawnLocationZ);

            if (!GetIsObjectValid(area))
            {
                var defaultLocation = GetLocation(GetWaypointByTag("DTH_DEFAULT_RESPAWN_POINT"));
                AssignCommand(player, () => ActionJumpToLocation(defaultLocation));
            }
            else
            {
                var location = Location(area, position, entity.RespawnLocationOrientation);
                AssignCommand(player, () => ActionJumpToLocation(location));
            }
        }

        /// <summary>
        /// Applies death penalties for a player.
        /// </summary>
        /// <param name="player">The player who we're applying penalties to</param>
        private static int ApplyPenalties(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            int multiplier;

            // 600+
            if (dbPlayer.TotalSPAcquired >= 600)
                multiplier = 65;
            // 450 - 599
            else if (dbPlayer.TotalSPAcquired >= 450)
                multiplier = 55;
            // 300 - 449
            else if (dbPlayer.TotalSPAcquired >= 300)
                multiplier = 45;
            // 200 - 299
            else if (dbPlayer.TotalSPAcquired >= 200)
                multiplier = 35;
            // 50 - 199
            else if (dbPlayer.TotalSPAcquired >= 50)
                multiplier = 25;
            // 0 - 49
            else
                multiplier = 15;

            var social = GetAbilityScore(player, AbilityType.Social);
            var newDebt = dbPlayer.TotalSPAcquired * multiplier;
            var reductionBonus = 0f;
            reductionBonus += Property.GetEffectiveUpgradeLevel(dbPlayer.CitizenPropertyId, PropertyUpgradeType.MedicalCenterLevel) * 0.05f; // -5% per Medical Center level

            if (social > 10)
            {
                reductionBonus += (social - 10) * 0.03f; // -3% per SOC
            }

            if (reductionBonus > 0.8f)
                reductionBonus = 0.8f;

            newDebt -= (int)(newDebt * reductionBonus);

            dbPlayer.XPDebt += newDebt;

            const int MaxDebt = 9999999;
            if (dbPlayer.XPDebt > MaxDebt)
                dbPlayer.XPDebt = MaxDebt;

            DB.Set(dbPlayer);

            SendMessageToPC(player, $"{newDebt} XP added to your debt. (Total: {dbPlayer.XPDebt} XP)");

            return dbPlayer.XPDebt;
        }

        /// <summary>
        /// Writes an audit entry to the Death audit group.
        /// </summary>
        /// <param name="player">The player who respawned</param>
        /// <param name="xpLost">The amount of XP lost</param>
        private static void WriteAudit(uint player, int xpLost)
        {
            var name = GetName(player);
            var log = $"RESPAWN - {name} - {xpLost} XP lost";

            Log.Write(LogGroup.Death, log);
        }
    }
}
