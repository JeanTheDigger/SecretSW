using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The cybernetic implant system (Standard characters only). Implants are passive perk
    /// lines: a character supports two installed lines (three after the Trials - a veteran
    /// body hardened to the surgery), enforced at purchase time. Swapping a line out rides
    /// the ordinary perk refund machinery. Each line's passive package is summed and cached
    /// here on purchase/refund/login; combat hot paths read the cache exactly like the
    /// stance cache, and save/might/speed bonuses apply as tagged permanent effects.
    /// </summary>
    public static class Implant
    {
        private const string ImplantEffectTag = "IMPLANT_EFFECTS";

        /// <summary>
        /// The seven implant lines.
        /// </summary>
        public static readonly PerkType[] ImplantLines =
        {
            PerkType.ImplantNeural,
            PerkType.ImplantOcular,
            PerkType.ImplantDermal,
            PerkType.ImplantSkeletal,
            PerkType.ImplantCardio,
            PerkType.ImplantServo,
            PerkType.ImplantCortical
        };

        /// <summary>
        /// The summed passive package of a creature's installed implants.
        /// </summary>
        public class ImplantPackage
        {
            public int AccuracyMod { get; set; }
            public int EvasionMod { get; set; }
            public int DefensePhysicalMod { get; set; }
            public int CritMod { get; set; }
            public int STMRegenPerTick { get; set; }
            public int SavingThrowMod { get; set; }
            public int MightBonus { get; set; }
            public int MoveSpeedPercent { get; set; }
        }

        // Cumulative values by perk level (index = level - 1).
        private static readonly int[] _neuralEvasion = { 2, 4, 6, 8, 10, 12 };
        private static readonly int[] _ocularAccuracy = { 2, 4, 6, 8, 10, 12 };
        private static readonly int[] _dermalDefense = { 2, 4, 6, 8, 10, 12 };
        private static readonly int[] _skeletalMight = { 1, 1, 2, 2, 3, 3 };
        private static readonly int[] _cardioSTMRegen = { 1, 2, 3, 4, 5, 6 };
        private static readonly int[] _servoSpeed = { 3, 6, 9, 12, 15, 18 };
        private static readonly int[] _corticalSaves = { 1, 2, 3, 4, 5, 6 };

        private static readonly Dictionary<uint, ImplantPackage> _cache = new();

        /// <summary>
        /// Retrieves the cached implant package for a creature, or null when none is installed.
        /// Hot-path safe: one dictionary lookup.
        /// </summary>
        public static ImplantPackage GetImplantPackage(uint creature)
        {
            return _cache.TryGetValue(creature, out var package) ? package : null;
        }

        /// <summary>
        /// The number of implant lines a character's body supports.
        /// </summary>
        public static int GetSlotLimit(Player dbPlayer)
        {
            return dbPlayer.HasCompletedTrials ? 3 : 2;
        }

        /// <summary>
        /// Counts the implant lines a character has installed (owns at any level).
        /// </summary>
        public static int CountInstalledLines(Player dbPlayer)
        {
            return ImplantLines.Count(line =>
                dbPlayer.Perks.ContainsKey(line) && dbPlayer.Perks[line] > 0);
        }

        /// <summary>
        /// Recomputes and caches a player's implant package and reapplies the tagged
        /// effects. Wired to implant perk purchases/refunds and to login.
        /// </summary>
        public static void Recalculate(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var package = new ImplantPackage();
            var anyInstalled = false;

            foreach (var line in ImplantLines)
            {
                var level = Perk.GetPerkLevel(player, line);
                if (level <= 0)
                    continue;
                if (level > 6)
                    level = 6;

                anyInstalled = true;
                var index = level - 1;

                if (line == PerkType.ImplantNeural)
                    package.EvasionMod += _neuralEvasion[index];
                else if (line == PerkType.ImplantOcular)
                {
                    package.AccuracyMod += _ocularAccuracy[index];
                    if (level >= 6)
                        package.CritMod += 5;
                }
                else if (line == PerkType.ImplantDermal)
                    package.DefensePhysicalMod += _dermalDefense[index];
                else if (line == PerkType.ImplantSkeletal)
                    package.MightBonus += _skeletalMight[index];
                else if (line == PerkType.ImplantCardio)
                    package.STMRegenPerTick += _cardioSTMRegen[index];
                else if (line == PerkType.ImplantServo)
                    package.MoveSpeedPercent += _servoSpeed[index];
                else if (line == PerkType.ImplantCortical)
                    package.SavingThrowMod += _corticalSaves[index];
            }

            if (anyInstalled)
                _cache[player] = package;
            else
                _cache.Remove(player);

            RemoveEffectByTag(player, ImplantEffectTag);

            if (package.SavingThrowMod > 0)
            {
                ApplyImplantEffect(player,
                    EffectSavingThrowIncrease((int)SavingThrow.All, package.SavingThrowMod));
            }

            if (package.MightBonus > 0)
            {
                ApplyImplantEffect(player, EffectAbilityIncrease(AbilityType.Might, package.MightBonus));
            }

            if (package.MoveSpeedPercent > 0)
            {
                ApplyImplantEffect(player, EffectMovementSpeedIncrease(package.MoveSpeedPercent));
            }
        }

        private static void ApplyImplantEffect(uint player, Effect effect)
        {
            var tagged = TagEffect(SupernaturalEffect(effect), ImplantEffectTag);
            ApplyEffectToObject(DurationType.Permanent, tagged, player);
        }

        /// <summary>
        /// Rebuilds the implant cache and effects when a player logs in.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void LoadOnEnter()
        {
            Recalculate(GetEnteringObject());
        }

        /// <summary>
        /// Clears cached implant state when a player leaves the server.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearOnExit()
        {
            _cache.Remove(GetExitingObject());
        }
    }
}
