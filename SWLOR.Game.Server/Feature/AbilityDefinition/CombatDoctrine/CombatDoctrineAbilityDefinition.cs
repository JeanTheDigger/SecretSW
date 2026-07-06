using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.CombatDoctrine
{
    /// <summary>
    /// The toggle abilities for the Standard combat doctrines. Activating a doctrine
    /// deactivates any other stance (forms included); the shared recast group enforces
    /// the six-second switch lockout.
    /// </summary>
    public class CombatDoctrineAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            BuildToggle(FeatType.DoctrineDuelist, PerkType.DoctrineDuelist, "Duelist Doctrine");
            BuildToggle(FeatType.DoctrineJuggernaut, PerkType.DoctrineJuggernaut, "Juggernaut");
            BuildToggle(FeatType.DoctrineTempest, PerkType.DoctrineTempest, "Tempest");
            BuildToggle(FeatType.DoctrineTerasKasi, PerkType.DoctrineTerasKasi, "Teräs Käsi");
            BuildToggle(FeatType.DoctrineMarksman, PerkType.DoctrineMarksman, "Marksman Doctrine");

            return _builder.Build();
        }

        private void BuildToggle(FeatType feat, PerkType perk, string name)
        {
            _builder.Create(feat, perk)
                .Name(name)
                .Level(1)
                .HasRecastDelay(RecastGroup.FormSwitch, 6f)
                .IsCastedAbility()
                .HasCustomValidation((activator, target, level, location) =>
                {
                    // Deactivating the current stance is always allowed; taking one needs the weapon.
                    if (Stance.GetActiveStanceType(activator) != perk)
                        return Stance.ValidateStanceWeapon(activator, perk);

                    return string.Empty;
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    Stance.ToggleStance(activator, perk);
                });
        }
    }
}
