namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The two ship weapon families. Energy is fully reduced by the target's shield rating
    /// (a hit below the rating does nothing and costs the shield nothing); Ordnance is
    /// reduced by only half the rating, always degrades it, and checks its RAW damage
    /// against the target's damage threshold - torpedoes stagger everything.
    /// </summary>
    public enum ShipDamageFamily
    {
        Energy = 0,
        Ordnance = 1
    }
}
