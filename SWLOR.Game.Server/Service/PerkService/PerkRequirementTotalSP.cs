using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.PerkService
{
    /// <summary>
    /// Requires a minimum total SP acquired (character-wide progression gate).
    /// Used by the cybernetics lines, which gate on overall advancement rather than one skill.
    /// </summary>
    public class PerkRequirementTotalSP : IPerkRequirement
    {
        private readonly int _requiredTotalSP;

        public PerkRequirementTotalSP(int requiredTotalSP)
        {
            _requiredTotalSP = requiredTotalSP;
        }

        public string CheckRequirements(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer.TotalSPAcquired >= _requiredTotalSP)
                return string.Empty;

            return $"You must have acquired {_requiredTotalSP} total SP. (You have {dbPlayer.TotalSPAcquired})";
        }

        public string RequirementText => $"{_requiredTotalSP} total SP acquired";
    }
}
