using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.MissionService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Mission objective tracker — holds each active mission's objectives IN MEMORY (keyed by area
    /// for now; will be keyed by instance once the Mission generator/instance system exists) and
    /// routes game events to them, announcing progress and firing overall success/failure.
    /// Self-contained: reuses existing creature/placeable death, item-acquire, and heartbeat events
    /// and needs no instance/PvP/grid infrastructure. Add objective types by subclassing
    /// MissionObjective and overriding the relevant hook.
    /// </summary>
    public static class Mission
    {
        private class Run
        {
            public uint Area;
            public List<MissionObjective> Objectives = new();
            public HashSet<MissionObjective> AnnouncedComplete = new();
            public bool Finished;
        }

        private static readonly Dictionary<uint, Run> _runsByArea = new();

        /// <summary>
        /// True if the given area currently has an active mission run.
        /// </summary>
        public static bool HasRun(uint area) => _runsByArea.ContainsKey(area);

        /// <summary>
        /// Adds an objective to the area's mission run, creating the run if needed.
        /// </summary>
        public static void AddObjective(uint area, MissionObjective objective)
        {
            if (!GetIsObjectValid(area) || objective == null)
                return;

            if (!_runsByArea.TryGetValue(area, out var run))
            {
                run = new Run { Area = area };
                _runsByArea[area] = run;
            }

            run.Objectives.Add(objective);
            Announce(area, "New objective: " + objective.Description);
        }

        /// <summary>
        /// Ends and clears the area's mission run.
        /// </summary>
        public static void EndRun(uint area)
        {
            if (_runsByArea.Remove(area))
                Announce(area, "Mission objectives cleared.");
        }

        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void OnCreatureDeath()
        {
            var creature = OBJECT_SELF;
            RouteAndEvaluate(o => o.OnCreatureKilled(creature));
        }

        [NWNEventHandler(ScriptName.OnPlaceableDeath)]
        public static void OnPlaceableDeath()
        {
            var placeable = OBJECT_SELF;
            RouteAndEvaluate(o => o.OnPlaceableDestroyed(placeable));
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnPlayerDied()
        {
            var player = GetLastPlayerDied();
            RouteAndEvaluate(o => o.OnPlayerDied(player));
        }

        [NWNEventHandler(ScriptName.OnModuleAcquire)]
        public static void OnItemAcquired()
        {
            var item = GetModuleItemAcquired();
            var acquiredBy = GetModuleItemAcquiredBy();
            RouteAndEvaluate(o => o.OnItemAcquired(item, acquiredBy));
        }

        [NWNEventHandler(ScriptName.OnSwlorHeartbeat)]
        public static void OnHeartbeat()
        {
            foreach (var run in _runsByArea.Values.ToList())
            {
                var players = Area.GetPlayersInArea(run.Area);
                foreach (var objective in run.Objectives)
                    objective.OnHeartbeat(run.Area, players);

                Evaluate(run);
            }
        }

        private static void RouteAndEvaluate(Action<MissionObjective> apply)
        {
            // ToList() so a run cannot be mutated mid-iteration by an announcement/side effect.
            foreach (var run in _runsByArea.Values.ToList())
            {
                foreach (var objective in run.Objectives)
                    apply(objective);

                Evaluate(run);
            }
        }

        private static void Evaluate(Run run)
        {
            foreach (var objective in run.Objectives)
            {
                if (objective.IsComplete && run.AnnouncedComplete.Add(objective))
                    Announce(run.Area, "Objective complete: " + objective.Description);
            }

            if (run.Finished)
                return;

            if (run.Objectives.Any(o => o.Failed))
            {
                run.Finished = true;
                Announce(run.Area, "*** MISSION FAILED ***");
                return;
            }

            // Success is evaluated over the completable (non-fail-condition) objectives only: a
            // fail-condition rider like "keep the VIP alive" must never gate completion, it can only
            // fail the mission (handled above). Guard against a run made up solely of riders.
            var completable = run.Objectives.Where(o => !o.IsFailCondition).ToList();
            if (completable.Count > 0 && completable.All(o => o.IsComplete))
            {
                run.Finished = true;
                Announce(run.Area, "*** All mission objectives complete! ***");
            }
        }

        private static void Announce(uint area, string message)
        {
            foreach (var player in Area.GetPlayersInArea(area))
                SendMessageToPC(player, message);
        }
    }
}
