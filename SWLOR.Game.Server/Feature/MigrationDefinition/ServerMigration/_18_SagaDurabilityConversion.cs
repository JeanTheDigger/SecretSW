using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    /// <summary>
    /// Converts every persisted player ship to the Saga-style durability model: shield
    /// pools become flat shield RATINGS, and each frame receives its damage threshold
    /// and a clean condition track. Stats re-derive from ship detail + item bonuses,
    /// exactly like migrations 15 and 17 did.
    /// </summary>
    public class _18_SagaDurabilityConversion : ServerMigrationBase, IServerMigration
    {
        public int Version => 18;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

        public void Migrate()
        {
            RecalculateAllShipStats();
        }
    }
}
