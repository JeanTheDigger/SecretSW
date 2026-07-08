using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.MissionService
{
    /// <summary>
    /// Protect the VIP: a tagged ally (NPC or PC) must stay alive. Parameterized by an optional survive
    /// duration:
    ///  - surviveSeconds &lt;= 0  → fail-condition RIDER: the VIP simply must still be alive when the other
    ///    objectives finish; it never completes on its own (IsFailCondition = true) but dies → mission fail.
    ///  - surviveSeconds &gt; 0   → survive-the-clock: completes once the VIP has lived that long; dies before
    ///    then → fail. This is a normal completable objective (IsFailCondition = false).
    /// Reuses the shared Failed state (see Mission.Evaluate) — any Failed objective fails the whole mission.
    /// </summary>
    public class ProtectObjective : MissionObjective
    {
        private readonly string _vipTag;
        private readonly bool _isRider;
        private int _remainingHeartbeats;

        /// <param name="vipTag">Tag of the creature to protect.</param>
        /// <param name="surviveSeconds">0 (default) = fail-condition rider; &gt;0 = survive-the-clock in seconds.</param>
        public ProtectObjective(string vipTag, int surviveSeconds = 0)
        {
            _vipTag = vipTag;
            _isRider = surviveSeconds <= 0;
            // SWLOR heartbeat ticks roughly once per second; treat one heartbeat as one second.
            _remainingHeartbeats = _isRider ? 0 : surviveSeconds;
        }

        public override bool IsFailCondition => _isRider;

        public override string Description => _isRider
            ? $"Keep the VIP [{_vipTag}] alive"
            : $"Protect the VIP [{_vipTag}] for {_remainingHeartbeats}s";

        public override void OnCreatureKilled(uint creature)
        {
            if (IsComplete || Failed) return;
            if (GetTag(creature) == _vipTag)
                Failed = true;
        }

        public override void OnHeartbeat(uint area, IReadOnlyList<uint> playersInArea)
        {
            if (IsComplete || Failed) return;

            // Catch a VIP that is dead/despawned even without a death event reaching us.
            var vip = GetObjectByTag(_vipTag);
            if (!GetIsObjectValid(vip) || GetIsDead(vip))
            {
                Failed = true;
                return;
            }

            if (_isRider) return; // rider never completes on its own; it only fails on death.

            if (_remainingHeartbeats > 0)
                _remainingHeartbeats--;

            if (_remainingHeartbeats <= 0)
                IsComplete = true;
        }
    }
}
