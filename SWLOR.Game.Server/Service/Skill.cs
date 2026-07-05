using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Feature.StatusEffectDefinition.StatusEffectData;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Service
{
    public static partial class Skill
    {
        /// <summary>
        /// The maximum number of skill points obtainable through the Phase 1 (daily activity) path.
        /// Reaching this total marks the boundary between Phase 1 and Phase 2 progression.
        /// </summary>
        public const int Phase1Cap = 350;

        /// <summary>
        /// The absolute maximum number of skill points a single character can ever acquire.
        /// Points between Phase1Cap and AbsoluteCap come only from the endgame paths (events, deep content).
        /// </summary>
        public const int AbsoluteCap = 700;

        /// <summary>
        /// The per-skill rank ceiling while a character is still in Phase 1 (below Phase1Cap total SP).
        /// </summary>
        public const int Phase1PerSkillCap = 50;

        /// <summary>
        /// The maximum number of skill ranks that can be converted from activity XP per UTC day in Phase 1.
        /// XP earned beyond the daily allowance is banked and converts on later days.
        /// </summary>
        public const int DailyRankLimit = 5;

        /// <summary>
        /// This is the maximum number of AP a single character can earn in total. This must be evenly divisible into AbsoluteCap.
        /// </summary>
        public static int APCap { get; } = AbsoluteCap / 10;

        /// <summary>
        /// Retrieves the rank ceiling currently in effect for a skill, based on the player's phase.
        /// Non-cap skills (languages) always use their full MaxRank. Cap-contributing skills are
        /// limited to Phase1PerSkillCap until the player crosses the Phase 1 boundary.
        /// </summary>
        /// <param name="dbPlayer">The player entity to evaluate.</param>
        /// <param name="skill">The skill to evaluate.</param>
        /// <returns>The effective maximum rank for this player and skill.</returns>
        public static int GetEffectiveMaxRank(Player dbPlayer, SkillType skill)
        {
            var details = GetSkillDetails(skill);

            if (!details.ContributesToSkillCap)
                return details.MaxRank;

            return dbPlayer.TotalSPAcquired >= Phase1Cap
                ? details.MaxRank
                : Math.Min(Phase1PerSkillCap, details.MaxRank);
        }

        /// <summary>
        /// Gives XP towards a specific skill to a player.
        /// </summary>
        /// <param name="player">The player to give XP to.</param>
        /// <param name="skill">The type of skill to give XP towards.</param>
        /// <param name="xp">The amount of XP to give.</param>
        /// <param name="ignoreBonuses">If true, bonuses from food and other sources will NOT be applied.</param>
        /// <param name="applyHenchmanPenalty">If true, a penalty will apply if the player has a henchman active (droid, pet, etc.)</param>
        public static void GiveSkillXP(
            uint player, 
            SkillType skill, 
            int xp, 
            bool ignoreBonuses = false,
            bool applyHenchmanPenalty = true)
        {
            if (skill == SkillType.Invalid || xp <= 0 || !GetIsPC(player) || GetIsDM(player)) return;

            var modifiedSkills = new List<SkillType>();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var details = GetSkillDetails(skill);
            var pcSkill = dbPlayer.Skills[skill];
            var requiredXP = GetRequiredXP(pcSkill.Rank);
            var receivedRankUp = false;
            var bonusPercentage = 0f;

            if (!ignoreBonuses)
            {
                // Bonus for positive Social modifier.
                var social = GetAbilityScore(player, AbilityType.Social);
                if (social > 0)
                    bonusPercentage += social * 0.025f;

                // Food bonus
                var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(player, StatusEffectType.Food);
                if (foodEffect != null)
                {
                    bonusPercentage += foodEffect.XPBonusPercent * 0.01f;
                }

                // DM bonus
                bonusPercentage += dbPlayer.DMXPBonus * 0.01f;

                // Dedication bonus
                if (StatusEffect.HasStatusEffect(player, StatusEffectType.Dedication))
                {
                    var source = StatusEffect.GetEffectData<uint>(player, StatusEffectType.Dedication);

                    if (GetIsObjectValid(source))
                    {
                        var effectiveLevel = Perk.GetPerkLevel(source, PerkType.Dedication);
                        social = GetAbilityScore(source, AbilityType.Social);
                        bonusPercentage += (10 + effectiveLevel * social) * 0.01f;
                    }
                }

                // Apply bonuses
                xp += (int)(xp * bonusPercentage);
            }

            // 30% penalty applied if a Henchman is active.
            if (applyHenchmanPenalty)
            {
                const float HenchmanPenalty = 0.3f;

                xp -= (int)(xp * HenchmanPenalty);
            }

            var debtRemoved = 0;
            if (dbPlayer.XPDebt > 0)
            {
                if (xp >= dbPlayer.XPDebt)
                {
                    debtRemoved = dbPlayer.XPDebt;
                    xp -= dbPlayer.XPDebt;
                }
                else
                {
                    debtRemoved = xp;
                    xp = 0;
                }
            }

            if (debtRemoved > 0)
            {
                dbPlayer.XPDebt -= debtRemoved;
                SendMessageToPC(player, $"{debtRemoved} XP was removed from your debt. (Remaining: {dbPlayer.XPDebt})");
            }

            if (xp <= 0)
            {
                DB.Set(dbPlayer);
                return;
            }
            
            // Reset the daily rank allowance at midnight UTC. The lazy check means offline
            // players reset correctly the first time they earn XP on a new day.
            if (dbPlayer.LastDailyReset.Date < DateTime.UtcNow.Date)
            {
                dbPlayer.RanksGainedToday = 0;
                dbPlayer.LastDailyReset = DateTime.UtcNow;
            }

            var effectiveMaxRank = GetEffectiveMaxRank(dbPlayer, skill);
            var inPhase1 = dbPlayer.TotalSPAcquired < Phase1Cap;

            SendMessageToPC(player, $"You earned {details.Name} skill experience. ({xp})");
            pcSkill.XP += xp;

            // Skill is at its effective rank ceiling. No additional XP can be banked.
            if (pcSkill.Rank >= effectiveMaxRank)
            {
                pcSkill.XP = 0;
            }

            while (pcSkill.XP >= requiredXP)
            {
                if (pcSkill.Rank >= effectiveMaxRank)
                    break;

                if (details.ContributesToSkillCap)
                {
                    // Past the Phase 1 boundary, activity XP no longer converts to ranks.
                    // Endgame ranks come from the event and deep-content paths (GiveEndgameSP).
                    // Banked XP is retained.
                    if (!inPhase1)
                        break;

                    // The daily allowance gates Phase 1 conversions. Banked XP carries to the next day.
                    if (dbPlayer.RanksGainedToday >= DailyRankLimit)
                        break;
                }

                receivedRankUp = true;
                pcSkill.XP -= requiredXP;

                if (details.ContributesToSkillCap)
                {
                    dbPlayer.UnallocatedSP++;
                    dbPlayer.TotalSPAcquired++;
                    dbPlayer.RanksGainedToday++;
                }

                pcSkill.Rank++;
                FloatingTextStringOnCreature($"Your {details.Name} skill level increased to rank {pcSkill.Rank}!", player, false);

                requiredXP = GetRequiredXP(pcSkill.Rank);
                if (pcSkill.Rank >= effectiveMaxRank)
                {
                    pcSkill.XP = 0;
                }

                dbPlayer.Skills[skill] = pcSkill;

                if (details.ContributesToSkillCap)
                {
                    ApplyAbilityPoint(player, dbPlayer);
                }

                inPhase1 = dbPlayer.TotalSPAcquired < Phase1Cap;
            }

            // XP beyond the next rank's requirement is retained (banked) so it can convert when
            // the daily allowance resets. Only a skill sitting at its effective ceiling discards XP.
            if (dbPlayer.Skills[skill].Rank >= effectiveMaxRank && dbPlayer.Skills[skill].XP > 0)
            {
                dbPlayer.Skills[skill].XP = 0;
            }

            DB.Set(dbPlayer);

            modifiedSkills.Add(skill);
            Gui.PublishRefreshEvent(player, new SkillXPRefreshEvent(modifiedSkills));

            // Send out an event signifying that a player has received a skill rank increase.
            if(receivedRankUp)
            {
                EventsPlugin.SignalEvent("SWLOR_GAIN_SKILL_POINT", player);
            }
        }

        /// <summary>
        /// Grants endgame skill ranks directly. This is the Phase 2 progression path, fed by
        /// events and deep content. It bypasses the daily allowance entirely and only functions
        /// once a player has crossed the Phase 1 boundary, up to the absolute cap.
        /// </summary>
        /// <param name="player">The player receiving the ranks.</param>
        /// <param name="skill">The skill to grant ranks in.</param>
        /// <param name="amount">The number of ranks to grant.</param>
        public static void GiveEndgameSP(uint player, SkillType skill, int amount)
        {
            if (skill == SkillType.Invalid || amount <= 0 || !GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var details = GetSkillDetails(skill);

            if (!details.ContributesToSkillCap)
                return;
            if (dbPlayer.TotalSPAcquired < Phase1Cap)
                return;

            var pcSkill = dbPlayer.Skills[skill];
            var effectiveMaxRank = GetEffectiveMaxRank(dbPlayer, skill);
            var granted = 0;

            while (granted < amount &&
                   dbPlayer.TotalSPAcquired < AbsoluteCap &&
                   pcSkill.Rank < effectiveMaxRank)
            {
                pcSkill.Rank++;
                pcSkill.XP = 0;
                dbPlayer.UnallocatedSP++;
                dbPlayer.TotalSPAcquired++;
                granted++;

                FloatingTextStringOnCreature($"Your {details.Name} skill level increased to rank {pcSkill.Rank}!", player, false);
                ApplyAbilityPoint(player, dbPlayer);
            }

            if (granted <= 0)
                return;

            dbPlayer.Skills[skill] = pcSkill;
            DB.Set(dbPlayer);

            Gui.PublishRefreshEvent(player, new SkillXPRefreshEvent(new List<SkillType> { skill }));
            EventsPlugin.SignalEvent("SWLOR_GAIN_SKILL_POINT", player);
        }

        /// <summary>
        /// Gives the player an ability point which can be distributed to the attribute of their choice
        /// from the character menu. One point is earned per 10 skill ranks
        /// </summary>
        /// <param name="player">The player to receive the AP.</param>
        /// <param name="dbPlayer">The database entity.</param>
        private static void ApplyAbilityPoint(uint player, Player dbPlayer)
        {
            // Total AP have been earned (700SP = 70AP)
            if (dbPlayer.TotalAPAcquired >= APCap) return;

            if (dbPlayer.TotalSPAcquired % 10 == 0)
            {
                dbPlayer.UnallocatedAP++;
                dbPlayer.TotalAPAcquired++;

                SendMessageToPC(player, ColorToken.Green("You acquired 1 ability point!"));
            }
        }

        /// <summary>
        /// If a player is missing any skills in their DB record, they will be added here.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void AddMissingSkills()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            foreach (var skill in GetAllSkills())
            {
                if (!dbPlayer.Skills.ContainsKey(skill.Key))
                {
                    dbPlayer.Skills[skill.Key] = new PlayerSkill();
                }
            }

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Calculates the maximum amount of XP that can be distributed to a skill without any loss.
        /// This prevents players from accidentally losing XP when distributing to skills at or near their maximum rank.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="skillType">The skill type to check</param>
        /// <returns>The maximum amount of XP that can be safely distributed</returns>
        public static int GetMaxDistributableXP(uint player, SkillType skillType)
        {
            if (skillType == SkillType.Invalid || !GetIsPC(player) || GetIsDM(player)) 
                return 0;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var currentSkill = dbPlayer.Skills[skillType];
            var effectiveMaxRank = GetEffectiveMaxRank(dbPlayer, skillType);

            // If skill is already at its effective maximum rank, no XP can be distributed
            if (currentSkill.Rank >= effectiveMaxRank)
                return 0;

            var totalDistributableXP = 0;
            var currentRank = currentSkill.Rank;
            var currentXP = currentSkill.XP;

            // Calculate XP needed to fill remaining ranks
            while (currentRank < effectiveMaxRank)
            {
                var requiredXPForNextRank = GetRequiredXP(currentRank);
                var xpNeededForThisRank = requiredXPForNextRank - currentXP;
                
                totalDistributableXP += xpNeededForThisRank;
                currentRank++;
                currentXP = 0; // After first rank, we start from 0 XP for subsequent ranks
            }

            return totalDistributableXP;
        }
    }
}
