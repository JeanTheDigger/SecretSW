using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The swoop racing scaffold: any area whose author places waypoints tagged
    /// RACE_WP_1..N becomes a timed circuit. /race starts (or cancels) a run through the
    /// checkpoints in order; per-area best times persist as track records. Mounts and
    /// swoop appearances are map-phase content that rides this scaffold.
    /// </summary>
    public static class SwoopRacing
    {
        private const float CheckpointRadius = 5f;

        private class RaceState
        {
            public uint Area { get; set; }
            public string AreaResref { get; set; }
            public List<uint> Waypoints { get; set; }
            public int NextIndex { get; set; }
            public DateTime StartTime { get; set; }
        }

        private static readonly Dictionary<uint, RaceState> _activeRaces = new();

        /// <summary>
        /// Starts a race on the current area's course, or cancels an active run.
        /// </summary>
        public static void ToggleRace(uint player)
        {
            if (_activeRaces.Remove(player))
            {
                SendMessageToPC(player, "Race cancelled.");
                return;
            }

            var area = GetArea(player);
            var waypoints = CollectCourse(area);
            if (waypoints.Count < 2)
            {
                SendMessageToPC(player, "There is no race course here.");
                return;
            }

            var state = new RaceState
            {
                Area = area,
                AreaResref = GetResRef(area),
                Waypoints = waypoints,
                NextIndex = 0,
                StartTime = DateTime.UtcNow
            };
            _activeRaces[player] = state;

            var track = DB.Get<RaceTrack>(state.AreaResref);
            var recordText = track == null
                ? "No record has been set on this track."
                : $"Track record: {FormatTime(track.BestTimeMilliseconds)} by {track.BestPlayerName}.";

            SendMessageToPC(player, $"Race started! {waypoints.Count} checkpoints. {recordText}");
            FloatingTextStringOnCreature(ColorToken.Green("GO!"), player, false);
            DelayCommand(0.5f, () => Tick(player));
        }

        /// <summary>
        /// Gathers the ordered RACE_WP_1..N waypoints belonging to the given area.
        /// </summary>
        private static List<uint> CollectCourse(uint area)
        {
            var waypoints = new List<uint>();

            for (var index = 1; ; index++)
            {
                var found = OBJECT_INVALID;
                for (var nth = 0; ; nth++)
                {
                    var waypoint = GetObjectByTag($"RACE_WP_{index}", nth);
                    if (!GetIsObjectValid(waypoint))
                        break;

                    if (GetArea(waypoint) == area)
                    {
                        found = waypoint;
                        break;
                    }
                }

                if (!GetIsObjectValid(found))
                    break;

                waypoints.Add(found);
            }

            return waypoints;
        }

        private static void Tick(uint player)
        {
            if (!_activeRaces.TryGetValue(player, out var state))
                return;

            // Leaving the track or the game forfeits the run.
            if (!GetIsObjectValid(player) || GetArea(player) != state.Area)
            {
                _activeRaces.Remove(player);
                if (GetIsObjectValid(player))
                    SendMessageToPC(player, "You left the track - race forfeited.");
                return;
            }

            var next = state.Waypoints[state.NextIndex];
            if (GetDistanceBetween(player, next) <= CheckpointRadius)
            {
                state.NextIndex++;

                if (state.NextIndex >= state.Waypoints.Count)
                {
                    FinishRace(player, state);
                    return;
                }

                FloatingTextStringOnCreature(
                    ColorToken.Cyan($"Checkpoint {state.NextIndex}/{state.Waypoints.Count}!"), player, false);
            }

            DelayCommand(0.5f, () => Tick(player));
        }

        private static void FinishRace(uint player, RaceState state)
        {
            _activeRaces.Remove(player);

            var elapsedMs = (int)(DateTime.UtcNow - state.StartTime).TotalMilliseconds;
            SendMessageToPC(player, $"Race complete! Your time: {FormatTime(elapsedMs)}.");

            var track = DB.Get<RaceTrack>(state.AreaResref) ?? new RaceTrack
            {
                Id = state.AreaResref
            };

            if (track.BestTimeMilliseconds <= 0 || elapsedMs < track.BestTimeMilliseconds)
            {
                track.BestTimeMilliseconds = elapsedMs;
                track.BestPlayerName = GetName(player);
                track.BestPlayerId = GetObjectUUID(player);
                track.BestSetAt = DateTime.UtcNow;
                DB.Set(track);

                Messaging.SendMessageNearbyToPlayers(player,
                    ColorToken.Green($"{GetName(player)} sets a new track record: {FormatTime(elapsedMs)}!"));
            }
            else
            {
                SendMessageToPC(player, $"Track record stands: {FormatTime(track.BestTimeMilliseconds)} by {track.BestPlayerName}.");
            }
        }

        private static string FormatTime(int milliseconds)
        {
            var time = TimeSpan.FromMilliseconds(milliseconds);
            return $"{(int)time.TotalMinutes}:{time.Seconds:D2}.{time.Milliseconds:D3}";
        }
    }
}
