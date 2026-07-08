using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.MissionService
{
    /// <summary>
    /// Hold Point (PvP/PvE): capture-and-hold a tagged zone (a center object + radius). A side captures by
    /// standing in the zone UNCONTESTED for a few consecutive ticks → the point flips to their control;
    /// controlled ticks then accrue toward the win. The enemy can re-capture (flip it back). Both sides
    /// present = contested → capture/hold pauses. Win = a side reaches the target hold-time under control.
    ///
    /// In a Side match, "who is present" is resolved by side. With NO Side match (pure PvE hold-the-point),
    /// every present PC counts as one pseudo-side ("players"), so the objective still works as a solo/co-op
    /// timed hold. Evaluated on the mission heartbeat (one tick ≈ one second).
    /// </summary>
    public class HoldPointObjective : MissionObjective
    {
        private const string PveSide = "players";

        private readonly uint _area;
        private readonly string _zoneTag;
        private readonly int _targetHoldTicks;
        private readonly int _captureTicks;
        private readonly float _radius;

        private string _owner;                 // controlling side, or null while neutral
        private string _capturingSide;         // side currently accruing capture progress
        private int _captureProgress;
        private readonly Dictionary<string, int> _holdTicks = new();
        private string _winningSide;

        public HoldPointObjective(uint area, string zoneTag, int targetHoldTicks = 10, int captureTicks = 3, float radius = 4.0f)
        {
            _area = area;
            _zoneTag = zoneTag;
            _targetHoldTicks = targetHoldTicks <= 0 ? 10 : targetHoldTicks;
            _captureTicks = captureTicks <= 0 ? 3 : captureTicks;
            _radius = radius <= 0f ? 4.0f : radius;
        }

        public override string Description
        {
            get
            {
                if (_winningSide != null)
                    return $"Side [{_winningSide}] held [{_zoneTag}]";

                if (_owner != null)
                    return $"Hold [{_zoneTag}] — controlled by [{_owner}] ({HeldBy(_owner)}/{_targetHoldTicks})";

                return $"Capture and hold [{_zoneTag}]";
            }
        }

        public override void OnHeartbeat(uint area, IReadOnlyList<uint> playersInArea)
        {
            if (IsComplete) return;

            var zone = GetObjectByTag(_zoneTag);
            if (!GetIsObjectValid(zone)) return;

            var hasMatch = Side.HasMatch(_area);

            // Which sides currently have a PC standing in the zone?
            var present = new HashSet<string>();
            foreach (var player in playersInArea)
            {
                if (!GetIsObjectValid(player)) continue;
                if (GetArea(player) != GetArea(zone)) continue;
                if (GetDistanceBetween(player, zone) > _radius) continue;

                var side = hasMatch ? Side.GetPlayerSide(_area, player) : PveSide;
                if (side != null)
                    present.Add(side);
            }

            // Contested or empty → capture stalls (hold time is retained, not lost).
            if (present.Count != 1)
            {
                _capturingSide = null;
                _captureProgress = 0;
                return;
            }

            string contender = null;
            foreach (var s in present) contender = s; // the single present side

            if (_owner == contender)
            {
                // Owner holds it uncontested — accrue toward the win.
                var held = HeldBy(contender) + 1;
                _holdTicks[contender] = held;
                if (held >= _targetHoldTicks)
                {
                    _winningSide = contender;
                    IsComplete = true;
                }
            }
            else
            {
                // A challenger holds it — accrue capture progress and flip ownership at the threshold.
                if (_capturingSide == contender)
                {
                    _captureProgress++;
                }
                else
                {
                    _capturingSide = contender;
                    _captureProgress = 1;
                }

                if (_captureProgress >= _captureTicks)
                {
                    _owner = contender;
                    _capturingSide = null;
                    _captureProgress = 0;
                }
            }
        }

        private int HeldBy(string side)
        {
            return _holdTicks.TryGetValue(side, out var t) ? t : 0;
        }
    }
}
