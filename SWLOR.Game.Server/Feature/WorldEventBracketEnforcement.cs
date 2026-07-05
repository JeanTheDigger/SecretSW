using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Enforces event brackets at the zone boundary: players who enter an active event area
    /// they do not belong in are moved back to their home point. The other half of the rule
    /// (a zone flipping while occupied) is handled by the occupant sweep in WorldEvent.OpenEvent.
    /// </summary>
    public static class WorldEventBracketEnforcement
    {
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void EnforceBracketOnEntry()
        {
            var player = GetEnteringObject();
            var area = GetArea(player);

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;
            if (WorldEvent.MeetsBracketRequirements(player, area))
                return;

            SendMessageToPC(player, ColorToken.Red("An event you may not take part in is underway here. You have been moved to safety."));
            DelayCommand(0.5f, () => Death.SendToHomePoint(player));
        }
    }
}
