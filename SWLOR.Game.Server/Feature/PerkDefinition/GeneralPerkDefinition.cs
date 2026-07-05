using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class GeneralPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Dash();
            SignatureWeapon();

            return _builder.Build();
        }

        // Both classes: attune to one specific weapon item with /attune. The bond grows
        // with the perk, and when its wielder perma-dies in an event, the weapon survives
        // as a lootable heirloom - event deaths generate stories and economy.
        private void SignatureWeapon()
        {
            _builder.Create(PerkCategoryType.General, PerkType.SignatureWeapon)
                .Name("Signature Weapon")

                .AddPerkLevel()
                .Description("Attune to the weapon in your hands (/attune): +3 damage with THAT item alone. Should you fall forever, it falls beside you - an heirloom for whoever claims it.")
                .Price(5)
                .RequirementTotalSP(400)

                .AddPerkLevel()
                .Description("Your bond deepens: +6 damage with your signature weapon.")
                .Price(6)
                .RequirementTotalSP(550);
        }

        private void Dash()
        {
            void ToggleDash(uint player)
            {
                if (Ability.IsAbilityToggled(player, AbilityToggleType.Dash))
                {
                    Ability.ToggleAbility(player, AbilityToggleType.Dash, false);
                }
            }

            _builder.Create(PerkCategoryType.General, PerkType.Dash)
                .Name("Dash")
                
                .AddPerkLevel()
                .Description("Grants the Dash ability. Increases movement rate by 10% while active.")
                .Price(2)
                .GrantsFeat(FeatType.Dash)

                .AddPerkLevel()
                .Description("Increases movement rate of Dash to 25%.")
                .Price(3)
                .PurchaseRequirement((player) =>
                {
                    if (Ability.IsAbilityToggled(player, AbilityToggleType.Dash))
                    {
                        return "Please disable Dash and try again.";
                    }

                    return string.Empty;
                })
                .TriggerPurchase(ToggleDash)
                .TriggerRefund(ToggleDash);
        }
    }
}
