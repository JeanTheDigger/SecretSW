using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Applies the Concealment perk's Hide/Move Silently bonuses as a permanent tagged
    /// effect, recalculated on login and on perk purchase/refund.
    /// </summary>
    public static class StealthConcealment
    {
        private const string EffectTag = "CONCEALMENT_PERK";

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnModuleEnter()
        {
            Recalculate(GetEnteringObject());
        }

        public static void Recalculate(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            // Strip the previous application.
            for (var effect = GetFirstEffect(player); GetIsEffectValid(effect); effect = GetNextEffect(player))
            {
                if (GetEffectTag(effect) == EffectTag)
                    RemoveEffect(player, effect);
            }

            var level = Perk.GetPerkLevel(player, PerkType.Concealment);
            if (level <= 0)
                return;

            var bonus = level * 3;
            var concealment = SupernaturalEffect(TagEffect(EffectLinkEffects(
                EffectSkillIncrease(NWNSkillType.Hide, bonus),
                EffectSkillIncrease(NWNSkillType.MoveSilently, bonus)), EffectTag));
            ApplyEffectToObject(DurationType.Permanent, concealment, player);
        }
    }
}
