using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.MissionService
{
    /// <summary>
    /// Extraction Race (PvP): a contested, item-centric win condition. One tagged item is in play; each side
    /// has its own extraction point. Whoever holds the item is the carrier (item drops on death → back in
    /// play for anyone), and the FIRST side to carry it to THEIR extraction point wins. Reuses the item /
    /// possessor tracking of RetrieveObjective, made side-aware via the Side rosters.
    ///
    /// Only a rostered PC carrier scores (an NPC/companion carrier is ignored for v1). Item start is just a
    /// tagged placement, so a neutral/center start (symmetric race) or an in-territory start (asymmetric
    /// attack/defend) both work — that is a map/authoring choice, not code.
    /// </summary>
    public class ExtractionRaceObjective : MissionObjective
    {
        private readonly uint _area;
        private readonly string _itemTag;
        private readonly Dictionary<string, string> _extractTagBySide;
        private readonly float _radius;
        private string _winningSide;

        public ExtractionRaceObjective(uint area, string itemTag, Dictionary<string, string> extractTagBySide, float radius = 3.0f)
        {
            _area = area;
            _itemTag = itemTag;
            _extractTagBySide = extractTagBySide ?? new Dictionary<string, string>();
            _radius = radius <= 0f ? 3.0f : radius;
        }

        public override string Description => _winningSide != null
            ? $"Side [{_winningSide}] extracted [{_itemTag}] — victory"
            : $"Extraction race: deliver [{_itemTag}] to your side's extraction point";

        public override void OnHeartbeat(uint area, IReadOnlyList<uint> playersInArea)
        {
            if (IsComplete) return;

            var item = GetObjectByTag(_itemTag);
            if (!GetIsObjectValid(item)) return;

            var holder = GetItemPossessor(item);
            if (!GetIsObjectValid(holder)) return; // dropped / on the ground — in play

            var side = Side.GetPlayerSide(_area, holder);
            if (side == null) return; // carrier is not a rostered PC

            if (!_extractTagBySide.TryGetValue(side, out var extractTag)) return;

            var destination = GetObjectByTag(extractTag);
            if (!GetIsObjectValid(destination)) return;

            if (GetArea(holder) == GetArea(destination) && GetDistanceBetween(holder, destination) <= _radius)
            {
                _winningSide = side;
                IsComplete = true;
            }
        }
    }
}
