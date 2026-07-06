using System;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// Persisted per-area race records. Id = the area's resref; a track exists wherever
    /// a map author places RACE_WP_1..N waypoints.
    /// </summary>
    public class RaceTrack : EntityBase
    {
        public RaceTrack()
        {
            BestPlayerName = string.Empty;
            BestPlayerId = string.Empty;
        }

        public int BestTimeMilliseconds { get; set; }
        public string BestPlayerName { get; set; }
        public string BestPlayerId { get; set; }
        public DateTime BestSetAt { get; set; }
    }
}
