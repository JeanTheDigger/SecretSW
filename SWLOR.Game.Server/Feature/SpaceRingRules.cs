using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Player-facing rules of the three space rings:
    /// - Entering a contested lane or deep space announces the stakes.
    /// - Destroying an NPC ship in ring 2 or 3 pays endgame SP to every top contributor,
    ///   routed to the skill each crew member actually used (safe orbits pay none -
    ///   space cannot cannibalize ground events as the optimal Phase-2 grind).
    /// </summary>
    public static class SpaceRingRules
    {
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void AnnounceRingStakes()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var area = GetArea(player);
            var ring = Space.GetSpaceRing(area);

            switch (ring)
            {
                case SpaceRingType.ContestedLane:
                    SendMessageToPC(player, ColorToken.Orange("CONTESTED SPACE: ship combat is unrestricted here, and destroyed ships lose their fitted modules."));
                    break;
                case SpaceRingType.DeepSpace:
                    SendMessageToPC(player, ColorToken.Red("DEEP SPACE: there is no law and no rescue out here. Ships destroyed beyond the lanes are LOST - frame and all. Crews escape by pod."));
                    break;
            }
        }

        /// <summary>
        /// Endgame SP for space kills: when a registered NPC ship dies in ring 2 or 3,
        /// each top contributor earns endgame SP (1 in contested lanes, 2 in deep space).
        /// Runs before the combat point cache is cleared by the XP distribution.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void OnNPCShipDeath()
        {
            var creature = OBJECT_SELF;
            if (GetIsPC(creature))
                return;
            if (Space.GetShipStatus(creature) == null)
                return;

            var ring = Space.GetSpaceRing(GetArea(creature));
            if (ring == SpaceRingType.SafeOrbit)
                return;

            var reward = ring == SpaceRingType.DeepSpace ? 2 : 1;
            foreach (var (player, skill) in CombatPoint.GetTopContributionSkills(creature))
            {
                if (!GetIsObjectValid(player))
                    continue;

                Skill.GiveEndgameSP(player, skill, reward);
            }
        }
    }
}
