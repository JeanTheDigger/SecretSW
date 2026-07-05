using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    /// <summary>
    /// Shields, Beast Mastery, and the Strong Style toggles have been removed from the game.
    /// Their perk definitions no longer exist, so refund amounts come from the final published
    /// price tables below. Affected characters also receive a rebuild token.
    /// </summary>
    public class _13_RemoveShieldsBeastsStrongStyle : PlayerMigrationBase
    {
        public override int Version => 13;

        // Per-level SP prices of every removed player perk, in level order.
        // Beast-side perks (Claw, Bite, etc.) lived on the beast's own SP pool and need no player refund.
        private static readonly Dictionary<PerkType, int[]> _removedPerkPrices = new()
        {
            // Shields
            { PerkType.ShieldProficiency, new[] { 1, 1, 1, 1, 1 } },
            { PerkType.ShieldMaster, new[] { 4 } },
            { PerkType.ShieldBash, new[] { 2, 3, 3 } },
            { PerkType.Bulwark, new[] { 3 } },
            { PerkType.ShieldResistance, new[] { 2, 3 } },
            { PerkType.Alacrity, new[] { 2 } },
            { PerkType.Clarity, new[] { 2 } },

            // Strong Style
            { PerkType.StrongStyleLightsaber, new[] { 1 } },
            { PerkType.StrongStyleSaberstaff, new[] { 1 } },

            // Beast Mastery
            { PerkType.Tame, new[] { 3, 3, 4, 5, 5 } },
            { PerkType.Reward, new[] { 1, 2, 2 } },
            { PerkType.Stabling, new[] { 1, 1, 1, 1, 1 } },
            { PerkType.Snarl, new[] { 2 } },
            { PerkType.Growl, new[] { 2 } },
            { PerkType.SoothePet, new[] { 2 } },
            { PerkType.ReviveBeast, new[] { 1, 2, 3 } },
            { PerkType.DNAManipulation, new[] { 2, 2, 2, 3, 3 } },
            { PerkType.IncubationProcessing, new[] { 2, 2, 3, 3 } },
            { PerkType.ErraticGenius, new[] { 2, 3, 3 } },
            { PerkType.IncubationManagement, new[] { 2, 3 } },
        };

        private static readonly FeatType[] _removedFeats =
        {
            // Engine shield proficiency (previously granted to everyone at initialization)
            FeatType.ShieldProficiency,

            // Shield perks
            FeatType.ShieldProficiency1,
            FeatType.ShieldProficiency2,
            FeatType.ShieldProficiency3,
            FeatType.ShieldProficiency4,
            FeatType.ShieldProficiency5,
            FeatType.ShieldMaster,
            FeatType.ShieldBash1,
            FeatType.ShieldBash2,
            FeatType.ShieldBash3,
            FeatType.Bulwark,

            // Strong Style
            FeatType.StrongStyleLightsaber,
            FeatType.StrongStyleSaberstaff,

            // Beast Mastery
            FeatType.Tame,
            FeatType.CallBeast,
            FeatType.Reward1,
            FeatType.Reward2,
            FeatType.Reward3,
            FeatType.Snarl,
            FeatType.Growl,
            FeatType.SoothePet,
            FeatType.ReviveBeast1,
            FeatType.ReviveBeast2,
            FeatType.ReviveBeast3,
        };

        public override void Migrate(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var refundedAny = false;

            foreach (var (perkType, prices) in _removedPerkPrices)
            {
                if (!dbPlayer.Perks.ContainsKey(perkType))
                    continue;

                var perkLevel = dbPlayer.Perks[perkType];
                var refundAmount = 0;
                for (var level = 1; level <= perkLevel && level <= prices.Length; level++)
                {
                    refundAmount += prices[level - 1];
                }

                dbPlayer.UnallocatedSP += refundAmount;
                dbPlayer.Perks.Remove(perkType);
                refundedAny = true;

                Log.Write(LogGroup.Migration, $"{dbPlayer.Name} ({dbPlayer.Id}) refunded {refundAmount} SP for removed perk '{perkType}'.");
                SendMessageToPC(player, $"Perk '{perkType}' has been removed from the game. You reclaimed {refundAmount} SP.");
            }

            // Beasts no longer exist; clear the orphaned reference.
            dbPlayer.ActiveBeastId = string.Empty;

            DB.Set(dbPlayer);

            foreach (var feat in _removedFeats)
            {
                CreaturePlugin.RemoveFeat(player, feat);
            }

            // Shield Resistance persisted base saving throws onto the character file; reset them.
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Fortitude, 0);
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Will, 0);
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Reflex, 0);

            if (refundedAny)
            {
                CreateItemOnObject("rebuild_token", player);
                SendMessageToPC(player, "You received a Rebuild Token because systems you had invested in were removed from the game.");
            }
        }
    }
}
