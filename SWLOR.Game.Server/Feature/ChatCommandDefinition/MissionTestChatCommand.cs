using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.MissionService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// (DM) Test harness for the mission objective framework — lets a DM spin up objectives in the
    /// current area and watch them track/complete against tagged creatures/placeables.
    /// </summary>
    public class MissionTestChatCommand : IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            ObjectiveTest();

            return _builder.Build();
        }

        private void ObjectiveTest()
        {
            _builder.Create("objtest")
                .Description("(DM) Mission objective test: /objtest kill <tag> <count> | boss <tag> | destroy <tag> | end")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) =>
                {
                    var area = GetArea(user);

                    if (args.Length == 0)
                    {
                        SendMessageToPC(user, "Usage: /objtest kill <tag> <count> | boss <tag> | destroy <tag> | reach <tag> [radius] | retrieve <itemTag> <extractTag> | end");
                        return;
                    }

                    switch (args[0].ToLower())
                    {
                        case "kill":
                            if (args.Length < 3 || !int.TryParse(args[2], out var count))
                            {
                                SendMessageToPC(user, "Usage: /objtest kill <tag> <count>");
                                return;
                            }
                            Mission.AddObjective(area, new ExterminateObjective(args[1], count));
                            break;
                        case "boss":
                            if (args.Length < 2)
                            {
                                SendMessageToPC(user, "Usage: /objtest boss <tag>");
                                return;
                            }
                            Mission.AddObjective(area, new AssassinateObjective(args[1]));
                            break;
                        case "destroy":
                            if (args.Length < 2)
                            {
                                SendMessageToPC(user, "Usage: /objtest destroy <tag>");
                                return;
                            }
                            Mission.AddObjective(area, new DestroyObjective(args[1]));
                            break;
                        case "reach":
                            if (args.Length < 2)
                            {
                                SendMessageToPC(user, "Usage: /objtest reach <destinationTag> [radius]");
                                return;
                            }
                            var radius = args.Length >= 3 && float.TryParse(args[2], out var r) ? r : 3.0f;
                            Mission.AddObjective(area, new ReachObjective(args[1], radius));
                            break;
                        case "retrieve":
                            if (args.Length < 3)
                            {
                                SendMessageToPC(user, "Usage: /objtest retrieve <itemTag> <extractTag>");
                                return;
                            }
                            Mission.AddObjective(area, new RetrieveObjective(args[1], args[2]));
                            break;
                        case "end":
                            Mission.EndRun(area);
                            break;
                        default:
                            SendMessageToPC(user, "Unknown type. Use: kill | boss | destroy | reach | retrieve | end.");
                            break;
                    }
                });
        }
    }
}
