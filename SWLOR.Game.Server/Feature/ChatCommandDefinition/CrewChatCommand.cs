using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// The crew station commands - v1 seats for passengers aboard a flying ship.
    /// Station terminal placeables are module content; these commands are the seats
    /// until that art exists.
    /// </summary>
    public class CrewChatCommand : IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            _builder.Create("turret")
                .Description("Mans the guns: fires the ship's turret battery (laser battery or quad laser must be fitted) at the pilot's target, using YOUR Agility, Ranged, and Perception. 6s cycle.")
                .Permissions(AuthorizationLevel.Player)
                .Action((user, target, location, args) =>
                {
                    SpaceCrew.FireTurret(user);
                });

            _builder.Create("shields")
                .Description("Mans engineering: recharges the ship's shield rating by 5 + 1 per 10 Engineering ranks. 12s cycle.")
                .Permissions(AuthorizationLevel.Player)
                .Action((user, target, location, args) =>
                {
                    SpaceCrew.RechargeShields(user);
                });

            _builder.Create("damagecontrol")
                .Description("Mans engineering: repairs the ship one step up the condition track. 18s cycle.")
                .Permissions(AuthorizationLevel.Player)
                .Action((user, target, location, args) =>
                {
                    SpaceCrew.DamageControl(user);
                });

            _builder.Create("flightmode")
                .Description("Sets your flight stance while piloting: /flightmode <attack|evasive|balanced>. Requires the Flight Stances perk.")
                .Permissions(AuthorizationLevel.Player)
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                        return "Usage: /flightmode <attack|evasive|balanced>";

                    var mode = args[0].ToLower();
                    if (mode != "attack" && mode != "evasive" && mode != "balanced")
                        return "Stance must be 'attack', 'evasive', or 'balanced'.";

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var stance = args[0].ToLower() switch
                    {
                        "attack" => FlightStanceType.Attack,
                        "evasive" => FlightStanceType.Evasive,
                        _ => FlightStanceType.Balanced
                    };
                    Space.SetFlightStance(user, stance);
                });

            _builder.Create("order")
                .Description("Issues a fleet order from a capital ship's bridge: /order <alpha|brace|wolfpack>. Requires the matching command doctrine.")
                .Permissions(AuthorizationLevel.Player)
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                        return "Usage: /order <alpha|brace|wolfpack>";

                    var order = args[0].ToLower();
                    if (order != "alpha" && order != "brace" && order != "wolfpack")
                        return "Order must be 'alpha', 'brace', or 'wolfpack'.";

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    Space.IssueOrder(user, args[0].ToLower());
                });

            _builder.Create("attune")
                .Description("Attunes you to the weapon in your main hand (requires the Signature Weapon perk). The bond grants bonus damage with that item alone - and it outlives you.")
                .Permissions(AuthorizationLevel.Player)
                .Action((user, target, location, args) =>
                {
                    SignatureWeapon.Attune(user);
                });

            return _builder.Build();
        }
    }
}
