using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A single placed structure captured in a ship interior layout snapshot.
    /// The blueprint only - the physical structure items must be re-acquired.
    /// </summary>
    public class ShipLayoutStructure
    {
        public StructureType StructureType { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Orientation { get; set; }
    }

    /// <summary>
    /// A snapshot of a ship interior's placed-structure arrangement, keyed to the owner
    /// (Id = owner player Id; one snapshot per player). Captured automatically when a
    /// frame is lost in deep space and manually via /shiplayout save. Restoring onto a
    /// new same-layout ship re-places matching structures from the player's inventory.
    /// </summary>
    public class ShipInteriorLayout : EntityBase
    {
        public ShipInteriorLayout()
        {
            Structures = new List<ShipLayoutStructure>();
            ShipName = string.Empty;
        }

        public string ShipName { get; set; }
        public PropertyLayoutType Layout { get; set; }
        public List<ShipLayoutStructure> Structures { get; set; }
        public DateTime SavedAt { get; set; }
    }
}
