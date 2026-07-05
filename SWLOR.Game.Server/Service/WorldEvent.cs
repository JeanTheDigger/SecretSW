using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.WorldEventService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The Phase-2 endgame event system. Event zones are ordinary areas flagged at runtime
    /// (by the scheduler rotation or by a DM). While a zone is active:
    /// - PvE zones: creatures flagged with the EVENT_SP_REWARD local variable award endgame SP
    ///   to every contributor when they die, routed to the skill each player actually used.
    /// - PvP zones: player kills award endgame SP to the killer's active weapon skill, with a
    ///   same-victim cooldown so kill trading pays nothing.
    /// Endgame SP only matters for characters past the Phase-1 cap (Skill.GiveEndgameSP gates it).
    /// </summary>
    public static class WorldEvent
    {
        // Area-scoped local variables.
        public const string EventZoneVariable = "IS_EVENT_ZONE";
        public const string EventTypeVariable = "EVENT_TYPE";

        // Creature-scoped local variable: endgame SP awarded to each contributor on death.
        public const string EventSPRewardVariable = "EVENT_SP_REWARD";

        // A killer earns SP for the same victim at most once per this window.
        private static readonly TimeSpan SameVictimWindow = TimeSpan.FromHours(1);

        private class ActiveEvent
        {
            public uint Area { get; set; }
            public WorldEventType Type { get; set; }
            public DateTime ClosesAt { get; set; }
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
        /// Opens an event in the given area for the specified duration.
        /// </summary>
        public static void OpenEvent(uint area, WorldEventType type, int durationMinutes)
        {
            if (!GetIsObjectValid(area) || type == WorldEventType.Invalid || durationMinutes <= 0)
                return;

            SetLocalBool(area, EventZoneVariable, true);
            SetLocalInt(area, EventTypeVariable, (int)type);

            _activeEvents[area] = new ActiveEvent
            {
                Area = area,
                Type = type,
                ClosesAt = DateTime.UtcNow.AddMinutes(durationMinutes)
            };

            var typeName = type == WorldEventType.PvP ? "PvP" : "PvE";
            BroadcastToServer($"A {typeName} event has begun: {GetName(area)} ({durationMinutes} minutes).");
        }

        /// <summary>
        /// Closes the event in the given area, if one is active.
        /// </summary>
        public static void CloseEvent(uint area)
        {
            if (!_activeEvents.Remove(area))
                return;

            DeleteLocalBool(area, EventZoneVariable);
            DeleteLocalInt(area, EventTypeVariable);

            BroadcastToServer($"The event at {GetName(area)} has ended.");
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
        /// Lists all active events as display strings (for DM tooling).
        /// </summary>
        public static IEnumerable<string> GetActiveEventDescriptions()
        {
            return _activeEvents.Values
                .Select(x => $"{GetName(x.Area)} - {x.Type} - closes {x.ClosesAt:HH:mm} UTC");
        }

        /// <summary>
        /// When a flagged creature dies inside an active event zone, every contributor gains
        /// endgame SP routed to the skill they used most against it.
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

            foreach (var (player, skill) in CombatPoint.GetTopContributionSkills(creature))
            {
                if (!GetIsObjectValid(player))
                    continue;

                Skill.GiveEndgameSP(player, skill, reward);
            }
        }

        /// <summary>
        /// Awards endgame SP for a player kill inside a PvP event zone, routed to the killer's
        /// active weapon skill. The same victim pays out at most once per hour per killer.
        /// Called by the death pipeline.
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
