using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    /// <summary>
    /// Patrol and bounty contracts - the repeatable space objectives that make Phase-1
    /// space a game rather than a hallway (the third Piloting XP source, alongside NPC
    /// kills and mining). Rewards pay gold plus Piloting XP through the ordinary skill
    /// pipeline, so the Phase-1 daily allowance governs them like everything else.
    /// Contract NPCs are any ship blueprints carrying the matching QUEST_NPC_GROUP_ID
    /// local (Space Pirates = 68, Raider Ace = 69); starport contract terminals hook
    /// these via the standard quest snippets at map time.
    /// </summary>
    public class PatrolContractQuestDefinition : IQuestListDefinition
    {
        private readonly QuestBuilder _builder = new();

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            PirateCull();
            LaneSweep();
            RaiderAce();

            return _builder.Build();
        }

        private void PirateCull()
        {
            _builder.Create("patrol_pirate_cull", "Patrol Contract: Pirate Cull")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("The lanes are crawling again. Destroy 5 pirate ships and report back for payment.")
                .AddKillObjective(NPCGroupType.SpacePirate, 5)

                .AddGoldReward(500)
                .OnCompleteAction((player, source) =>
                {
                    Skill.GiveSkillXP(player, SkillType.Piloting, 400);
                });
        }

        private void LaneSweep()
        {
            _builder.Create("patrol_lane_sweep", "Convoy Contract: Lane Sweep")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("A convoy is staging and the insurance underwriters are nervous. Sweep 8 pirate ships from the lanes before it departs.")
                .AddKillObjective(NPCGroupType.SpacePirate, 8)

                .AddGoldReward(1200)
                .OnCompleteAction((player, source) =>
                {
                    Skill.GiveSkillXP(player, SkillType.Piloting, 1000);
                });
        }

        private void RaiderAce()
        {
            _builder.Create("patrol_raider_ace", "Bounty Contract: The Raider Ace")
                .IsRepeatable()

                .AddState()
                .SetStateJournalText("A named ace is picking off haulers along the lanes. Find them and end their career.")
                .AddKillObjective(NPCGroupType.SpaceRaiderAce, 1)

                .AddGoldReward(1000)
                .OnCompleteAction((player, source) =>
                {
                    Skill.GiveSkillXP(player, SkillType.Piloting, 800);
                });
        }
    }
}
