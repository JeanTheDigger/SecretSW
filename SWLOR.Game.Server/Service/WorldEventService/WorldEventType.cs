namespace SWLOR.Game.Server.Service.WorldEventService
{
    public enum WorldEventType
    {
        Invalid = 0,

        // PvE event zones: endgame SP flows from event objectives (flagged creatures).
        PvE = 1,

        // PvP event zones: lethal, endgame SP flows from player kills.
        PvP = 2
    }
}
