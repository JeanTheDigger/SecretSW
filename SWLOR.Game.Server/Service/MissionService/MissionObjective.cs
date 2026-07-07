namespace SWLOR.Game.Server.Service.MissionService
{
    /// <summary>
    /// Base class for an in-memory, mission-scoped objective (NOT stored in the persistent quest
    /// journal — missions are ephemeral). Concrete objectives override the event hooks they care
    /// about; the Mission service routes game events (creature death, placeable death, ...) to every
    /// active objective and checks completion. Mirrors the clean IQuestObjective pattern from
    /// QuestService but keeps all state in memory.
    /// </summary>
    public abstract class MissionObjective
    {
        /// <summary>
        /// Whether this objective has been satisfied.
        /// </summary>
        public bool IsComplete { get; protected set; }

        /// <summary>
        /// A short, human-readable description including current progress (shown to players).
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Called when any creature dies. Objectives that track kills override this.
        /// </summary>
        public virtual void OnCreatureKilled(uint creature) { }

        /// <summary>
        /// Called when any placeable is destroyed. Objectives that track destruction override this.
        /// </summary>
        public virtual void OnPlaceableDestroyed(uint placeable) { }
    }
}
