using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.MissionService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Mission objective tracker — first slice of the mission system. Holds each active mission's
    /// objectives IN MEMORY (keyed by area for now; will be keyed by instance once the Mission
    /// generator/instance system exists) and routes game events to them, announcing progress and
    /// firing overall completion. Self-contained: reuses the existing creature/placeable death
    /// events and needs no instance/PvP/grid infrastructure. Add objective types by subclassing
    /// MissionObjective and overriding the relevant hook.
    /// </summary>
    public static class Mission
    {
        private class Run
        {
            public uint Area;
            public List<MissionObjective> Objectives = new();
            public bool Announced;
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
            RouteToObjectives(o => o.OnCreatureKilled(creature));
        }

        [NWNEventHandler(ScriptName.OnPlaceableDeath)]
        public static void OnPlaceableDeath()
        {
            var placeable = OBJECT_SELF;
            RouteToObjectives(o => o.OnPlaceableDestroyed(placeable));
        }

        private static void RouteToObjectives(Action<MissionObjective> apply)
        {
            // ToList() so a run cannot be mutated mid-iteration by an announcement/side effect.
            foreach (var run in _runsByArea.Values.ToList())
            {
                var newlyComplete = new List<MissionObjective>();
                foreach (var objective in run.Objectives)
                {
                    var wasComplete = objective.IsComplete;
                    apply(objective);
                    if (!wasComplete && objective.IsComplete)
                        newlyComplete.Add(objective);
                }

                foreach (var objective in newlyComplete)
                    Announce(run.Area, "Objective complete: " + objective.Description);

                if (!run.Announced && run.Objectives.Count > 0 && run.Objectives.All(o => o.IsComplete))
                {
                    run.Announced = true;
                    Announce(run.Area, "*** All mission objectives complete! ***");
                }
            }
        }

        private static void Announce(uint area, string message)
        {
            foreach (var player in Area.GetPlayersInArea(area))
                SendMessageToPC(player, message);
        }
    }
}
