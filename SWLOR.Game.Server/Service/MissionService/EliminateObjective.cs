using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.MissionService
{
    /// <summary>
    /// How elimination is scored for an EliminateObjective.
    /// </summary>
    public enum EliminationMode
    {
        SingleElimination, // one life each — first death takes a player out of the match
        LimitedLives,      // N lives each — a player is out after their Nth death
        SharedTickets      // a side shares a pool of N respawns; once its pool is empty, deaths are final
    }

    /// <summary>
    /// Eliminate the Opposing Side (PvP team-deathmatch win condition). Watches the Side rosters for the
    /// match in a given area and completes when only ONE side still has players left in the fight — that
    /// side is the winner. Only PLAYERS gate the outcome (allied NPCs are support and never counted).
    ///
    /// Elimination is NOT permadeath: in real-time combat a defeated player is simply out for this match.
    /// Scoring modes:
    ///  - SingleElimination (default): one death removes a player — fully functional today (no respawn needed).
    ///  - LimitedLives(N) / SharedTickets(N): presume respawn-in-place, so they only become meaningful once the
    ///    team-respawn death intercept exists; the counting is in place and ready for it.
    ///
    /// v1 scope: this computes the WIN CONDITION only. Benching/spectating an eliminated player and the
    /// team-respawn flow are separate increments. A disconnected (but still-rostered) player counts as still
    /// in until the match/side is otherwise resolved.
    /// </summary>
    public class EliminateObjective : MissionObjective
    {
        private readonly uint _area;
        private readonly EliminationMode _mode;
        private readonly int _livesOrTickets;

        // Per-player remaining lives (SingleElimination / LimitedLives).
        private readonly Dictionary<string, int> _livesRemaining = new();
        // Per-side remaining shared respawn tickets (SharedTickets).
        private readonly Dictionary<string, int> _sideTickets = new();
        // Eliminated PC UUIDs grouped by side.
        private readonly Dictionary<string, HashSet<string>> _eliminatedBySide = new();

        private string _winningSide;

        public EliminateObjective(uint area, EliminationMode mode = EliminationMode.SingleElimination, int livesOrTickets = 1)
        {
            _area = area;
            _mode = mode;
            _livesOrTickets = livesOrTickets <= 0 ? 1 : livesOrTickets;
        }

        public override string Description
        {
            get
            {
                if (_winningSide != null)
                    return $"Side [{_winningSide}] wins — opposing side eliminated";

                return _mode switch
                {
                    EliminationMode.LimitedLives => $"Eliminate the opposing side ({_livesOrTickets} lives each)",
                    EliminationMode.SharedTickets => $"Eliminate the opposing side ({_livesOrTickets} shared tickets)",
                    _ => "Eliminate the opposing side"
                };
            }
        }

        public override void OnPlayerDied(uint player)
        {
            if (IsComplete) return;

            var side = Side.GetPlayerSide(_area, player);
            if (side == null) return; // not a participant in this match

            var uuid = GetObjectUUID(player);
            if (IsEliminated(side, uuid)) return; // already out — ignore further deaths

            switch (_mode)
            {
                case EliminationMode.SharedTickets:
                    var remaining = _sideTickets.TryGetValue(side, out var tickets) ? tickets : _livesOrTickets;
                    if (remaining > 0)
                        _sideTickets[side] = remaining - 1; // spend a shared respawn, player stays in
                    else
                        Eliminate(side, uuid); // pool empty — this death is final
                    break;

                default: // SingleElimination / LimitedLives
                    var startingLives = _mode == EliminationMode.SingleElimination ? 1 : _livesOrTickets;
                    var lives = (_livesRemaining.TryGetValue(uuid, out var l) ? l : startingLives) - 1;
                    _livesRemaining[uuid] = lives;
                    if (lives <= 0)
                        Eliminate(side, uuid);
                    break;
            }

            CheckForWinner();
        }

        private void Eliminate(string side, string uuid)
        {
            if (!_eliminatedBySide.TryGetValue(side, out var set))
            {
                set = new HashSet<string>();
                _eliminatedBySide[side] = set;
            }

            set.Add(uuid);
        }

        private bool IsEliminated(string side, string uuid)
        {
            return _eliminatedBySide.TryGetValue(side, out var set) && set.Contains(uuid);
        }

        private int EliminatedCount(string side)
        {
            return _eliminatedBySide.TryGetValue(side, out var set) ? set.Count : 0;
        }

        private void CheckForWinner()
        {
            var sides = Side.GetSideNames(_area);
            if (sides.Count < 2) return; // need at least two sides to decide a winner

            string survivor = null;
            var sidesStillIn = 0;

            foreach (var side in sides)
            {
                var rosterSize = Side.GetSidePlayerCount(_area, side);
                if (rosterSize == 0) continue; // an empty side isn't in the fight

                if (EliminatedCount(side) < rosterSize)
                {
                    sidesStillIn++;
                    survivor = side;
                }
            }

            // Exactly one side left standing (and at least one side was wiped) → that side wins.
            if (sidesStillIn == 1)
            {
                _winningSide = survivor;
                IsComplete = true;
            }
        }
    }
}
