using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.MissionService
{
    /// <summary>
    /// Base class for an in-memory, mission-scoped objective (NOT stored in the persistent quest
    /// journal — missions are ephemeral). Concrete objectives override the event hooks they care
    /// about; the Mission service routes game events (creature death, placeable death, item acquire,
    /// heartbeat) to every active objective and checks completion/failure. Mirrors the clean
    /// IQuestObjective pattern from QuestService but keeps all state in memory.
    /// </summary>
    public abstract class MissionObjective
    {
        /// <summary>
        /// Whether this objective has been satisfied.
        /// </summary>
        public bool IsComplete { get; protected set; }

        /// <summary>
        /// Whether this objective has failed (e.g. a protected NPC died). A failed objective fails the mission.
        /// </summary>
        public bool Failed { get; protected set; }

        /// <summary>
        /// True if this objective is a fail-condition rider rather than a goal to complete: it exists only
        /// to be able to FAIL the mission (e.g. "keep the VIP alive"), and must NOT gate mission success.
        /// Mission success is evaluated over the non-fail-condition objectives only, but ANY objective that
        /// enters the Failed state fails the whole mission. Defaults to false (a normal completable goal).
        /// </summary>
        public virtual bool IsFailCondition => false;

        /// <summary>
        /// A short, human-readable description including current progress (shown to players).
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Called when any creature dies. Objectives that track kills / protected NPCs override this.
        /// </summary>
        public virtual void OnCreatureKilled(uint creature) { }

        /// <summary>
        /// Called when any placeable is destroyed. Objectives that track destruction override this.
        /// </summary>
        public virtual void OnPlaceableDestroyed(uint placeable) { }

        /// <summary>
        /// Called when a creature acquires an item. Objectives that track item pickup override this.
        /// </summary>
        public virtual void OnItemAcquired(uint item, uint acquiredBy) { }

        /// <summary>
        /// Periodic tick (SWLOR heartbeat) for proximity/timed objectives. 'playersInArea' is the set
        /// of PCs currently in the mission area.
        /// </summary>
        public virtual void OnHeartbeat(uint area, IReadOnlyList<uint> playersInArea) { }
    }
}
