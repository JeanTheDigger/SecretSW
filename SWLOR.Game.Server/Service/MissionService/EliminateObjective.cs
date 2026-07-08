using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.MissionService
{
    /// <summary>
    /// Eliminate the Opposing Side (PvP team-deathmatch win condition). Thin adapter over the Side match's
    /// life-state: on each heartbeat it asks Side.GetMatchWinner and completes when only one side is left
    /// standing (that side wins). Only PLAYERS gate the outcome (allied NPCs are support and never counted).
    ///
    /// Scoring (SingleElimination / LimitedLives / SharedTickets) and the per-player/ticket accounting live in
    /// the Side match (set via Side.StartMatch), so the death intercept and this objective never disagree on
    /// who is out. Polling on heartbeat (rather than OnPlayerDied) avoids any handler-ordering race with the
    /// Death intercept that registers the death.
    ///
    /// Elimination is NOT permadeath: in real-time combat a defeated player is simply out for this match.
    /// LimitedLives/SharedTickets only become meaningful alongside the team-respawn death intercept (which
    /// keeps a respawning player in the arena instead of the medcenter).
    /// </summary>
    public class EliminateObjective : MissionObjective
    {
        private readonly uint _area;
        private string _winningSide;

        public EliminateObjective(uint area)
        {
            _area = area;
        }

        public override string Description => _winningSide != null
            ? $"Side [{_winningSide}] wins — opposing side eliminated"
            : "Eliminate the opposing side";

        public override void OnHeartbeat(uint area, IReadOnlyList<uint> playersInArea)
        {
            if (IsComplete) return;

            var winner = Side.GetMatchWinner(_area);
            if (winner != null)
            {
                _winningSide = winner;
                IsComplete = true;
            }
        }
    }
}
