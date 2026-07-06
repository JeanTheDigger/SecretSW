using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    /// <summary>
    /// Capital gating moved off Piloting: Starships now covers flown hulls only, and
    /// capital deeds require the new Leadership Capital Command certification. Anyone
    /// who had earned Starships 5 (the old capital gate) is granted Capital Command I
    /// free so no existing captain loses their bridge.
    /// </summary>
    public class _14_GrantCapitalCommand : PlayerMigrationBase
    {
        public override int Version => 14;

        public override void Migrate(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (!dbPlayer.Perks.ContainsKey(PerkType.Starships) ||
                dbPlayer.Perks[PerkType.Starships] < 5)
                return;
            if (dbPlayer.Perks.ContainsKey(PerkType.CapitalCommand))
                return;

            dbPlayer.Perks[PerkType.CapitalCommand] = 1;
            DB.Set(dbPlayer);

            SendMessageToPC(player, "Capital ships are now COMMANDED (Leadership), not flown. As an experienced captain, you have been granted Capital Command I free of charge.");
        }
    }
}
