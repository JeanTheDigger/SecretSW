using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// The humanity cost of cybernetics: every installed implant line reduces healing
    /// RECEIVED by 5% (max 15% at three lines). Wired through the NWNX heal event: the
    /// original heal is skipped and re-applied reduced, with a recursion guard so the
    /// re-application passes through untouched.
    /// </summary>
    public static class ImplantHumanityCost
    {
        private const string RecursionGuardVariable = "HUMANITY_HEAL_GUARD";

        [NWNEventHandler(ScriptName.OnHealBefore)]
        public static void ApplyHumanityCost()
        {
            var target = StringToObject(EventsPlugin.GetEventData("TARGET_OBJECT_ID"));
            if (!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target))
                return;

            // Our own reduced re-application - let it through.
            if (GetLocalBool(target, RecursionGuardVariable))
            {
                DeleteLocalBool(target, RecursionGuardVariable);
                return;
            }

            var playerId = GetObjectUUID(target);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            var lines = Implant.CountInstalledLines(dbPlayer);
            if (lines <= 0)
                return;

            if (!int.TryParse(EventsPlugin.GetEventData("HEAL_AMOUNT"), out var amount) || amount <= 0)
                return;

            var reductionPercent = Math.Min(lines * 5, 15);
            var reduced = amount - amount * reductionPercent / 100;
            if (reduced >= amount)
                return;

            EventsPlugin.SkipEvent();

            if (reduced > 0)
            {
                SetLocalBool(target, RecursionGuardVariable, true);
                ApplyEffectToObject(DurationType.Instant, EffectHeal(reduced), target);
            }
        }
    }
}
