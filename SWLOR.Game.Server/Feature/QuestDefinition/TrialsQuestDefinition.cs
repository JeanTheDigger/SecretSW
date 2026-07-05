using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    /// <summary>
    /// The Trials - the ceremony gate between Phase 1 and Phase 2. One mechanical quest with
    /// per-class ceremony flavor: Force-Sensitives are knighted, Standard characters are
    /// commissioned. Unlocks at the Phase-1 cap; completing it sets the knighthood flag,
    /// opening ranks 51-100, event SP, and perma-death exposure.
    /// A DM starts the trial (/trialsbegin) and spawns the Trials Guardian - any creature
    /// with its QUEST_NPC_GROUP_ID local set - typically inside an Open-bracket event.
    /// </summary>
    public class TrialsQuestDefinition : IQuestListDefinition
    {
        public const string QuestId = "trials_knighthood";

        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            _builder.Create(QuestId, "The Trials")
                .PrerequisiteTotalSP(Skill.Phase1Cap)

                .AddState()
                .SetStateJournalText("Your training has carried you as far as training can. What remains must be proven, not practiced: face the Trials Guardian and prevail. The staff will arrange your trial.")
                .AddKillObjective(NPCGroupType.TrialsGuardian, 1)

                .OnCompleteAction((player, source) =>
                {
                    var playerId = GetObjectUUID(player);
                    var dbPlayer = DB.Get<Player>(playerId);
                    if (dbPlayer.HasCompletedTrials)
                        return;

                    dbPlayer.HasCompletedTrials = true;
                    DB.Set(dbPlayer);

                    var isForceSensitive = dbPlayer.CharacterType == CharacterType.ForceSensitive;
                    SendMessageToPC(player, ColorToken.Green(isForceSensitive
                        ? "You kneel a Padawan and rise a Knight. The path to mastery - and its dangers - are open to you."
                        : "Your commission is confirmed in the only court that matters: the field. The path to mastery - and its dangers - are open to you."));

                    BroadcastCeremony($"{GetName(player)} has passed the Trials.");
                    Log.Write(LogGroup.Server, $"TRIALS PASSED: {GetName(player)} ({playerId})");
                });

            return _builder.Build();
        }

        private static void BroadcastCeremony(string message)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                SendMessageToPC(player, ColorToken.Cyan(message));
            }
        }
    }
}
