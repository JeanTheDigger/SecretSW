using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Mk I-III frame refits: three ordered slots, one branch chosen per slot, permanent
    /// (re-buying a slot at full price overwrites its branch). Engine buys maneuver and
    /// speed, Armor buys hull and threshold, Emitter buys shield rating, Expansion adds a
    /// systems node. Performed aboard your OWN DOCKED ship with /refit; costs are credits
    /// until the shipwright material recipes arrive with the map-phase item pass.
    /// Frame class is the ceiling: magnitudes differ by class, and no refit changes what
    /// the ship IS.
    /// </summary>
    public static class ShipRefit
    {
        public const string EngineBranch = "engine";
        public const string ArmorBranch = "armor";
        public const string EmitterBranch = "emitter";
        public const string ExpansionBranch = "expansion";

        private static readonly int[] _slotCosts = { 5000, 15000, 40000 };

        private static readonly string[] _branches = { EngineBranch, ArmorBranch, EmitterBranch, ExpansionBranch };

        /// <summary>
        /// Per-branch deltas by frame class (fighter/transport/capital), per the piece-8 table.
        /// </summary>
        private static (int hull, int threshold, int shieldRating, int evasion) GetBranchDeltas(string branch, ShipFrameClass frameClass)
        {
            return branch switch
            {
                ArmorBranch => frameClass switch
                {
                    ShipFrameClass.Fighter => (15, 3, 0, 0),
                    ShipFrameClass.Transport => (30, 4, 0, 0),
                    _ => (100, 8, 0, 0)
                },
                EmitterBranch => frameClass switch
                {
                    ShipFrameClass.Fighter => (0, 0, 3, 0),
                    ShipFrameClass.Transport => (0, 0, 4, 0),
                    _ => (0, 0, 8, 0)
                },
                EngineBranch => frameClass switch
                {
                    ShipFrameClass.Fighter => (0, 0, 0, 3),
                    ShipFrameClass.Transport => (0, 0, 0, 2),
                    _ => (0, 0, 0, 1)
                },
                _ => (0, 0, 0, 0)
            };
        }

        /// <summary>
        /// The number of bonus low-power (systems) nodes a ship's refits grant.
        /// </summary>
        public static int GetBonusLowPowerNodes(PlayerShip dbShip)
        {
            return dbShip?.Refits?.Count(x => x == ExpansionBranch) ?? 0;
        }

        /// <summary>
        /// The bonus movement-speed factor from engine refits (+5% per engine refit).
        /// </summary>
        public static float GetSpeedFactorBonus(PlayerShip dbShip)
        {
            return (dbShip?.Refits?.Count(x => x == EngineBranch) ?? 0) * 0.05f;
        }

        /// <summary>
        /// Applies the summed refit deltas onto a freshly re-derived ship status.
        /// Used by the migration recompute and by the refit purchase itself.
        /// </summary>
        public static void ApplyRefitDeltas(PlayerShip dbShip, ShipStatus status)
        {
            if (dbShip.Refits == null || dbShip.Refits.Count == 0)
                return;

            var frameClass = Space.GetFrameClass(status);
            foreach (var branch in dbShip.Refits)
            {
                var (hull, threshold, shieldRating, evasion) = GetBranchDeltas(branch, frameClass);
                status.MaxHull += hull;
                status.Hull += hull;
                status.DamageThreshold += threshold;
                status.MaxShield += shieldRating;
                status.Shield += shieldRating;
                status.Evasion += evasion;
            }
        }

        /// <summary>
        /// Performs a refit on the ship whose interior the owner is standing in.
        /// The ship must be docked (not underway); the next open Mk slot is filled, or -
        /// when all three are filled - the named branch replaces the highest slot's branch.
        /// </summary>
        public static void PerformRefit(uint player, string branch)
        {
            if (!_branches.Contains(branch))
            {
                SendMessageToPC(player, "Usage: /refit <engine|armor|emitter|expansion>");
                return;
            }

            var area = GetArea(player);
            var propertyId = Property.GetPropertyId(area);
            if (string.IsNullOrWhiteSpace(propertyId))
            {
                SendMessageToPC(player, "You must be aboard your ship to refit it.");
                return;
            }

            var dbProperty = DB.Get<WorldProperty>(propertyId);
            if (dbProperty == null || dbProperty.PropertyType != PropertyType.Starship)
            {
                SendMessageToPC(player, "You must be aboard your ship to refit it.");
                return;
            }

            var query = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyId, false);
            var dbShip = DB.Search(query).FirstOrDefault();
            if (dbShip == null)
            {
                SendMessageToPC(player, "You must be aboard your ship to refit it.");
                return;
            }

            var playerId = GetObjectUUID(player);
            if (dbShip.OwnerPlayerId != playerId)
            {
                SendMessageToPC(player, "Only the ship's owner can commission a refit.");
                return;
            }

            // The yard cannot work on a ship that is underway.
            for (var pilot = GetFirstPC(); GetIsObjectValid(pilot); pilot = GetNextPC())
            {
                if (!Space.IsPlayerInSpaceMode(pilot))
                    continue;

                var dbPilot = DB.Get<Player>(GetObjectUUID(pilot));
                if (dbPilot.ActiveShipId == dbShip.Id)
                {
                    SendMessageToPC(player, "The ship is underway. Dock before commissioning a refit.");
                    return;
                }
            }

            dbShip.Refits ??= new List<string>();
            var slot = Math.Min(dbShip.Refits.Count, 2);
            var replacing = dbShip.Refits.Count >= 3;

            var cost = _slotCosts[slot];
            if (dbShip.Status.CapitalShip)
                cost *= 5;

            if (GetGold(player) < cost)
            {
                SendMessageToPC(player, $"The Mk {slot + 1} refit costs {cost} credits.");
                return;
            }

            TakeGoldFromCreature(cost, player, true);

            if (replacing)
            {
                // Strip the old branch's deltas from the highest slot, then overwrite it.
                var frameClass = Space.GetFrameClass(dbShip.Status);
                var (hull, threshold, shieldRating, evasion) = GetBranchDeltas(dbShip.Refits[2], frameClass);
                dbShip.Status.MaxHull -= hull;
                dbShip.Status.Hull = Math.Min(dbShip.Status.Hull, dbShip.Status.MaxHull);
                dbShip.Status.DamageThreshold -= threshold;
                dbShip.Status.MaxShield -= shieldRating;
                dbShip.Status.Shield = Math.Min(dbShip.Status.Shield, dbShip.Status.MaxShield);
                dbShip.Status.Evasion -= evasion;
                dbShip.Refits[2] = branch;
            }
            else
            {
                dbShip.Refits.Add(branch);
            }

            var newClass = Space.GetFrameClass(dbShip.Status);
            var (nHull, nThreshold, nSR, nEvasion) = GetBranchDeltas(branch, newClass);
            dbShip.Status.MaxHull += nHull;
            dbShip.Status.Hull += nHull;
            dbShip.Status.DamageThreshold += nThreshold;
            dbShip.Status.MaxShield += nSR;
            dbShip.Status.Shield += nSR;
            dbShip.Status.Evasion += nEvasion;

            DB.Set(dbShip);

            SendMessageToPC(player, ColorToken.Cyan($"Mk {slot + 1} refit complete: {branch.ToUpper()}. ({cost} credits)"));
        }
    }
}
