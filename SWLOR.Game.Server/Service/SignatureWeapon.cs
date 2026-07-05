using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The Signature Weapon bond: a Phase-2 perk (both classes) that attunes a character
    /// to one specific weapon ITEM. The bond grants bonus damage only with that item, and
    /// when its wielder perma-dies in an event, the weapon survives as a lootable
    /// HEIRLOOM at the death site - event deaths generate stories and economy.
    /// Bond state is cached for the native damage hot path and rebuilt on login/attune.
    /// </summary>
    public static class SignatureWeapon
    {
        public const string SignatureItemVariable = "SIGNATURE_ITEM_ID";

        // playerId (uuid string) -> (attuned bond mark, bonus damage)
        private static readonly Dictionary<string, (int, int)> _bonds = new();

        /// <summary>
        /// Retrieves the signature damage bonus for an attacker/weapon pair, from cache.
        /// Hot-path safe. Returns 0 unless the wielded item carries the attuned bond mark.
        /// </summary>
        public static int GetBonus(string attackerPlayerId, int weaponBondMark)
        {
            if (weaponBondMark <= 0)
                return 0;

            return _bonds.TryGetValue(attackerPlayerId, out var bond) && bond.Item1 == weaponBondMark
                ? bond.Item2
                : 0;
        }

        /// <summary>
        /// Attunes the player to the weapon in their main hand. Re-attuning moves the
        /// bond: the old weapon's mark stays on the item but no longer matches.
        /// </summary>
        public static void Attune(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var level = Perk.GetPerkLevel(player, PerkType.SignatureWeapon);
            if (level <= 0)
            {
                SendMessageToPC(player, "You have not learned the Signature Weapon bond. (General perk: Signature Weapon)");
                return;
            }

            var weapon = GetItemInSlot(InventorySlot.RightHand, player);
            if (!GetIsObjectValid(weapon))
            {
                SendMessageToPC(player, "A weapon must be in your main hand to attune to it.");
                return;
            }

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var bondMark = Random.Next(1, int.MaxValue);
            SetLocalInt(weapon, SignatureItemVariable, bondMark);
            dbPlayer.SignatureItemId = bondMark.ToString();
            DB.Set(dbPlayer);

            Rebuild(player, dbPlayer);
            SendMessageToPC(player, ColorToken.Cyan($"You attune to {GetName(weapon)}. It is part of you now - and it will outlive you."));
        }

        private static void Rebuild(uint player, Player dbPlayer)
        {
            var playerId = GetObjectUUID(player);
            var level = Perk.GetPerkLevel(player, PerkType.SignatureWeapon);

            if (level <= 0 || string.IsNullOrEmpty(dbPlayer.SignatureItemId) ||
                !int.TryParse(dbPlayer.SignatureItemId, out var bondMark))
            {
                _bonds.Remove(playerId);
                return;
            }

            _bonds[playerId] = (bondMark, level * 3);
        }

        /// <summary>
        /// Drops a perma-dead character's attuned weapon at their death site as a
        /// lootable heirloom. Called by the perma-death pipeline.
        /// </summary>
        public static void DropHeirloom(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null || string.IsNullOrEmpty(dbPlayer.SignatureItemId))
                return;

            if (!int.TryParse(dbPlayer.SignatureItemId, out var bondMark))
                return;

            var deathLocation = GetLocation(player);
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (GetLocalInt(item, SignatureItemVariable) != bondMark)
                    continue;

                CopyObject(item, deathLocation);
                DestroyObject(item);
                Messaging.SendMessageNearbyToPlayers(player,
                    ColorToken.Orange($"{GetName(player)}'s signature weapon falls from their hands - an heirloom for whoever dares claim it."));
                break;
            }

            // Check the equipped slots as well - the bond is usually in hand at the end.
            foreach (var slot in new[] { InventorySlot.RightHand, InventorySlot.LeftHand })
            {
                var equipped = GetItemInSlot(slot, player);
                if (!GetIsObjectValid(equipped))
                    continue;
                if (GetLocalInt(equipped, SignatureItemVariable) != bondMark)
                    continue;

                CopyObject(equipped, deathLocation);
                DestroyObject(equipped);
                Messaging.SendMessageNearbyToPlayers(player,
                    ColorToken.Orange($"{GetName(player)}'s signature weapon falls from their hands - an heirloom for whoever dares claim it."));
                break;
            }
        }

        /// <summary>
        /// Rebuilds the bond cache when a player logs in.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void LoadOnEnter()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            Rebuild(player, dbPlayer);
        }

        /// <summary>
        /// Clears cached bond state when a player leaves the server.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearOnExit()
        {
            _bonds.Remove(GetObjectUUID(GetExitingObject()));
        }
    }
}
