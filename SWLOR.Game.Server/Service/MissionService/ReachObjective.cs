using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.MissionService
{
    /// <summary>
    /// Who must reach the destination for a ReachObjective to complete.
    /// </summary>
    public enum ReachQualifier
    {
        AnyOne,          // any single participant reaching it completes the objective
        AllLiving,       // every living participant must be at the destination
        SpecificCarrier  // a specific creature (e.g. the item holder or rescued NPC) must reach it
    }

    /// <summary>
    /// Reach / Extract: completes when the qualifying creature(s) get within radius of a tagged
    /// destination object. Evaluated on the mission heartbeat. Reused by Retrieve/Rescue/Escort via
    /// SpecificCarrier.
    /// </summary>
    public class ReachObjective : MissionObjective
    {
        private readonly string _destinationTag;
        private readonly float _radius;
        private readonly ReachQualifier _qualifier;
        private uint _carrier;

        public ReachObjective(string destinationTag, float radius = 3.0f, ReachQualifier qualifier = ReachQualifier.AnyOne)
        {
            _destinationTag = destinationTag;
            _radius = radius <= 0f ? 3.0f : radius;
            _qualifier = qualifier;
        }

        /// <summary>
        /// Sets the specific creature that must reach the destination (for SpecificCarrier mode).
        /// </summary>
        public void SetCarrier(uint carrier)
        {
            _carrier = carrier;
        }

        public override string Description => $"Reach the destination [{_destinationTag}]";

        public override void OnHeartbeat(uint area, IReadOnlyList<uint> playersInArea)
        {
            if (IsComplete) return;

            var destination = GetObjectByTag(_destinationTag);
            if (!GetIsObjectValid(destination)) return;

            switch (_qualifier)
            {
                case ReachQualifier.SpecificCarrier:
                    if (GetIsObjectValid(_carrier) && !GetIsDead(_carrier) && WithinRadius(_carrier, destination))
                        IsComplete = true;
                    break;
                case ReachQualifier.AllLiving:
                    if (playersInArea.Count > 0 && playersInArea.All(p => WithinRadius(p, destination)))
                        IsComplete = true;
                    break;
                default: // AnyOne
                    if (playersInArea.Any(p => WithinRadius(p, destination)))
                        IsComplete = true;
                    break;
            }
        }

        private bool WithinRadius(uint who, uint destination)
        {
            if (!GetIsObjectValid(who) || GetArea(who) != GetArea(destination))
                return false;

            return GetDistanceBetween(who, destination) <= _radius;
        }
    }
}
