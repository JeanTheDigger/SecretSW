using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Entity
{
    public class PlayerShip: EntityBase
    {
        public PlayerShip()
        {
            PlayerHotBars = new Dictionary<string, string>();
            Refits = new List<string>();
        }

        /// <summary>
        /// Permanent frame refits in Mk order (index 0 = Mk I). Each entry is a branch:
        /// engine, armor, emitter, or expansion. Max three; re-buying a slot overwrites it.
        /// </summary>
        public List<string> Refits { get; set; }

        [Indexed]
        public string OwnerPlayerId { get; set; }
        [Indexed]
        public string PropertyId { get; set; }
        public string SerializedItem { get; set; }
        public ShipStatus Status { get; set; }
        public Dictionary<string, string> PlayerHotBars { get; set; }
    }
}
