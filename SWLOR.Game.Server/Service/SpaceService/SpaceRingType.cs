namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The three rings of space, mirroring the gathering rings. Stakes scale outward:
    /// safe orbits cost a repair bill, contested lanes carry the module-loss economy,
    /// and deep space wagers the frame itself. Areas declare their ring with the
    /// SPACE_RING local variable; anything unset is a safe orbit.
    /// </summary>
    public enum SpaceRingType
    {
        SafeOrbit = 1,
        ContestedLane = 2,
        DeepSpace = 3
    }
}
