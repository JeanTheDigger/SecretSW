using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.PerkService
{
    /// <summary>
    /// Requires a free implant slot to install a NEW implant line. A character supports
    /// two installed lines, or three after the Trials. Upgrading a line already installed
    /// always passes - the slot is already occupied by it.
    /// </summary>
    public class PerkRequirementImplantSlot : IPerkRequirement
    {
        private readonly PerkType _perkType;

        public PerkRequirementImplantSlot(PerkType perkType)
        {
            _perkType = perkType;
        }

        public string CheckRequirements(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            // Already installed - leveling it up occupies no new slot.
            if (dbPlayer.Perks.ContainsKey(_perkType) && dbPlayer.Perks[_perkType] > 0)
                return string.Empty;

            var limit = Implant.GetSlotLimit(dbPlayer);
            return Implant.CountInstalledLines(dbPlayer) >= limit
                ? $"Your body supports at most {limit} implant lines. One must be removed before another can be installed."
                : string.Empty;
        }

        public string RequirementText => "Requires a free implant slot.";
    }
}
