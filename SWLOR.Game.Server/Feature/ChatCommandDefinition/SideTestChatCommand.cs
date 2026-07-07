using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// (DM) Test harness for the two-side hostility engine (Service/Side.cs). Lets a DM flip the current area
    /// into a Full-PvP two-side match, drop themselves/other players onto sides, tag a targeted creature as a
    /// side's allied NPC, and tear the whole thing down — to verify cross-side PCs read hostile, allied NPCs
    /// only attack the opposing side, and everything cleans up.
    /// </summary>
    public class SideTestChatCommand : IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            SideTest();

            return _builder.Build();
        }

        private void SideTest()
        {
            _builder.Create("sidetest")
                .Description("(DM) Two-side PvP test: /sidetest start | join <side> | npc <side> (targets a creature) | end")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) =>
                {
                    var area = GetArea(user);

                    if (args.Length == 0)
                    {
                        SendMessageToPC(user, "Usage: /sidetest start | join <side> | npc <side> | end");
                        return;
                    }

                    switch (args[0].ToLower())
                    {
                        case "start":
                            Side.StartMatch(area);
                            SendMessageToPC(user, "Two-side match started in this area (Full PvP + friendly fire).");
                            break;
                        case "join":
                            if (args.Length < 2)
                            {
                                SendMessageToPC(user, "Usage: /sidetest join <side>");
                                return;
                            }
                            if (!Side.HasMatch(area))
                            {
                                SendMessageToPC(user, "No active match here. Run /sidetest start first.");
                                return;
                            }
                            Side.AssignPlayer(area, user, args[1]);
                            SendMessageToPC(user, $"You joined side [{args[1]}].");
                            break;
                        case "npc":
                            if (args.Length < 2)
                            {
                                SendMessageToPC(user, "Usage: /sidetest npc <side> (target a creature first)");
                                return;
                            }
                            if (!Side.HasMatch(area))
                            {
                                SendMessageToPC(user, "No active match here. Run /sidetest start first.");
                                return;
                            }
                            if (!GetIsObjectValid(target) || GetObjectType(target) != ObjectType.Creature)
                            {
                                SendMessageToPC(user, "Target a creature to assign as an allied NPC.");
                                return;
                            }
                            // Put it on a neutral base faction (zero enemies) synchronously, then apply side relations.
                            ChangeToStandardFaction(target, StandardFaction.Commoner);
                            Side.AddNpc(area, target, args[1]);
                            SendMessageToPC(user, $"{GetName(target)} is now an allied NPC of side [{args[1]}].");
                            break;
                        case "end":
                            Side.EndMatch(area);
                            SendMessageToPC(user, "Two-side match ended; relations and PvP setting restored.");
                            break;
                        default:
                            SendMessageToPC(user, "Unknown. Use: start | join <side> | npc <side> | end.");
                            break;
                    }
                });
        }
    }
}
