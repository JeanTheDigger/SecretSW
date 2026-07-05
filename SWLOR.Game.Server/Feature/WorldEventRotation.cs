using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.WorldEventService;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// The automatic event baseline: a round-robin rotation that opens a world event on a
    /// fixed cadence so the Phase-2 endgame runs without staff online. Knight and Master
    /// brackets alternate through the table so both tiers get regular events; DM-run
    /// specials (/eventopen) ride on top and may ignore brackets entirely.
    /// </summary>
    public static class WorldEventRotation
    {
        private static readonly TimeSpan RotationInterval = TimeSpan.FromHours(3);
        private const int EventDurationMinutes = 45;

        private class RotationEntry
        {
            public string AreaTag { get; init; }
            public WorldEventType Type { get; init; }
            public WorldEventBracket Bracket { get; init; }
        }

        // Contested sites across the cluster. Tags reference existing module areas.
        private static readonly List<RotationEntry> _rotation = new()
        {
            new RotationEntry { AreaTag = "DantooineKinrathCaves", Type = WorldEventType.PvE, Bracket = WorldEventBracket.Knight },
            new RotationEntry { AreaTag = "Ossuswastes", Type = WorldEventType.PvP, Bracket = WorldEventBracket.Master },
            new RotationEntry { AreaTag = "MonCaladungeon", Type = WorldEventType.PvE, Bracket = WorldEventBracket.Knight },
            new RotationEntry { AreaTag = "DathomirCaveRuins", Type = WorldEventType.PvE, Bracket = WorldEventBracket.Master },
            new RotationEntry { AreaTag = "KorribanValleyoftheDarkLords", Type = WorldEventType.PvP, Bracket = WorldEventBracket.Knight },
            new RotationEntry { AreaTag = "DathGrottoCaverns", Type = WorldEventType.PvE, Bracket = WorldEventBracket.Master },
        };

        private static int _nextIndex;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void StartRotation()
        {
            Scheduler.ScheduleRepeating(OpenNextEvent, RotationInterval);
        }

        private static void OpenNextEvent()
        {
            if (_rotation.Count == 0)
                return;

            // Walk the table until an existing, currently-idle area is found.
            for (var attempts = 0; attempts < _rotation.Count; attempts++)
            {
                var entry = _rotation[_nextIndex];
                _nextIndex = (_nextIndex + 1) % _rotation.Count;

                var area = FindAreaByTag(entry.AreaTag);
                if (!GetIsObjectValid(area))
                {
                    Log.Write(LogGroup.Error, $"World event rotation: area tag '{entry.AreaTag}' was not found in the module. Skipping.");
                    continue;
                }

                if (WorldEvent.IsEventZone(area))
                    continue;

                WorldEvent.OpenEvent(area, entry.Type, EventDurationMinutes, entry.Bracket);
                return;
            }
        }

        private static uint FindAreaByTag(string tag)
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                if (GetTag(area) == tag)
                    return area;
            }

            return OBJECT_INVALID;
        }
    }
}
