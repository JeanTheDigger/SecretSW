namespace SWLOR.Game.Server.Service.AbilityService
{
    public enum AbilityToggleType
    {
        Invalid = 0,
        Dash = 1,
        // Strong Style has been removed from the game (its identity moves to Form V / Djem So).
        // Values retained for serialized Player.AbilityToggles data integrity.
        StrongStyleSaberstaff = 2,
        StrongStyleLightsaber = 3
    }
}
