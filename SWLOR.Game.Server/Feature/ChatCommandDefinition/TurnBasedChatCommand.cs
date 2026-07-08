using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Chat commands for the turn-based tactical combat mode (increment 1).
    /// /tbstart and /tbend are DM controls; /endturn is used by the active player to pass.
    /// </summary>
    public class TurnBasedChatCommand : IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            StartEncounter();
            EndEncounter();
            EndTurn();
            Move();
            Status();

            return _builder.Build();
        }

        private void Status()
        {
            _builder.Create("tbstatus")
                .Description("Shows the current turn-based initiative order, round, and your remaining move/action.")
                .Permissions(AuthorizationLevel.Player, AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) => TurnBased.SendStatus(user));
        }

        private void Move()
        {
            _builder.Create("tbmove")
                .Description("Move on your turn: enters targeting mode to click a destination within your budget.")
                .Permissions(AuthorizationLevel.Player, AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) => TurnBased.BeginMove(user));
        }

        private void StartEncounter()
        {
            _builder.Create("tbstart")
                .Description("(DM) Starts a turn-based encounter with everyone in your current area.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) => TurnBased.StartEncounter(GetArea(user)));
        }

        private void EndEncounter()
        {
            _builder.Create("tbend")
                .Description("(DM) Ends the turn-based encounter in your current area.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) => TurnBased.EndEncounter(GetArea(user)));
        }

        private void EndTurn()
        {
            _builder.Create("endturn")
                .Description("Ends your turn in turn-based combat.")
                .Permissions(AuthorizationLevel.Player, AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    if (!TurnBased.IsActiveTurn(user))
                    {
                        SendMessageToPC(user, "It is not your turn.");
                        return;
                    }

                    TurnBased.EndTurn(user);
                });
        }
    }
}
