using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Ship interior layout templates: the arrangement of placed structures is
    /// snapshotted (the blueprint, not the items) automatically when a frame is lost in
    /// deep space and manually via /shiplayout save. On a new ship with the SAME interior
    /// layout, /shiplayout restore re-places matching structure items from the player's
    /// inventory and lists the missing ones as a shopping list. One snapshot per player.
    /// </summary>
    public static class ShipLayout
    {
        /// <summary>
        /// Captures the placed-structure arrangement of a ship property.
        /// Safe to call at frame-loss time - the structure records still exist until the
        /// delayed property deletion runs.
        /// </summary>
        /// <param name="ownerPlayerId">The ship owner whose snapshot slot is written.</param>
        /// <param name="shipProperty">The starship property being captured.</param>
        public static void SnapshotInterior(string ownerPlayerId, WorldProperty shipProperty)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), shipProperty.Id, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure);
            var count = (int)DB.SearchCount(query);
            var structures = DB.Search(query.AddPaging(count, 0)).ToList();

            var snapshot = new ShipInteriorLayout
            {
                Id = ownerPlayerId,
                ShipName = shipProperty.CustomName,
                Layout = shipProperty.Layout,
                SavedAt = DateTime.UtcNow
            };

            foreach (var structure in structures)
            {
                if (!structure.Positions.ContainsKey(PropertyLocationType.StaticPosition))
                    continue;

                var position = structure.Positions[PropertyLocationType.StaticPosition];
                snapshot.Structures.Add(new ShipLayoutStructure
                {
                    StructureType = structure.StructureType,
                    X = position.X,
                    Y = position.Y,
                    Z = position.Z,
                    Orientation = position.Orientation
                });
            }

            DB.Set(snapshot);
        }

        /// <summary>
        /// Resolves the starship property the player is currently standing inside,
        /// provided they own it. Returns null otherwise (with feedback already sent).
        /// </summary>
        private static WorldProperty GetOwnShipInterior(uint player)
        {
            var area = GetArea(player);
            var propertyId = Property.GetPropertyId(area);
            if (string.IsNullOrWhiteSpace(propertyId))
            {
                SendMessageToPC(player, "You must be inside your ship to manage its layout.");
                return null;
            }

            var property = DB.Get<WorldProperty>(propertyId);
            if (property == null || property.PropertyType != PropertyType.Starship)
            {
                SendMessageToPC(player, "You must be inside your ship to manage its layout.");
                return null;
            }

            var playerId = GetObjectUUID(player);
            if (property.OwnerPlayerId != playerId)
            {
                SendMessageToPC(player, "Only the ship's owner may manage its layout.");
                return null;
            }

            return property;
        }

        /// <summary>
        /// Manually saves the current ship interior arrangement as the player's snapshot.
        /// </summary>
        public static void SaveLayout(uint player)
        {
            var property = GetOwnShipInterior(player);
            if (property == null)
                return;

            SnapshotInterior(GetObjectUUID(player), property);
            SendMessageToPC(player, "Ship interior layout saved. Use '/shiplayout restore' aboard a ship with the same interior to re-place your furnishings.");
        }

        /// <summary>
        /// Shows the player's saved snapshot: source ship, structure count, and age.
        /// </summary>
        public static void ShowStatus(uint player)
        {
            var snapshot = DB.Get<ShipInteriorLayout>(GetObjectUUID(player));
            if (snapshot == null)
            {
                SendMessageToPC(player, "You have no saved ship layout. Use '/shiplayout save' aboard your ship.");
                return;
            }

            SendMessageToPC(player,
                $"Saved layout: {snapshot.ShipName} ({snapshot.Structures.Count} structures), captured {snapshot.SavedAt:yyyy-MM-dd HH:mm} UTC.");
        }

        /// <summary>
        /// Best-effort restore: re-places structures from the player's snapshot using
        /// matching structure items in their inventory. Same interior layout required.
        /// Missing structure items are listed as a shopping list.
        /// </summary>
        public static void RestoreLayout(uint player)
        {
            var property = GetOwnShipInterior(player);
            if (property == null)
                return;

            var snapshot = DB.Get<ShipInteriorLayout>(GetObjectUUID(player));
            if (snapshot == null || snapshot.Structures.Count <= 0)
            {
                SendMessageToPC(player, "You have no saved ship layout. Use '/shiplayout save' aboard your ship.");
                return;
            }

            if (snapshot.Layout != property.Layout)
            {
                SendMessageToPC(player, $"Your saved layout was captured aboard '{snapshot.ShipName}', which has a different interior. Layouts only restore onto the same hull layout.");
                return;
            }

            var area = GetArea(player);

            // Index the player's carried structure items by type.
            var carried = new Dictionary<StructureType, Queue<uint>>();
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                var structureType = Property.GetStructureTypeFromItem(item);
                if (structureType == StructureType.Invalid)
                    continue;

                if (!carried.ContainsKey(structureType))
                    carried[structureType] = new Queue<uint>();
                carried[structureType].Enqueue(item);
            }

            var placed = 0;
            var missing = new Dictionary<StructureType, int>();

            foreach (var entry in snapshot.Structures)
            {
                if (!carried.ContainsKey(entry.StructureType) || carried[entry.StructureType].Count <= 0)
                {
                    missing[entry.StructureType] = missing.TryGetValue(entry.StructureType, out var n) ? n + 1 : 1;
                    continue;
                }

                var item = carried[entry.StructureType].Dequeue();
                var location = Location(area, Vector3(entry.X, entry.Y, entry.Z), entry.Orientation);
                var structure = Property.CreateStructure(property.Id, item, entry.StructureType, location);

                // CreateStructure records a zero orientation; carry the snapshot's facing over.
                structure.Positions[PropertyLocationType.StaticPosition].Orientation = entry.Orientation;
                DB.Set(structure);

                var placeable = Property.GetPlaceableByPropertyId(structure.Id);
                if (GetIsObjectValid(placeable))
                {
                    AssignCommand(placeable, () => SetFacing(entry.Orientation));
                }

                placed++;
            }

            SendMessageToPC(player, $"Layout restored: {placed} of {snapshot.Structures.Count} structures placed.");

            if (missing.Count > 0)
            {
                var shoppingList = string.Join(", ", missing.Select(x =>
                {
                    var detail = Property.GetStructureByType(x.Key);
                    return $"{detail.Name} x{x.Value}";
                }));
                SendMessageToPC(player, $"Missing structures (re-acquire and run restore again): {shoppingList}");
            }
        }
    }
}
