using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Force Lock - the Teräs Käsi capstone's silence. While active, the target cannot use
    /// Force abilities (enforced in Ability.CanUseAbility / CanActivateAbility). Deliberately
    /// short and applied by an ability on a long shared recast so it can never be chained.
    /// </summary>
    public class ForceLockStatusEffectDefinition : IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            _builder.Create(StatusEffectType.ForceLock)
                .Name("Force Lock")
                .EffectIcon(EffectIconType.Stunned);

            return _builder.Build();
        }
    }
}
