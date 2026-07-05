using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    /// <summary>
    /// The toggle abilities for the seven lightsaber forms. Activating a form deactivates any
    /// other; the shared recast group enforces the six-second switch lockout.
    /// </summary>
    public class LightsaberFormAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            BuildToggle(FeatType.FormShiiCho, PerkType.FormShiiCho, "Form I: Shii-Cho");
            BuildToggle(FeatType.FormMakashi, PerkType.FormMakashi, "Form II: Makashi");
            BuildToggle(FeatType.FormSoresu, PerkType.FormSoresu, "Form III: Soresu");
            BuildToggle(FeatType.FormAtaru, PerkType.FormAtaru, "Form IV: Ataru");
            BuildToggle(FeatType.FormDjemSo, PerkType.FormDjemSo, "Form V: Djem So");
            BuildToggle(FeatType.FormNiman, PerkType.FormNiman, "Form VI: Niman");
            BuildToggle(FeatType.FormJuyo, PerkType.FormJuyo, "Form VII: Juyo");

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
                    // Deactivating the current form is always allowed; taking a stance needs a saber.
                    if (Stance.GetActiveForm(activator) != perk && !Stance.IsSaberEquipped(activator))
                        return "A lightsaber or saberstaff must be equipped to take a form.";

                    return string.Empty;
                })
                .HasImpactAction((activator, target, level, location) =>
                {
                    Stance.ToggleForm(activator, perk);
                });
        }
    }
}
