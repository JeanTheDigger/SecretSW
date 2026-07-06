using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.WorldEventService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The Phase-2 endgame event system. Event zones are ordinary areas flagged at runtime
    /// (by the scheduler rotation or by a DM). While a zone is active:
    /// - PvE zones: creatures flagged with the EVENT_SP_REWARD local variable award endgame SP
    ///   to every contributor when they die, and may drop a stance unlock (holocron/datacron).
    /// - PvP zones: player kills award endgame SP to the killer's active weapon skill, with a
    ///   same-victim cooldown so kill trading pays nothing; the event's top killer receives a
    ///   stance unlock when the event closes.
    /// Every event carries a power bracket (Knight/Master/Open) enforced at the zone boundary.
    /// Endgame SP only matters for characters past the Phase-1 cap (Skill.GiveEndgameSP gates it).
    /// </summary>
    public static class WorldEvent
    {
        // Area-scoped local variables.
        public const string EventZoneVariable = "IS_EVENT_ZONE";
        public const string EventTypeVariable = "EVENT_TYPE";
        public const string EventBracketVariable = "EVENT_BRACKET";

        // Creature-scoped local variables: endgame SP awarded to each contributor on death,
        // and the percent chance to drop a stance unlock item (unset = default; negative = never).
        public const string EventSPRewardVariable = "EVENT_SP_REWARD";
        public const string EventUnlockDropChanceVariable = "EVENT_UNLOCK_DROP_CHANCE";
        private const int DefaultUnlockDropChance = 25;

        // The Master bracket begins at this much total SP; the Knight bracket ends before it.
        public const int MasterBracketMinimumSP = 500;

        // A killer earns SP for the same victim at most once per this window.
        private static readonly TimeSpan SameVictimWindow = TimeSpan.FromHours(1);

        private class ActiveEvent
        {
            public uint Area { get; set; }
            public WorldEventType Type { get; set; }
            public WorldEventBracket Bracket { get; set; }
            public DateTime ClosesAt { get; set; }
            public Dictionary<string, int> PvPKillCounts { get; } = new();
        }

        private static readonly Dictionary<uint, ActiveEvent> _activeEvents = new();
        private static readonly Dictionary<(string, string), DateTime> _recentPvPCredits = new();

        /// <summary>
        /// On module load, start the expiry sweep. Events opened with a duration close themselves.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void StartEventProcessor()
        {
            Scheduler.ScheduleRepeating(CloseExpiredEvents, TimeSpan.FromMinutes(1));
        }

        private static void CloseExpiredEvents()
        {
            var now = DateTime.UtcNow;
            foreach (var active in _activeEvents.Values.Where(x => x.ClosesAt <= now).ToList())
            {
                CloseEvent(active.Area);
            }
        }

        /// <summary>
        /// Opens an event in the given area for the specified duration. Players already inside
        /// who do not meet the bracket are moved out - a zone can flip while occupied.
        /// </summary>
        public static void OpenEvent(uint area, WorldEventType type, int durationMinutes, WorldEventBracket bracket = WorldEventBracket.Open)
        {
            if (!GetIsObjectValid(area) || type == WorldEventType.Invalid || durationMinutes <= 0)
                return;

            SetLocalBool(area, EventZoneVariable, true);
            SetLocalInt(area, EventTypeVariable, (int)type);
            SetLocalInt(area, EventBracketVariable, (int)bracket);

            _activeEvents[area] = new ActiveEvent
            {
                Area = area,
                Type = type,
                Bracket = bracket,
                ClosesAt = DateTime.UtcNow.AddMinutes(durationMinutes)
            };

            SweepOccupants(area);

            var typeName = type == WorldEventType.PvP ? "PvP" : "PvE";
            var bracketName = bracket == WorldEventBracket.Open ? "open" : $"{bracket}-tier";
            BroadcastToServer($"A {bracketName} {typeName} event has begun: {GetName(area)} ({durationMinutes} minutes).");
        }

        /// <summary>
        /// Closes the event in the given area, if one is active. In a PvP event, the top
        /// killer receives a stance unlock (holocron/datacron) as the victor's prize.
        /// </summary>
        public static void CloseEvent(uint area)
        {
            if (!_activeEvents.Remove(area, out var active))
                return;

            DeleteLocalBool(area, EventZoneVariable);
            DeleteLocalInt(area, EventTypeVariable);
            DeleteLocalInt(area, EventBracketVariable);

            BroadcastToServer($"The event at {GetName(area)} has ended.");
            AwardPvPVictor(active);
        }

        private static void AwardPvPVictor(ActiveEvent active)
        {
            if (active.Type != WorldEventType.PvP || active.PvPKillCounts.Count == 0)
                return;

            var victorId = active.PvPKillCounts
                .OrderByDescending(x => x.Value)
                .First().Key;

            var victor = GetPlayerById(victorId);
            if (!GetIsObjectValid(victor))
                return;

            GiveStanceUnlockItem(victor);
            BroadcastToServer($"{GetName(victor)} emerged victorious from the event at {GetName(active.Area)}.");
        }

        private static uint GetPlayerById(string playerId)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetObjectUUID(player) == playerId)
                    return player;
            }

            return OBJECT_INVALID;
        }

        /// <summary>
        /// Determines whether the given area is an active event zone.
        /// </summary>
        public static bool IsEventZone(uint area)
        {
            return GetLocalBool(area, EventZoneVariable);
        }

        /// <summary>
        /// Retrieves the event type of the given area, or Invalid if no event is active there.
        /// </summary>
        public static WorldEventType GetEventType(uint area)
        {
            return !IsEventZone(area)
                ? WorldEventType.Invalid
                : (WorldEventType)GetLocalInt(area, EventTypeVariable);
        }

        /// <summary>
        /// Retrieves the bracket of the given area's event. Only meaningful while IsEventZone is true.
        /// </summary>
        public static WorldEventBracket GetEventBracket(uint area)
        {
            return (WorldEventBracket)GetLocalInt(area, EventBracketVariable);
        }

        /// <summary>
        /// Determines whether a player belongs inside the given area's event bracket.
        /// Non-event areas admit everyone; DMs are always admitted. Characters who have not
        /// completed the Trials may only enter Open events, and only once Phase 1 is finished
        /// (Trials candidates attending their own ceremony). Flagged characters split by total
        /// SP: Knight events below the Master threshold, Master events at or above it.
        /// </summary>
        public static bool MeetsBracketRequirements(uint player, uint area)
        {
            if (!IsEventZone(area))
                return true;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return true;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return false;

            var bracket = GetEventBracket(area);

            if (!dbPlayer.HasCompletedTrials)
            {
                return bracket == WorldEventBracket.Open &&
                       dbPlayer.TotalSPAcquired >= Skill.Phase1Cap;
            }

            return bracket switch
            {
                WorldEventBracket.Knight => dbPlayer.TotalSPAcquired < MasterBracketMinimumSP,
                WorldEventBracket.Master => dbPlayer.TotalSPAcquired >= MasterBracketMinimumSP,
                _ => true
            };
        }

        /// <summary>
        /// Moves every player inside the area who fails the bracket check back to their
        /// home point. Used when an event opens on an occupied zone.
        /// </summary>
        private static void SweepOccupants(uint area)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetArea(player) != area)
                    continue;
                if (MeetsBracketRequirements(player, area))
                    continue;

                SendMessageToPC(player, ColorToken.Red("An event has begun here that you may not take part in. You have been moved to safety."));
                Death.SendToHomePoint(player);
            }
        }

        /// <summary>
        /// Lists all active events as display strings (for DM tooling).
        /// </summary>
        public static IEnumerable<string> GetActiveEventDescriptions()
        {
            return _activeEvents.Values
                .Select(x => $"{GetName(x.Area)} - {x.Type} - {x.Bracket} - closes {x.ClosesAt:HH:mm} UTC");
        }

        /// <summary>
        /// When a flagged creature dies inside an active event zone, every contributor gains
        /// endgame SP routed to the skill they used most against it, and one random contributor
        /// may receive a stance unlock (holocron/datacron).
        /// Runs before the combat point cache is cleared by the XP distribution.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void OnEventCreatureDeath()
        {
            var creature = OBJECT_SELF;
            var reward = GetLocalInt(creature, EventSPRewardVariable);
            if (reward <= 0)
                return;

            var area = GetArea(creature);
            if (!IsEventZone(area))
                return;

            var contributors = new List<uint>();
            foreach (var (player, skill) in CombatPoint.GetTopContributionSkills(creature))
            {
                if (!GetIsObjectValid(player))
                    continue;

                Skill.GiveEndgameSP(player, skill, reward);
                contributors.Add(player);
            }

            RollUnlockDrop(creature, contributors);
        }

        private static void RollUnlockDrop(uint creature, List<uint> contributors)
        {
            if (contributors.Count == 0)
                return;

            var chance = GetLocalInt(creature, EventUnlockDropChanceVariable);
            if (chance == 0)
                chance = DefaultUnlockDropChance;
            if (chance < 0)
                return;

            if (Random.D100(1) > chance)
                return;

            var receiver = contributors[Random.Next(contributors.Count)];
            GiveStanceUnlockItem(receiver);
        }

        /// <summary>
        /// Awards endgame SP for a player kill inside a PvP event zone, routed to the killer's
        /// active weapon skill. The same victim pays out at most once per hour per killer.
        /// Kills also count toward the event's victor tally. Called by the death pipeline.
        /// </summary>
        public static void ProcessPvPKill(uint killer, uint victim)
        {
            if (!GetIsPC(killer) || GetIsDM(killer) || !GetIsPC(victim) || GetIsDM(victim))
                return;

            var area = GetArea(victim);
            if (GetEventType(area) != WorldEventType.PvP || GetArea(killer) != area)
                return;

            var killerId = GetObjectUUID(killer);
            var victimId = GetObjectUUID(victim);
            var now = DateTime.UtcNow;

            if (_recentPvPCredits.TryGetValue((killerId, victimId), out var lastCredit) &&
                now - lastCredit < SameVictimWindow)
            {
                SendMessageToPC(killer, "You have defeated this opponent too recently to learn anything new.");
                return;
            }

            var weapon = GetItemInSlot(InventorySlot.RightHand, killer);
            var skill = Skill.GetSkillTypeByBaseItem(GetBaseItemType(weapon));
            if (skill == SkillType.Invalid)
                skill = SkillType.MartialArts;

            _recentPvPCredits[(killerId, victimId)] = now;
            Skill.GiveEndgameSP(killer, skill, 1);

            if (_activeEvents.TryGetValue(area, out var active))
            {
                active.PvPKillCounts.TryGetValue(killerId, out var kills);
                active.PvPKillCounts[killerId] = kills + 1;
            }
        }

        // ==============================================================================
        // Stance unlock items (holocrons for Force-Sensitives, combat datacrons for
        // Standard characters). Fabricated at runtime on a generic usable blueprint;
        // the STANCE_UNLOCK item definition consumes them. Proper art is a HAK task.
        // ==============================================================================

        public const string StanceUnlockItemTag = "STANCE_UNLOCK";
        public const string StanceUnlockPerkVariable = "UNLOCK_PERK";
        private const string StanceUnlockBaseResref = "recipe_trnsabers";

        private static readonly PerkType[] _formUnlocks =
        {
            PerkType.FormShiiCho, PerkType.FormMakashi, PerkType.FormSoresu,
            PerkType.FormAtaru, PerkType.FormDjemSo, PerkType.FormNiman, PerkType.FormJuyo,
            // Flight doctrines are class-neutral - they roll in both pools.
            PerkType.DoctrineInterceptor, PerkType.DoctrineStrike, PerkType.DoctrineEscort,
            PerkType.DoctrineLineCommander, PerkType.DoctrineFleetDefense, PerkType.DoctrineWolfpack,
            PerkType.DroidOverseer
        };

        private static readonly PerkType[] _standardUnlocks =
        {
            PerkType.DoctrineDuelist, PerkType.DoctrineJuggernaut, PerkType.DoctrineTempest,
            PerkType.DoctrineTerasKasi, PerkType.DoctrineMarksman,
            PerkType.ImplantNeural, PerkType.ImplantOcular, PerkType.ImplantDermal,
            PerkType.ImplantSkeletal, PerkType.ImplantCardio, PerkType.ImplantServo,
            PerkType.ImplantCortical,
            PerkType.DoctrineInterceptor, PerkType.DoctrineStrike, PerkType.DoctrineEscort,
            PerkType.CarboniteProjector, PerkType.CombatJetpack, PerkType.OrbitalStrike,
            PerkType.DoctrineLineCommander, PerkType.DoctrineFleetDefense, PerkType.DoctrineWolfpack,
            PerkType.DroidOverseer
        };

        /// <summary>
        /// Fabricates a random unlock item matched to the player's class and places it in
        /// their inventory. FS characters receive form holocrons; Standard characters
        /// receive combat datacrons (doctrines) or prototype schematics (implants).
        /// </summary>
        public static void GiveStanceUnlockItem(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            var isForceSensitive = dbPlayer.CharacterType == CharacterType.ForceSensitive;
            var pool = isForceSensitive ? _formUnlocks : _standardUnlocks;
            var perkType = pool[Random.Next(pool.Length)];
            var perkDetail = Perk.GetPerkDetails(perkType);
            var perkName = perkDetail.Name;

            string itemName, itemDescription;
            if (perkDetail.Category == PerkCategoryType.Leadership)
            {
                itemName = $"Command Codex: {perkName}";
                itemDescription = $"A fleet commander's codex on {perkName}. Using it opens the path to the doctrine's higher levels.";
            }
            else if (perkDetail.Category == PerkCategoryType.Piloting)
            {
                itemName = $"Flight Recorder: {perkName}";
                itemDescription = $"A named ace's flight recorder, its telemetry a masterclass in {perkName}. Using it opens the path to the doctrine's higher levels.";
            }
            else if (isForceSensitive)
            {
                itemName = $"Holocron: {perkName}";
                itemDescription = $"An ancient holocron holding a master's insight into {perkName}. Using it opens the path to the form's higher levels.";
            }
            else if (perkDetail.Category == PerkCategoryType.Cybernetics)
            {
                itemName = $"Prototype Schematic: {perkName}";
                itemDescription = $"Stolen prototype schematics for the {perkName} implant line. Using them opens the path to its prototype tiers.";
            }
            else
            {
                itemName = $"Combat Datacron: {perkName}";
                itemDescription = $"A combat datacron recorded by a veteran of {perkName}. Using it opens the path to the doctrine's higher levels.";
            }

            var item = CreateItemOnObject(StanceUnlockBaseResref, player);
            SetTag(item, StanceUnlockItemTag);
            SetName(item, itemName);
            SetDescription(item, itemDescription);
            SetLocalInt(item, StanceUnlockPerkVariable, (int)perkType);

            SendMessageToPC(player, ColorToken.Cyan($"You have received: {itemName}!"));
        }

        private static void BroadcastToServer(string message)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                SendMessageToPC(player, ColorToken.Cyan(message));
            }
        }
    }
}
