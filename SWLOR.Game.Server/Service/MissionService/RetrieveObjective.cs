using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.MissionService
{
    /// <summary>
    /// Retrieve: grab a tagged item and carry it to a tagged extraction point. Item-centric — the
    /// objective follows the ITEM, not a fixed carrier: whoever holds it is the carrier, and if it is
    /// dropped (e.g. on death) it is back in play for anyone to grab, which enables the PvP extraction
    /// race. Completes when the current holder is within radius of the extraction point.
    /// </summary>
    public class RetrieveObjective : MissionObjective
    {
        private readonly string _itemTag;
        private readonly string _extractTag;
        private readonly float _radius;
        private bool _acquired;

        public RetrieveObjective(string itemTag, string extractTag, float radius = 3.0f)
        {
            _itemTag = itemTag;
            _extractTag = extractTag;
            _radius = radius <= 0f ? 3.0f : radius;
        }

        public override string Description => _acquired
            ? $"Carry [{_itemTag}] to extraction [{_extractTag}]"
            : $"Retrieve the item [{_itemTag}]";

        public override void OnItemAcquired(uint item, uint acquiredBy)
        {
            if (IsComplete) return;
            if (GetTag(item) == _itemTag)
                _acquired = true;
        }

        public override void OnHeartbeat(uint area, IReadOnlyList<uint> playersInArea)
        {
            if (IsComplete) return;

            var item = GetObjectByTag(_itemTag);
            if (!GetIsObjectValid(item)) return;

            var holder = GetItemPossessor(item);
            if (!GetIsObjectValid(holder))
            {
                // Dropped / on the ground — back in play; reflect that in the description.
                _acquired = false;
                return;
            }

            _acquired = true;

            var destination = GetObjectByTag(_extractTag);
            if (!GetIsObjectValid(destination)) return;

            if (GetArea(holder) == GetArea(destination) && GetDistanceBetween(holder, destination) <= _radius)
                IsComplete = true;
        }
    }
}
