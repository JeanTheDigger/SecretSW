namespace SWLOR.Game.Server.Service.WorldEventService
{
    /// <summary>
    /// The power bracket of a world event. Brackets keep Phase-2 entry survivable:
    /// fresh Knights and established Masters fight in separate events, except in
    /// Open events (DM specials and the Trials ceremonies), which admit everyone
    /// eligible to be in an event zone at all.
    /// </summary>
    public enum WorldEventBracket
    {
        Open = 0,
        Knight = 1,
        Master = 2
    }
}
