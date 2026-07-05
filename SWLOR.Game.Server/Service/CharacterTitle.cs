using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The universal tier titles: one mechanical ladder (Phase 1 / Trials passed / cap
    /// reached) with per-class skins. Faction-specific flavors (Sith rites, unit
    /// commissions) are roleplay skins over the same three tiers.
    /// </summary>
    public static class CharacterTitle
    {
        public static string GetTitle(Player dbPlayer)
        {
            var isForceSensitive = dbPlayer.CharacterType == CharacterType.ForceSensitive;

            if (dbPlayer.HasCompletedTrials && dbPlayer.TotalSPAcquired >= Skill.AbsoluteCap)
                return isForceSensitive ? "Master" : "Living Legend";

            if (dbPlayer.HasCompletedTrials)
                return isForceSensitive ? "Knight" : "Veteran";

            return isForceSensitive ? "Padawan" : "Recruit";
        }
    }
}
