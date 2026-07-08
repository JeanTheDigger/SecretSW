using System;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.SkillService
{
    // Note: Corresponds to iprp_skill.2da
    // New additions or changes to this file should also be made to the 2DA.
    public enum SkillType
    {
        [Skill(SkillCategoryType.Invalid,
            "Invalid",
            0,
            false,
            "Unused in-game.",
            false,
            false,
            false)]
        Invalid = 0,

        [Skill(SkillCategoryType.Combat,
            "One-Handed",
            100,
            true,
            "Ability to use one-handed weapons like vibroblades, finesse vibroblades, and lightsabers.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon)]
        OneHanded = 1,

        [Skill(SkillCategoryType.Combat,
            "Two-Handed",
            100,
            true,
            "Ability to use heavy weapons like heavy vibroblades, polearms, and saberstaffs in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon)]
        TwoHanded = 2,

        [Skill(SkillCategoryType.Combat,
            "Martial Arts", 100,
            true,
            "Ability to fight using katars and staves in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon)]
        MartialArts = 3,

        [Skill(SkillCategoryType.Combat,
            "Ranged",
            100,
            true,
            "Ability to use ranged weapons like pistols, shurikens, and rifles in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Weapon)]
        Ranged = 4,

        [Skill(SkillCategoryType.Combat,
            "Force",
            100,
            true,
            "Ability to use Force abilities.",
            true,
            false,
            false,
            CombatPointCategoryType.Utility,
            CharacterType.ForceSensitive)]
        Force = 5,

        [Skill(SkillCategoryType.Combat,
            "Armor",
            100,
            true,
            "Ability to effectively wear and defend against attacks with armor.",
            true,
            false,
            false)]
        Armor = 6,

        [Skill(SkillCategoryType.Utility,
            "Piloting",
            100,
            true,
            "Ability to pilot starships, follow navigation charts, and control starship systems.",
            true,
            false,
            false)]
        Piloting = 7,

        [Skill(SkillCategoryType.Utility,
            "First Aid",
            100,
            true,
            "Ability to treat bodily injuries in the field with healing kits and stim packs.",
            true,
            false,
            false)]
        FirstAid = 8,

        // Smithery (hand-crafting of weapons and armor) has been removed from the game. Gear is
        // now bought, earned through factions/DM events, or found in dungeons. The enum value and
        // 2DA row are retained for serialized player-data integrity; the skill is inactive and no
        // longer contributes to the skill cap.
        [Skill(SkillCategoryType.Crafting,
            "Smithery",
            100,
            false,
            "Ability to create weapons and armor like vibroblades, blasters, and helmets.",
            false,
            false,
            false)]
        Smithery = 9,

        // Fabrication (hand-crafting of structures and furniture) has been removed from the game.
        // Furniture and structures are now bought or earned. The enum value and 2DA row are
        // retained for serialized player-data integrity; the skill is inactive and no longer
        // contributes to the skill cap.
        [Skill(SkillCategoryType.Crafting,
            "Fabrication",
            100,
            false,
            "Ability to create base structures and furniture.",
            false,
            false,
            false)]
        Fabrication = 10,

        // Gathering (harvesting/refining/scavenging) has been removed from the game. Gear and
        // materials are now bought, earned through factions/DM events, or found in dungeons.
        // The enum value and 2DA row are retained for serialized player-data integrity; the skill
        // is inactive and no longer contributes to the skill cap.
        [Skill(SkillCategoryType.Crafting,
            "Gathering",
            100,
            false,
            "Ability to harvest raw materials and scavenge for supplies.",
            false,
            false,
            false)]
        Gathering = 11,

        [Skill(SkillCategoryType.Utility,
            "Leadership",
            100,
            true,
            "Ability to handle people, negotiate, and manage relations.",
            true,
            false,
            false)]
        Leadership = 12,

        // Beast Mastery has been removed from the game. The enum value and 2DA row are retained
        // for serialized player-data integrity; the skill is inactive and no longer contributes
        // to the skill cap. Droids (Engineering) carry the companion niche.
        [Skill(SkillCategoryType.Combat,
            "Beast Mastery",
            50,
            false,
            "Ability to tame wild animals, raise them, and train them.",
            false,
            false,
            false)]
        BeastMastery = 13,

        [Skill(SkillCategoryType.Languages,
            "Mirialan",
            20,
            true,
            "Ability to speak the Mirialan language.",
            false,
            false,
            false)]
        Mirialan = 14,

        [Skill(SkillCategoryType.Languages,
            "Bothese",
            20,
            true,
            "Ability to speak the Bothese language.",
            false,
            false,
            false)]
        Bothese = 15,

        [Skill(SkillCategoryType.Languages,
            "Cheunh",
            20,
            true,
            "Ability to speak the Cheunh language.",
            false,
            false,
            false)]
        Cheunh = 16,


        [Skill(SkillCategoryType.Languages,
            "Zabraki",
            20,
            true,
            "Ability to speak the Zabraki language.",
            false,
            false,
            false)]
        Zabraki = 17,

        [Skill(SkillCategoryType.Languages,
            "Twi'leki (Ryl)",
            20,
            true,
            "Ability to speak the Twi'leki (Ryl) language.",
            false,
            false,
            false)]
        Twileki = 18,

        [Skill(SkillCategoryType.Languages,
            "Catharese", 20,
            true,
            "Ability to speak the Catharese language.",
            false,
            false,
            false)]
        Catharese = 19,

        [Skill(SkillCategoryType.Languages,
            "Dosh",
            20,
            true,
            "Ability to speak the Dosh language.",
            false,
            false,
            false)]
        Dosh = 20,

        [Skill(SkillCategoryType.Languages,
            "Shyriiwook",
            20,
            true,
            "Ability to speak the Shyriiwook (Wookieespeak) language.",
            false,
            false,
            false)]
        Shyriiwook = 21,

        [Skill(SkillCategoryType.Languages,
            "Droidspeak",
            20,
            true,
            "Ability to speak the Droidspeak language.",
            false,
            false,
            false)]
        Droidspeak = 22,

        [Skill(SkillCategoryType.Languages,
            "Basic",
            20,
            true,
            "Ability to speak the Galactic Basic language.",
            false,
            false,
            false)]
        Basic = 23,

        [Skill(SkillCategoryType.Languages,
            "Mandoa",
            20,
            true,
            "Ability to speak the Mandoa language.",
            false,
            false,
            false)]
        Mandoa = 24,

        [Skill(SkillCategoryType.Languages,
            "Huttese",
            20,
            true,
            "Ability to speak the Huttese language.",
            false,
            false,
            false)]
        Huttese = 25,

        [Skill(SkillCategoryType.Languages,
            "Mon Calamarian",
            20,
            true,
            "Ability to speak the Mon Calamarian language.",
            false,
            false,
            false)]
        MonCalamarian = 26,

        [Skill(SkillCategoryType.Languages,
            "Ugnaught",
            20,
            true,
            "Ability to speak the Ugnaught language.",
            false,
            false,
            false)]
        Ugnaught = 27,

        [Skill(SkillCategoryType.Languages,
            "Rodese",
            20,
            true,
            "Ability to speak the Rodese language.",
            false,
            false,
            false)]
        Rodese = 28,

        [Skill(SkillCategoryType.Languages,
            "Togruti",
            20,
            true,
            "Ability to speak the Togruti language.",
            false,
            false,
            false)]
        Togruti = 29,

        [Skill(SkillCategoryType.Languages,
            "Kel Dor",
            20,
            true,
            "Ability to speak the Kel Dor language.",
            false,
            false,
            false)]
        KelDor = 30,

        // Agriculture (farming, fishing, and cooking) has been removed from the game along with the
        // food-buff system. The enum value and 2DA row are retained for serialized player-data
        // integrity; the skill is inactive and no longer contributes to the skill cap.
        [Skill(SkillCategoryType.Crafting,
            "Agriculture",
            100,
            false,
            "Ability to farm, fish, and cook.",
            false,
            false,
            false)]
        Agriculture = 31,

        [Skill(SkillCategoryType.Crafting,
            "Engineering",
            100,
            true,
            "Ability to create starships, modules, droids, and other electronic & mechanical items.",
            true,
            true,
            true)]
        Engineering = 32,

        [Skill(SkillCategoryType.Combat,
            "Devices",
            100,
            true,
            "Ability to use grenades, bombs, and other electronics.",
            true,
            false,
            false,
            CombatPointCategoryType.Utility,
            CharacterType.Standard)]
        Devices = 33,

        [Skill(SkillCategoryType.Languages,
            "Nautila",
            20,
            true,
            "Ability to speak the Nautila language.",
            false,
            false,
            false)]
            Nautila = 34,

        [Skill(SkillCategoryType.Languages,
            "Ewokese",
            20,
            true,
           "Ability to speak the Ewok language.",
           false,
           false,
           false)]
           Ewokese = 35,

        [Skill(SkillCategoryType.Combat,
            "Slicing",
            100,
            true,
            "Ability to slice droids, turrets, and electronic systems in combat.",
            true,
            false,
            false,
            CombatPointCategoryType.Utility)]
        Slicing = 36,

        [Skill(SkillCategoryType.Combat,
            "Stealth",
            100,
            true,
            "Ability to move unseen and strike from hiding.",
            true,
            false,
            false,
            CombatPointCategoryType.Utility)]
        Stealth = 37,
    }

    public class SkillAttribute : Attribute
    {
        public SkillCategoryType Category { get; set; }
        public string Name { get; set; }
        public int MaxRank { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
        public bool ContributesToSkillCap { get; set; }
        public bool IsShownInCraftMenu { get; set; }
        public bool IsShownInResearchMenu { get; set; }
        public CharacterType CharacterTypeRestriction { get; set; }

        public CombatPointCategoryType CombatPointCategory { get; set; } 

        public SkillAttribute(
            SkillCategoryType category,
            string name,
            int maxRank,
            bool isActive,
            string description,
            bool contributesToSkillCap,
            bool isShownInCraftMenu,
            bool isShownInResearchMenu,
            CombatPointCategoryType combatPointCategory = CombatPointCategoryType.Exempt,
            CharacterType characterTypeRestriction = CharacterType.Invalid)
        {
            Category = category;
            Name = name;
            MaxRank = maxRank;
            IsActive = isActive;
            Description = description;
            ContributesToSkillCap = contributesToSkillCap;
            IsShownInCraftMenu = isShownInCraftMenu;
            IsShownInResearchMenu = isShownInResearchMenu;
            CharacterTypeRestriction = characterTypeRestriction;
            CombatPointCategory = combatPointCategory;
        }
    }
}
