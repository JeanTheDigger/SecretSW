using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.PerkService
{
    /// <summary>
    /// Requires a minimum rank in ANY ONE of a set of skills.
    /// Used by the lightsaber forms, which gate on the wielded weapon's skill
    /// (One-Handed OR Two-Handed) rather than the Force skill.
    /// </summary>
    public class PerkRequirementAnySkill : IPerkRequirement
    {
        private readonly List<SkillType> _types;
        private readonly int _requiredRank;

        public PerkRequirementAnySkill(int requiredRank, params SkillType[] types)
        {
            _requiredRank = requiredRank;
            _types = types.ToList();
        }

        public string CheckRequirements(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (_types.Any(type => dbPlayer.Skills[type].Rank >= _requiredRank))
                return string.Empty;

            return $"One of the following skills must be rank {_requiredRank}: {SkillNames}";
        }

        public string RequirementText => $"{SkillNames} rank {_requiredRank} (any one)";

        private string SkillNames => string.Join(" or ", _types.Select(t => Skill.GetSkillDetails(t).Name));
    }
}
