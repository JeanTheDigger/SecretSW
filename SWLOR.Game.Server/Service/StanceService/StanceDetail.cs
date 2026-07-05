using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.StanceService
{
    /// <summary>
    /// The stat package a combat stance (lightsaber form) applies while active.
    /// All values are static per perk level; the hot path reads a cached instance.
    /// </summary>
    public class StanceDetail
    {
        public string Name { get; set; }

        // Added to accuracy while active.
        public int AccuracyMod { get; set; }

        // Added to evasion while active.
        public int EvasionMod { get; set; }

        // When set, the weapon's damage stat re-maps to this ability (Form V only).
        public AbilityType DamageStatOverride { get; set; } = AbilityType.Invalid;

        // Flat DMG added to weapon attacks.
        public int FlatDMG { get; set; }

        // DMG added equal to (MGT modifier * this) / 2.
        public int MgtModDMGHalves { get; set; }

        // Added to critical chance (percentage points).
        public int CritMod { get; set; }

        // Added to physical defense.
        public int DefensePhysicalMod { get; set; }

        // Added to the saber blaster-deflection chance.
        public int DeflectMod { get; set; }

        // FP restored on each natural regeneration tick.
        public int FPRegenPerTick { get; set; }

        // Flat DMG removed from weapon attacks (defensive forms trade damage away).
        public int DamagePenalty { get; set; }
    }
}
