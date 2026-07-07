using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.MissionService
{
    /// <summary>
    /// Sabotage / Slice: a tagged terminal must be worked for a number of consecutive ticks by a PC standing
    /// next to it. Modeled as a proximity-hold channel (honest simplification — NOT a literal ability channel
    /// with damage-interrupt): each heartbeat, if at least one PC is within radius of the terminal, progress
    /// advances; if nobody is in range, progress RESETS to zero. This creates the intended "defend the hacker"
    /// gameplay — the team must hold the position long enough. Completes once progress reaches requiredTicks.
    /// (A future refinement can swap this for the real ability-activation interrupt logic in Ability.cs.)
    /// </summary>
    public class SliceObjective : MissionObjective
    {
        private readonly string _terminalTag;
        private readonly int _requiredTicks;
        private readonly float _radius;
        private int _progress;

        /// <param name="terminalTag">Tag of the terminal placeable to slice.</param>
        /// <param name="requiredTicks">Consecutive in-range heartbeats needed (≈ seconds). Default 5.</param>
        /// <param name="radius">How close a PC must be to the terminal to make progress.</param>
        public SliceObjective(string terminalTag, int requiredTicks = 5, float radius = 3.0f)
        {
            _terminalTag = terminalTag;
            _requiredTicks = requiredTicks <= 0 ? 5 : requiredTicks;
            _radius = radius <= 0f ? 3.0f : radius;
        }

        public override string Description => _progress > 0 && !IsComplete
            ? $"Slicing terminal [{_terminalTag}]... ({_progress}/{_requiredTicks})"
            : $"Slice the terminal [{_terminalTag}]";

        public override void OnHeartbeat(uint area, IReadOnlyList<uint> playersInArea)
        {
            if (IsComplete) return;

            var terminal = GetObjectByTag(_terminalTag);
            if (!GetIsObjectValid(terminal)) return;

            var anyInRange = playersInArea.Any(p =>
                GetIsObjectValid(p) &&
                GetArea(p) == GetArea(terminal) &&
                GetDistanceBetween(p, terminal) <= _radius);

            if (anyInRange)
            {
                _progress++;
                if (_progress >= _requiredTicks)
                    IsComplete = true;
            }
            else
            {
                // Nobody holding the terminal — the slice attempt lapses.
                _progress = 0;
            }
        }
    }
}
