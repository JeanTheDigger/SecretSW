using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class DevicesPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DemolitionExpert();
            FragGrenade();
            ConcussionGrenade();
            FlashbangGrenade();
            IonGrenade();
            KoltoGrenade();
            AdhesiveGrenade();
            SmokeBomb();
            KoltoBomb();
            IncendiaryBomb();
            GasBomb();
            StealthGenerator();
            Flamethrower();
            WristRocket();
            DeflectorShield();
            CarboniteProjector();
            CombatJetpack();
            OrbitalStrike();
            ToxinVials();

            return _builder.Build();
        }

        private void ToxinVials()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.ToxinVials)
                .Name("Toxin Vials")

                .AddPerkLevel()
                .Description("Hurl a toxin flask: the target suffers Perception-scaled poison for 18 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ToxinFlask)

                .AddPerkLevel()
                .Description("Your toxins are refined: the poison runs for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard);
        }

        // ==========================================================================
        // Devices Phase-2 tech. All three are marquee event tech: level 1 requires
        // an unlock item from the event economy, and the Devices gates sit past
        // rank 50, inheriting the Trials gate.
        // ==========================================================================

        private void CarboniteProjector()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.CarboniteProjector)
                .Name("Carbonite Projector")

                .AddPerkLevel()
                .Description("Flash-freezes a target in carbonite for 2 seconds (they cannot act; a long immunity follows - the marquee tech hold, priced under the CC rules).")
                .Price(5)
                .RequirementUnlocked()
                .RequirementSkill(SkillType.Devices, 60)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CarboniteProjector)

                .AddPerkLevel()
                .Description("Your carbonite freeze lasts 3 seconds.")
                .Price(6)
                .RequirementSkill(SkillType.Devices, 80)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void CombatJetpack()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.CombatJetpack)
                .Name("Combat Jetpack")

                .AddPerkLevel()
                .Description("Rocket to a position up to 20 meters away - mobility as equipment.")
                .Price(5)
                .RequirementUnlocked()
                .RequirementSkill(SkillType.Devices, 65)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CombatJetpack)

                .AddPerkLevel()
                .Description("The burn carries you onward: +25% movement speed for 6 seconds after landing.")
                .Price(6)
                .RequirementSkill(SkillType.Devices, 85)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void OrbitalStrike()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.OrbitalStrike)
                .Name("Orbital Strike")

                .AddPerkLevel()
                .Description("Paint a position for orbital bombardment: 6 seconds later, heavy fire damage strikes everything hostile in the area. 5-minute cycle. The counterplay is leaving.")
                .Price(6)
                .RequirementUnlocked()
                .RequirementSkill(SkillType.Devices, 90)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.OrbitalStrike);
        }

        private void DemolitionExpert()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.DemolitionExpert)
                .Name("Demolition Expert")

                .AddPerkLevel()
                .Description("10% chance to use a Devices ability without consuming explosives.")
                .Price(1)
                .RequirementSkill(SkillType.Devices, 10)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DemolitionExpert1)

                .AddPerkLevel()
                .Description("20% chance to use a Devices ability without consuming explosives.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DemolitionExpert2)

                .AddPerkLevel()
                .Description("30% chance to use a Devices ability without consuming explosives.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DemolitionExpert3);
        }

        private void FragGrenade()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.FragGrenade)
                .Name("Frag Grenade")

                .AddPerkLevel()
                .Description("Deals 20 fire DMG, scaling with your Perception modifier, to all creatures within range of explosion. Consumes explosives on use.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FragGrenade1)

                .AddPerkLevel()
                .Description("Deals 50 fire DMG, scaling with your Perception modifier, to all creatures within range of explosion. Also has an 8DC reflex check to inflict Bleeding. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FragGrenade2)

                .AddPerkLevel()
                .Description("Deals 80 fire DMG, scaling with your Perception modifier, to all creatures within range of explosion. Also has a 12DC reflex check to inflict Bleeding. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FragGrenade3);
        }

        private void ConcussionGrenade()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade")

                .AddPerkLevel()
                .Description("Deals 20 electrical DMG, scaling with your Perception modifier, to all creatures within range of explosion. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.ConcussionGrenade1)

                .AddPerkLevel()
                .Description("Deals 35 electrical DMG, scaling with your Perception modifier, to all creatures within range of explosion. Also has an 8DC reflex check to inflict Knockdown for 3 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ConcussionGrenade2)

                .AddPerkLevel()
                .Description("Deals 50 electrical DMG, scaling with your Perception modifier, to all creatures within range of explosion. Also has a 12DC reflex check to inflict Knockdown for 3 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ConcussionGrenade3);
        }

        private void FlashbangGrenade()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.FlashbangGrenade)
                .Name("Flashbang Grenade")

                .AddPerkLevel()
                .Description("Reduces Accuracy by 10 on all enemies within range of explosion for 20 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 10)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.FlashbangGrenade1)

                .AddPerkLevel()
                .Description("Reduces Accuracy by 20 on all enemies within range of explosion for 20 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FlashbangGrenade2)

                .AddPerkLevel()
                .Description("Reduces Accuracy by 30 on all enemies within range of explosion for 20 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FlashbangGrenade3);
        }

        private void IonGrenade()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.IonGrenade)
                .Name("Ion Grenade")

                .AddPerkLevel()
                .Description("Deals 20 electrical DMG, scaling with your Perception modifier, to all enemies within range of explosion. Deals bonus damage to droids. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.IonGrenade1)

                .AddPerkLevel()
                .Description("Deals 45 electrical DMG, scaling with your Perception modifier, to all enemies within range of explosion. Deals bonus damage to droids. Also has a 10DC fortitude check to inflict stun to droids for 6 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.IonGrenade2)

                .AddPerkLevel()
                .Description("Deals 70 electrical DMG, scaling with your Perception modifier, to all enemies within range of explosion. Deals bonus damage to droids. Also has a 14DC fortitude check to inflict stun to droids for 6 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.IonGrenade3);
        }

        private void KoltoGrenade()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.KoltoGrenade)
                .Name("Kolto Grenade")

                .AddPerkLevel()
                .Description("Grants 6 HP regeneration to all party members within range of explosion for 45 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 4)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.KoltoGrenade1)

                .AddPerkLevel()
                .Description("Grants 14 HP regeneration to all party members within range of explosion for 45 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KoltoGrenade2)

                .AddPerkLevel()
                .Description("Grants 24 HP regeneration to all party members within range of explosion for 45 seconds. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KoltoGrenade3);
        }

        private void AdhesiveGrenade()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade")

                .AddPerkLevel()
                .Description("Inflicts slow on all enemies within range of explosion for 4 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.AdhesiveGrenade1)

                .AddPerkLevel()
                .Description("Inflicts slow on all enemies within range of explosion for 6 seconds. Also has an 8DC fortitude check to inflict immobilize instead. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.AdhesiveGrenade2)

                .AddPerkLevel()
                .Description("Inflicts slow on all enemies within range of explosion for 8 seconds. Also has a 12DC fortitude check to inflict immobilize instead. Consumes explosives on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.AdhesiveGrenade3);
        }

        private void SmokeBomb()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.SmokeBomb)
                .Name("Smoke Bomb")

                .AddPerkLevel()
                .Description("Creates a smokescreen at the explosion site, granting invisibility to all creatures who enter the area of effect for 20 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 8)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .GrantsFeat(FeatType.SmokeBomb1)

                .AddPerkLevel()
                .Description("Creates a smokescreen at the explosion site, granting invisibility to all creatures who enter the area of effect for 40 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 28)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.SmokeBomb2)

                .AddPerkLevel()
                .Description("Creates a smokescreen at the explosion site, granting invisibility to all creatures who enter the area of effect for 60 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 48)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.SmokeBomb3);
        }

        private void KoltoBomb()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.KoltoBomb)
                .Name("Kolto Bomb")

                .AddPerkLevel()
                .Description("Creates a Kolto field at the explosion site, granting 4 HP regeneration to all creatures who enter the area of effect for 20 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 10)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .GrantsFeat(FeatType.KoltoBomb1)

                .AddPerkLevel()
                .Description("Creates a Kolto field at the explosion site, granting 12 HP regeneration to all creatures who enter the area of effect for 40 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 24)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KoltoBomb2)

                .AddPerkLevel()
                .Description("Creates a Kolto field at the explosion site, granting 20 HP regeneration to all creatures who enter the area of effect for 60 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 44)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KoltoBomb3);
        }

        private void IncendiaryBomb()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.IncendiaryBomb)
                .Name("Incendiary Bomb")

                .AddPerkLevel()
                .Description("Creates a fire field at the explosion site, dealing 4 fire DMG, scaling with your Perception Score, to all creatures who enter the area of effect for 20 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 13)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .GrantsFeat(FeatType.IncendiaryBomb1)

                .AddPerkLevel()
                .Description("Creates a fire field at the explosion site, dealing 10 fire DMG, scaling with your Perception Score, to all creatures who enter the area of effect for 40 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 33)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.IncendiaryBomb2)

                .AddPerkLevel()
                .Description("Creates a fire field at the explosion site, dealing 16 fire DMG, scaling with your Perception Score, to all creatures who enter the area of effect for 60 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 43)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.IncendiaryBomb3);
        }

        private void GasBomb()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.GasBomb)
                .Name("Gas Bomb")

                .AddPerkLevel()
                .Description("Creates a poison field at the explosion site, dealing 4 poison DMG, scaling with your Perception Score, to all creatures who enter the area of effect for 18 seconds. Consumes explosives on use.")
                .RequirementSkill(SkillType.Devices, 16)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .GrantsFeat(FeatType.GasBomb1)

                .AddPerkLevel()
                .Description("Creates a poison field at the explosion site, dealing 12 poison DMG, scaling with your Perception Score, to all creatures who enter the area of effect for 30 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 34)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.GasBomb2)

                .AddPerkLevel()
                .Description("Creates a poison field at the explosion site, dealing 16 poison DMG, scaling with your Perception Score, to all creatures who enter the area of effect for 48 seconds. Consumes explosives on use.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 46)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.GasBomb3);
        }

        private void StealthGenerator()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.StealthGenerator)
                .Name("Stealth Generator")

                .AddPerkLevel()
                .Description("Grants invisibility to the user for 30 seconds.")
                .RequirementSkill(SkillType.Devices, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .GrantsFeat(FeatType.StealthGenerator1)

                .AddPerkLevel()
                .Description("Grants invisibility to the user for 60 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StealthGenerator2)

                .AddPerkLevel()
                .Description("Grants invisibility to the user for 2 minutes.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StealthGenerator3);
        }

        private void Flamethrower()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.Flamethrower)
                .Name("Flamethrower")

                .AddPerkLevel()
                .Description("Deals 20 fire DMG, scaling with your Perception modifier, to all targets within a cone in front of the user.")
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.Flamethrower1)

                .AddPerkLevel()
                .Description("Deals 50 fire DMG, scaling with your Perception modifier, to all targets within a cone in front of the user. Also has an 8DC reflex check to inflict Burning.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Flamethrower2)

                .AddPerkLevel()
                .Description("Deals 80 fire DMG, scaling with your Perception modifier, to all targets within a cone in front of the user. Also has a 12DC reflex check to inflict Burning.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Flamethrower3);
        }

        private void WristRocket()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.WristRocket)
                .Name("Wrist Rocket")

                .AddPerkLevel()
                .Description("Inflicts 20 fire DMG, scaling with your Perception modifier, to a single target.")
                .RequirementSkill(SkillType.Devices, 10)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.WristRocket1)

                .AddPerkLevel()
                .Description("Inflicts 45 fire DMG, scaling with your Perception modifier, to a single target. Also has an 8DC fortitude check to inflict Knockdown for 2 seconds.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WristRocket2)

                .AddPerkLevel()
                .Description("Inflicts 90 fire DMG, scaling with your Perception modifier, to a single target. Also has a 12DC fortitude check to inflict Knockdown for 2 seconds.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WristRocket3);
        }

        private void DeflectorShield()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.DeflectorShield)
                .Name("Deflector Shield")

                .AddPerkLevel()
                .Description("Grants temporary hit points to the user for a short period of time.")
                .RequirementSkill(SkillType.Devices, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .Price(2)
                .GrantsFeat(FeatType.DeflectorShield1)

                .AddPerkLevel()
                .Description("Grants temporary hit points to the user for a short period of time.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DeflectorShield2)

                .AddPerkLevel()
                .Description("Grants temporary hit points to the user and all nearby party members for a short period of time.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DeflectorShield3);
        }
    }
}
