using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class OneHandedPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Doublehand();
            DualWield();
            WeaponFocusVibroblades();
            ImprovedCriticalVibroblades();
            VibrobladeProficiency();
            VibrobladeMastery();
            HackingBlade();
            RiotBlade();
            WeaponFocusFinesseVibroblades();
            ImprovedCriticalFinesseVibroblades();
            FinesseVibrobladeProficiency();
            FinesseVibrobladeMastery();
            PoisonStab();
            Backstab();
            WeaponFocusLightsabers();
            ImprovedCriticalLightsabers();
            LightsaberProficiency();
            LightsaberMastery();
            SaberStrike();
            ImprovedTwoWeaponFighting();
            Duelist();
            WailingBlows();
            PommelStrike();

            return _builder.Build();
        }

        private void Doublehand()
        {
            _builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.Doublehand)
                .Name("Doublehand")

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 2 when no off-hand item is equipped. [Cross Skill]")
                .Price(1)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.Doublehand1)

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 6 when no off-hand item is equipped. [Cross Skill]")
                .Price(1)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.OneHanded, 10)
                .GrantsFeat(FeatType.Doublehand2)

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 10 when no off-hand item is equipped. [Cross Skill]")
                .Price(1)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.OneHanded, 20)
                .GrantsFeat(FeatType.Doublehand3)

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 14 when no off-hand item is equipped. [Cross Skill]")
                .Price(1)
                .DroidAISlots(5)
                .RequirementSkill(SkillType.OneHanded, 30)
                .GrantsFeat(FeatType.Doublehand4)

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 19 when no off-hand item is equipped. [Cross Skill]")
                .Price(2)
                .DroidAISlots(6)
                .RequirementSkill(SkillType.OneHanded, 40)
                .GrantsFeat(FeatType.Doublehand5);
        }

        private void DualWield()
        {
            _builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.DualWield)
                .Name("Dual Wield")

                .AddPerkLevel()
                .Description("Enables the use of two one-handed weapons at the same time. Off-hand attacks suffer -10% to hit. [Cross Skill]")
                .Price(4)
                .DroidAISlots(1)
                .RequirementSkill(SkillType.OneHanded, 15)
                .GrantsFeat(FeatType.DualWield);
        }

        private void WailingBlows()
        {
            _builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.WailingBlows)
                .Name("Wailing Blows")

                .AddPerkLevel()
                .Description("While dual-wielding one-handed weapons, you gain 15% critical chance.")
                .Price(4)
                .RequirementSkill(SkillType.OneHanded, 25)
                .GrantsFeat(FeatType.WailingBlows)

                .TriggerEquippedItem((player, item, slot, type, level) =>
                 {
                     if (slot != InventorySlot.RightHand) return;

                     Stat.ApplyCritModifier(player, item);
                 })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyCritModifier(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyCritModifier(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyCritModifier(player, item);
                });
        }

        private void Duelist()
        {
            _builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.Duelist)
                .Name("Duelist")

                .AddPerkLevel()
                .Description("While wielding a one-handed weapon with a free off-hand, you gain 5% to hit and 5% critical chance. [Cross Skill]")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 15)
                .GrantsFeat(FeatType.Duelist)

                .TriggerEquippedItem((player, item, slot, type, level) =>
                 {
                     if (slot != InventorySlot.RightHand) return;

                     Stat.ApplyCritModifier(player, item);
                 })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyCritModifier(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyCritModifier(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyCritModifier(player, item);
                });
        }

        private void WeaponFocusVibroblades()
        {
            _builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.WeaponFocusVibroblades)
                .Name("Weapon Focus - Vibroblades")

                .AddPerkLevel()
                .Description("Your accuracy with vibroblades is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 5)
                .GrantsFeat(FeatType.WeaponFocusVibroblades)

                .AddPerkLevel()
                .Description("Your base damage with vibroblades is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.OneHanded, 15)
                .GrantsFeat(FeatType.WeaponSpecializationVibroblades);
        }

        private void ImprovedCriticalVibroblades()
        {
            _builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.ImprovedCriticalVibroblades)
                .Name("Improved Critical - Vibroblades")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with vibroblades by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 25)
                .GrantsFeat(FeatType.ImprovedCriticalVibroblades);
        }

        private void VibrobladeProficiency()
        {
            _builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.VibrobladeProficiency)
                .Name("Vibroblade Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Vibroblades.")
                .Price(2)
                .GrantsFeat(FeatType.VibrobladeProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 10)
                .GrantsFeat(FeatType.VibrobladeProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 20)
                .GrantsFeat(FeatType.VibrobladeProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 30)
                .GrantsFeat(FeatType.VibrobladeProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 40)
                .GrantsFeat(FeatType.VibrobladeProficiency5);
        }

        private void VibrobladeMastery()
        {
            _builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.VibrobladeMastery)
                .Name("Vibroblade Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Vibroblade.")
                .Price(8)
                .RequirementSkill(SkillType.OneHanded, 25)
                .GrantsFeat(FeatType.VibrobladeMastery1)
                
                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Vibroblade.")
                .Price(8)
                .RequirementSkill(SkillType.OneHanded, 50)
                .GrantsFeat(FeatType.VibrobladeMastery2);
        }

        private void HackingBlade()
        {
            _builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.HackingBlade)
                .Name("Hacking Blade")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6 DMG and has a DC10 Fortitude check to inflict Bleed for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HackingBlade1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 15 DMG and has a DC15 Fortitude check to inflict Bleed for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HackingBlade2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 22 DMG and has a DC20 Fortitude check to inflict Bleed for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HackingBlade3);
        }

        private void RiotBlade()
        {
            _builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.RiotBlade)
                .Name("Riot Blade")

                .AddPerkLevel()
                .Description("Instantly deals 14 DMG to your target.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RiotBlade1)

                .AddPerkLevel()
                .Description("Instantly deals 26 DMG to your target.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RiotBlade2)

                .AddPerkLevel()
                .Description("Instantly deals 38 DMG to your target.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RiotBlade3);
        }

        private void WeaponFocusFinesseVibroblades()
        {
            _builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.WeaponFocusFinesseVibroblades)
                .Name("Weapon Focus - Finesse Vibroblades")

                .AddPerkLevel()
                .Description("Your accuracy with finesse vibroblades is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 5)
                .GrantsFeat(FeatType.WeaponFocusFinesseVibroblades)

                .AddPerkLevel()
                .Description("Your base damage with finesse vibroblades is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.OneHanded, 15)
                .GrantsFeat(FeatType.WeaponSpecializationFinesseVibroblades);
        }

        private void ImprovedCriticalFinesseVibroblades()
        {
            _builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.ImprovedCriticalFinesseVibroblades)
                .Name("Improved Critical - Finesse Vibroblades")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with finesse vibroblades by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 25)
                .GrantsFeat(FeatType.ImprovedCriticalFinesseVibroblades);
        }

        private void FinesseVibrobladeProficiency()
        {
            _builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.FinesseVibrobladeProficiency)
                .Name("Finesse Vibroblade Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Finesse Vibroblades.")
                .Price(2)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Finesse Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 10)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Finesse Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 20)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Finesse Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 30)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Finesse Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 40)
                .GrantsFeat(FeatType.FinesseVibrobladeProficiency5);
        }

        private void FinesseVibrobladeMastery()
        {
            _builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.FinesseVibrobladeMastery)
                .Name("Finesse Vibroblade Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack with a Finesse Vibroblade.")
                .Price(8)
                .RequirementSkill(SkillType.OneHanded, 25)
                .GrantsFeat(FeatType.FinesseVibrobladeMastery1)

                .AddPerkLevel()
                .Description("Grants an additional attack with a Finesse Vibroblade.")
                .Price(8)
                .RequirementSkill(SkillType.OneHanded, 50)
                .GrantsFeat(FeatType.FinesseVibrobladeMastery2);
        }

        private void PoisonStab()
        {
            _builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.PoisonStab)
                .Name("Poison Stab")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 8 DMG and has a DC10 Fortitude check to inflict Poison for 30 seconds.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.OneHanded, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PoisonStab1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 18 DMG and has a DC15 Fortitude check to inflict Poison for 1 minute.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.OneHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PoisonStab2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 28 DMG and has a DC20 Fortitude check to inflict Poison for 1 minute.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.OneHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PoisonStab3);
        }

        private void Backstab()
        {
            _builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.Backstab)
                .Name("Backstab")

                .AddPerkLevel()
                .Description("Deals 14 DMG to your target when dealt from behind. Damage is halved if not behind target.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.OneHanded, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Backstab1)

                .AddPerkLevel()
                .Description("Deals 30 DMG to your target when dealt from behind. Damage is halved if not behind target.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Backstab2)

                .AddPerkLevel()
                .Description("Deals 45 DMG to your target when dealt from behind. Damage is halved if not behind target.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.OneHanded, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Backstab3);
        }

        private void WeaponFocusLightsabers()
        {
            _builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.WeaponFocusLightsabers)
                .Name("Weapon Focus - Lightsabers")

                .AddPerkLevel()
                .Description("Your accuracy with lightsabers is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.WeaponFocusLightsabers)

                .AddPerkLevel()
                .Description("Your base damage with lightsabers is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.OneHanded, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.WeaponSpecializationLightsabers);
        }

        private void ImprovedCriticalLightsabers()
        {
            _builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.ImprovedCriticalLightsabers)
                .Name("Improved Critical - Lightsabers")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with lightsabers by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ImprovedCriticalLightsabers);
        }

        private void LightsaberProficiency()
        {
            _builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.LightsaberProficiency)
                .Name("Lightsaber Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Lightsabers.")
                .Price(2)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Lightsabers.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Lightsabers.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Lightsabers.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Lightsabers.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberProficiency5);
        }

        private void LightsaberMastery()
        {
            _builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.LightsaberMastery)
                .Name("Lightsaber Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Lightsaber.")
                .Price(8)
                .RequirementSkill(SkillType.OneHanded, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberMastery1)
                
                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Lightsaber.")
                .Price(8)
                .RequirementSkill(SkillType.OneHanded, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LightsaberMastery2);
        }



        private void SaberStrike()
        {
            _builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.SaberStrike)
                .Name("Saber Strike")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6 DMG and has a DC10 Fortitude check to inflict Breach, reducing evasion for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberStrike1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 15 DMG and has a DC15 Fortitude check to inflict Breach, reducing evasion for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberStrike2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 22 DMG and has a DC20 Fortitude check to inflict Breach, reducing evasion for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberStrike3);
        }

        private void ImprovedTwoWeaponFighting()
        {
            _builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.ImprovedTwoWeaponFightingOneHanded)
                .Name("Improved Two Weapon Fighting (One-Handed)")

                .AddPerkLevel()
                .Description("Grants an additional off-hand attack when dual wielding or using a double-sided weapon. [Cross Skill]")
                .Price(4)
                .RequirementSkill(SkillType.OneHanded, 40)
                .RequirementCannotHavePerk(PerkType.ImprovedTwoWeaponFightingTwoHanded)
                .GrantsFeat(FeatType.ImprovedTwoWeaponFighting);
        }


        private void PommelStrike()
        {
            _builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.PommelStrike)
                .Name("Pommel Strike")

                .AddPerkLevel()
                .Description("Strike your enemy with the pommel of your saber, doing damage and dazing them.")
                .Price(2)
                .DroidAISlots(1)
                .RequirementSkill(SkillType.OneHanded, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.PommelStrike1)

                .AddPerkLevel()
                .Description("Strike your enemy with the pommel of your saber, doing damage and dazing them.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.PommelStrike2)

                .AddPerkLevel()
                .Description("Strike your enemy with the pommel of your saber, doing damage and dazing them.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.OneHanded, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.PommelStrike3);
        }

    }
}
    
