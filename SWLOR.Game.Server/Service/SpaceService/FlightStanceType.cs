namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A pilot's flight stance - the light, class-neutral cousin of the ground stance
    /// system. Attack runs hot (+10 accuracy, -10 evasion), Evasive flies loose
    /// (-10 accuracy, +10 evasion), Balanced is the neutral default.
    /// </summary>
    public enum FlightStanceType
    {
        Balanced = 0,
        Attack = 1,
        Evasive = 2
    }
}
