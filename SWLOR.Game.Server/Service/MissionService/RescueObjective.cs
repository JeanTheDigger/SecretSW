using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.MissionService
{
    /// <summary>
    /// Rescue / Escort: a tagged NPC must survive and reach a tagged extraction point.
    ///  - Rescue  (startsCaptive = true):  the NPC starts captive/idle and is "freed" when a PC gets close
    ///    to it; only after being freed does it start following the team to extraction.
    ///  - Escort  (startsCaptive = false): the NPC is already with the team and follows immediately.
    /// The NPC follows the nearest PC (user decision — team-follow, not a fixed path, to avoid stuck-escort
    /// deaths) and the reach check is delegated to an inner ReachObjective in SpecificCarrier mode (the NPC
    /// is the carrier). NPC dies → objective FAILS (shared Failed state → mission fail).
    /// </summary>
    public class RescueObjective : MissionObjective
    {
        private readonly string _npcTag;
        private readonly bool _startsCaptive;
        private readonly float _rescueRadius;
        private readonly ReachObjective _reach;
        private bool _freed;
        private bool _following;

        /// <param name="npcTag">Tag of the NPC to rescue/escort.</param>
        /// <param name="extractTag">Tag of the extraction destination object.</param>
        /// <param name="startsCaptive">true = Rescue (must be freed first); false = Escort (follows at once).</param>
        /// <param name="radius">Proximity radius for freeing the captive and for reaching extraction.</param>
        public RescueObjective(string npcTag, string extractTag, bool startsCaptive = true, float radius = 3.0f)
        {
            _npcTag = npcTag;
            _startsCaptive = startsCaptive;
            _rescueRadius = radius <= 0f ? 3.0f : radius;
            _reach = new ReachObjective(extractTag, radius, ReachQualifier.SpecificCarrier);
            _freed = !startsCaptive; // escort is already "freed"
        }

        public override string Description
        {
            get
            {
                if (!_freed) return $"Reach and free [{_npcTag}]";
                return _startsCaptive
                    ? $"Escort [{_npcTag}] to safety"
                    : $"Escort [{_npcTag}] to the extraction point";
            }
        }

        public override void OnCreatureKilled(uint creature)
        {
            if (IsComplete || Failed) return;
            if (GetTag(creature) == _npcTag)
                Failed = true;
        }

        public override void OnHeartbeat(uint area, IReadOnlyList<uint> playersInArea)
        {
            if (IsComplete || Failed) return;

            var npc = GetObjectByTag(_npcTag);
            if (!GetIsObjectValid(npc) || GetIsDead(npc))
            {
                Failed = true;
                return;
            }

            var leader = NearestPlayer(npc, playersInArea);

            // Phase 1 — free the captive when a PC gets close.
            if (!_freed)
            {
                if (GetIsObjectValid(leader) && GetDistanceBetween(npc, leader) <= _rescueRadius)
                    _freed = true;
                else
                    return;
            }

            // Phase 2 — make the NPC follow the nearest PC (issued once).
            if (!_following && GetIsObjectValid(leader))
            {
                var followLeader = leader;
                AssignCommand(npc, () =>
                {
                    ClearAllActions();
                    ActionForceFollowObject(followLeader, 2.0f);
                });
                _following = true;
            }

            // Phase 3 — reach extraction (the NPC is the carrier).
            _reach.SetCarrier(npc);
            _reach.OnHeartbeat(area, playersInArea);
            if (_reach.IsComplete)
                IsComplete = true;
        }

        private static uint NearestPlayer(uint npc, IReadOnlyList<uint> playersInArea)
        {
            var nearest = OBJECT_INVALID;
            var nearestDistance = -1f;

            foreach (var player in playersInArea)
            {
                if (!GetIsObjectValid(player)) continue;
                var distance = GetDistanceBetween(npc, player);
                if (nearestDistance < 0f || distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = player;
                }
            }

            return nearest;
        }
    }
}
