using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;

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

            return _builder.Build();
        }
    }
}
