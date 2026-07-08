using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class PilotingPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            Starships(builder);
            FlightStances(builder);
            FlightDoctrines(builder);
            DefensiveModules(builder);
            OffensiveModules(builder);
            EnergyManagement(builder);
            IntuitivePiloting(builder);

            return builder.Build();
        }

        // The three flight doctrine arcs (class-neutral). Levels 1-3 are the Phase-1 arc;
        // levels 4-6 unlock via flight recorders looted from events, on top of Phase-2
        // Piloting gates.
        private void FlightDoctrines(PerkBuilder builder)
        {
            BuildFlightDoctrine(builder, PerkType.DoctrineInterceptor, "Interceptor Doctrine",
                "The dogfighter's art: +{0} accuracy while flying a FIGHTER-class hull.");

            BuildFlightDoctrine(builder, PerkType.DoctrineStrike, "Strike Doctrine",
                "The capital-killer's art: +{0} damage on all ordnance weapons (missiles, torpedoes, bombs), on any hull.");

            BuildFlightDoctrine(builder, PerkType.DoctrineEscort, "Escort Doctrine",
                "The wingmate's art: +{0} evasion while flying a FIGHTER-class hull.");
        }

        private static void BuildFlightDoctrine(PerkBuilder builder, PerkType perkType, string name, string template)
        {
            var perLevel = perkType == PerkType.DoctrineStrike ? 2 : 1;
            var gates = new[] { 15, 30, 45, 60, 75, 90 };
            var prices = new[] { 2, 3, 3, 5, 5, 6 };

            var perk = builder.Create(PerkCategoryType.Piloting, perkType)
                .Name(name);

            for (var level = 1; level <= 6; level++)
            {
                perk.AddPerkLevel()
                    .Description(string.Format(template, perLevel * level))
                    .Price(prices[level - 1])
                    .RequirementSkill(SkillType.Piloting, gates[level - 1]);

                if (level == 4)
                    perk.RequirementUnlocked();
            }
        }

        private void FlightStances(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.FlightStances)
                .Name("Flight Stances")

                .AddPerkLevel()
                .Description("Grants the attack, evasive, and balanced flight stances, switched with the /flightmode command while piloting. Attack: +10 accuracy, -10 evasion. Evasive: the reverse.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 5);
        }

        private void Starships(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.Starships)
                .Name("Starships")

                .AddPerkLevel()
                .Description("Enables you to pilot tier 1 starships.")
                .Price(1)
                .GrantsFeat(FeatType.Starships1)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 2 starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)
                .GrantsFeat(FeatType.Starships2)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 3 starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(FeatType.Starships3)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 4 starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)
                .GrantsFeat(FeatType.Starships4)

                .AddPerkLevel()
                .Description("Enables you to pilot tier 5 starships.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 40)
                .GrantsFeat(FeatType.Starships5);
        }

        private void DefensiveModules(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.DefensiveModules)
                .Name("Defensive Modules")

                .AddPerkLevel()
                .Description("Enables you to attach tier 1 defensive modules on starships.")
                .Price(1)
                .GrantsFeat(FeatType.DefensiveModules1)

                .AddPerkLevel()
                .Description("Enables you to attach tier 2 defensive modules on starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)
                .GrantsFeat(FeatType.DefensiveModules2)

                .AddPerkLevel()
                .Description("Enables you to attach tier 3 defensive modules on starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(FeatType.DefensiveModules3)

                .AddPerkLevel()
                .Description("Enables you to attach tier 4 defensive modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)
                .GrantsFeat(FeatType.DefensiveModules4)

                .AddPerkLevel()
                .Description("Enables you to attach tier 5 defensive modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 40)
                .GrantsFeat(FeatType.DefensiveModules5);
        }

        private void OffensiveModules(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.OffensiveModules)
                .Name("Offensive Modules")

                .AddPerkLevel()
                .Description("Enables you to attach tier 1 offensive modules on starships.")
                .Price(1)
                .GrantsFeat(FeatType.OffensiveModules1)

                .AddPerkLevel()
                .Description("Enables you to attach tier 2 offensive modules on starships.")
                .Price(1)
                .RequirementSkill(SkillType.Piloting, 10)
                .GrantsFeat(FeatType.OffensiveModules2)

                .AddPerkLevel()
                .Description("Enables you to attach tier 3 offensive modules on starships.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(FeatType.OffensiveModules3)

                .AddPerkLevel()
                .Description("Enables you to attach tier 4 offensive modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 30)
                .GrantsFeat(FeatType.OffensiveModules4)

                .AddPerkLevel()
                .Description("Enables you to attach tier 5 offensive modules on starships.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 40)
                .GrantsFeat(FeatType.OffensiveModules5);
        }

        private void EnergyManagement(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.EnergyManagement)
                .Name("Energy Management")

                .AddPerkLevel()
                .Description("Reduces energy consumption of modules by 20%.")
                .Price(5)
                .RequirementSkill(SkillType.Piloting, 20)
                .GrantsFeat(FeatType.EnergyManagement1)

                .AddPerkLevel()
                .Description("Reduces energy consumption of modules by 40%.")
                .Price(5)
                .RequirementSkill(SkillType.Piloting, 40)
                .GrantsFeat(FeatType.EnergyManagement2);
        }

        private void IntuitivePiloting(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Piloting, PerkType.IntuitivePiloting)
                .Name("Intuitive Piloting")

                .AddPerkLevel()
                .Description("Allows for Willpower to be used in place of Perception for starship module effectiveness.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 0)
                .GrantsFeat(FeatType.IntuitivePiloting);
        }
    }
}
