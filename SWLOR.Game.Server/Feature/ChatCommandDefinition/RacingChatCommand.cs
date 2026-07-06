using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Swoop racing commands. Tracks are authored in the toolset by placing waypoints
    /// tagged RACE_WP_1..N in an area.
    /// </summary>
    public class RacingChatCommand : IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            _builder.Create("race")
                .Description("Starts a timed run on this area's race course (or cancels your active run). Hit every checkpoint in order to set a time.")
                .Permissions(AuthorizationLevel.Player)
                .Action((user, target, location, args) =>
                {
                    SwoopRacing.ToggleRace(user);
                });

            return _builder.Build();
        }
    }
}
