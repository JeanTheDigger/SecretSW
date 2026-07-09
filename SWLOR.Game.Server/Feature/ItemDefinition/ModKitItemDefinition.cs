using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    /// <summary>
    /// Mod Kit — installs an enhancement (mod) into an existing weapon or armor.
    ///
    /// Base-item crafting (Smithery) was removed from the game, and with it the old flow that
    /// slotted enhancements onto a freshly-crafted weapon/armor. Weapons and armor are now bought,
    /// looted, or earned; the mod kit is how a player still tunes them: it transfers an enhancement
    /// item's bonuses onto a target weapon/armor, up to a fixed number of mod slots.
    ///
    /// Enhancement items are the same weapon/armor enhancement items the crafting system used — they
    /// carry WeaponEnhancement / ArmorEnhancement item properties whose sub-type and amount describe
    /// each bonus. Those bonuses are rebuilt with the existing
    /// Craft.BuildItemPropertyForEnhancement mapper and applied permanently to the target.
    ///
    /// Requires a usable item with tag "mod_kit" to exist in the module (a simple activatable .uti
    /// blueprint) and enhancement items to be available from vendors/loot. The slot cap is a
    /// deliberately conservative first pass; tune MaxModSlots to taste.
    /// </summary>
    public class ModKitItemDefinition : IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        // Maximum number of mods that may be installed on a single item. Tunable.
        private const int MaxModSlots = 2;
        private const string ModSlotsUsedVariable = "MOD_SLOTS_USED";

        public Dictionary<string, ItemDetail> BuildItems()
        {
            ModKit();

            return _builder.Build();
        }

        private void ModKit()
        {
            _builder.Create("mod_kit")
                .ValidationAction((user, item, target, location, propertyIndex) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user) || GetIsDMPossessed(user))
                        return "Only players may use a mod kit.";

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, propertyIndex) =>
                {
                    // Step 1: choose the weapon or armor to modify.
                    Targeting.EnterTargetingMode(user, ObjectType.Item,
                        "Select a weapon or armor in your inventory to modify.", baseItem =>
                    {
                        if (GetItemPossessor(baseItem) != user)
                        {
                            SendMessageToPC(user, "The item to modify must be in your inventory.");
                            return;
                        }

                        var enhancementType = GetEnhancementTypeForItem(baseItem);
                        if (enhancementType == ItemPropertyType.Invalid)
                        {
                            SendMessageToPC(user, "Only weapons and armor can be modified with a mod kit.");
                            return;
                        }

                        var slotsUsed = GetLocalInt(baseItem, ModSlotsUsedVariable);
                        if (slotsUsed >= MaxModSlots)
                        {
                            SendMessageToPC(user, "That item has no free mod slots remaining.");
                            return;
                        }

                        // Step 2: choose the enhancement (mod) to install.
                        Targeting.EnterTargetingMode(user, ObjectType.Item,
                            "Select an enhancement (mod) in your inventory to install.", enhancement =>
                        {
                            if (GetItemPossessor(enhancement) != user)
                            {
                                SendMessageToPC(user, "The enhancement must be in your inventory.");
                                return;
                            }

                            if (enhancement == baseItem)
                            {
                                SendMessageToPC(user, "You cannot install an item into itself.");
                                return;
                            }

                            // Re-read the slot count in case the item changed between targeting steps.
                            var currentSlotsUsed = GetLocalInt(baseItem, ModSlotsUsedVariable);
                            if (currentSlotsUsed >= MaxModSlots)
                            {
                                SendMessageToPC(user, "That item has no free mod slots remaining.");
                                return;
                            }

                            // Gate the enhancement's tier behind the player's combat skill, replacing the
                            // recipe-relative level check the crafting system used.
                            var enhancementLevel = GetEnhancementLevel(enhancement);
                            if (!MeetsSkillRequirement(user, enhancementType, enhancementLevel))
                            {
                                SendMessageToPC(user,
                                    $"You need rank {enhancementLevel} in the relevant combat skill " +
                                    $"({(enhancementType == ItemPropertyType.ArmorEnhancement ? "Armor" : "a weapon skill")}) to install this enhancement.");
                                return;
                            }

                            if (!InstallEnhancement(enhancement, baseItem, enhancementType))
                            {
                                SendMessageToPC(user, "That enhancement cannot be installed into this item.");
                                return;
                            }

                            SetLocalInt(baseItem, ModSlotsUsedVariable, currentSlotsUsed + 1);
                            DestroyObject(enhancement);
                            Item.ReduceItemStack(item, 1);
                            SendMessageToPC(user,
                                $"Enhancement installed. ({currentSlotsUsed + 1}/{MaxModSlots} mod slots used)");
                        });
                    });
                });
        }

        /// <summary>
        /// Returns the enhancement item-property type that matches a base item: WeaponEnhancement for
        /// weapons, ArmorEnhancement for armor and shields, or Invalid if the item cannot be modified.
        /// </summary>
        private static ItemPropertyType GetEnhancementTypeForItem(uint item)
        {
            var baseItem = GetBaseItemType(item);

            if (Item.WeaponBaseItemTypes.Contains(baseItem))
                return ItemPropertyType.WeaponEnhancement;

            if (Item.ArmorBaseItemTypes.Contains(baseItem) || Item.ShieldBaseItemTypes.Contains(baseItem))
                return ItemPropertyType.ArmorEnhancement;

            return ItemPropertyType.Invalid;
        }

        /// <summary>
        /// Reads the enhancement item's level from its EnhancementLevel item property (0 if absent).
        /// </summary>
        private static int GetEnhancementLevel(uint enhancement)
        {
            for (var ip = GetFirstItemProperty(enhancement); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(enhancement))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.EnhancementLevel)
                    return GetItemPropertyCostTableValue(ip);
            }

            return 0;
        }

        /// <summary>
        /// A player may only install an enhancement whose level they can back with combat skill: the
        /// Armor skill for armor/shields, or their best weapon skill for weapons. Looted/bought gear
        /// carries no recipe level, so this stands in for the recipe-relative check the crafting
        /// system used to keep high-tier mods off low-investment characters.
        /// </summary>
        private static bool MeetsSkillRequirement(uint user, ItemPropertyType enhancementType, int enhancementLevel)
        {
            if (enhancementLevel <= 0)
                return true;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(user));

            var rank = enhancementType == ItemPropertyType.ArmorEnhancement
                ? GetSkillRank(dbPlayer, SkillType.Armor)
                : Math.Max(
                    Math.Max(GetSkillRank(dbPlayer, SkillType.OneHanded), GetSkillRank(dbPlayer, SkillType.TwoHanded)),
                    Math.Max(GetSkillRank(dbPlayer, SkillType.MartialArts), GetSkillRank(dbPlayer, SkillType.Ranged)));

            return rank >= enhancementLevel;
        }

        private static int GetSkillRank(Player dbPlayer, SkillType skill)
        {
            return dbPlayer.Skills.ContainsKey(skill) ? dbPlayer.Skills[skill].Rank : 0;
        }

        /// <summary>
        /// Transfers an enhancement item's bonuses onto the target item. The enhancement carries one
        /// or more marker properties (WeaponEnhancement / ArmorEnhancement) whose sub-type and amount
        /// describe each bonus; each is rebuilt via Craft.BuildItemPropertyForEnhancement and applied
        /// permanently to the target. Returns true if at least one bonus was applied.
        /// </summary>
        private static bool InstallEnhancement(uint enhancement, uint target, ItemPropertyType enhancementType)
        {
            var applied = false;

            for (var ip = GetFirstItemProperty(enhancement); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(enhancement))
            {
                if (GetItemPropertyType(ip) != enhancementType)
                    continue;

                var subType = (EnhancementSubType)GetItemPropertySubType(ip);
                var amount = GetItemPropertyCostTableValue(ip);

                ItemProperty builtProperty;
                try
                {
                    builtProperty = Craft.BuildItemPropertyForEnhancement(subType, amount);
                }
                catch
                {
                    // Enhancement sub-type not supported in this context; skip it rather than abort.
                    continue;
                }

                BiowareXP2.IPSafeAddItemProperty(target, builtProperty, 0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
                applied = true;
            }

            return applied;
        }
    }
}
