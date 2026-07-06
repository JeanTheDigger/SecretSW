namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The scale class of a ship weapon. Scale penalties attach to the WEAPON, not the
    /// hull: standard weapons track anything (and find bigger targets easier to hit);
    /// capital-grade batteries barely track fighters at all - a capital's real
    /// anti-fighter defense is its gunners on standard guns.
    /// </summary>
    public enum ShipWeaponScale
    {
        Standard = 0,
        CapitalGrade = 1
    }

    /// <summary>
    /// The size class of a ship frame, derived from its definition until the frame
    /// catalog is authored per-hull.
    /// </summary>
    public enum ShipFrameClass
    {
        Fighter = 0,
        Transport = 1,
        Capital = 2
    }
}
