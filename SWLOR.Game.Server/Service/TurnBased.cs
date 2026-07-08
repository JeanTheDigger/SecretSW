using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Turn-based tactical combat mode — INCREMENT 1: the sequencing engine.
    /// Freezes all combatants, rolls initiative (Agility + Perception + d10), and advances
    /// turns one combatant at a time, unfreezing only whoever is currently up.
    /// Later increments layer on: grid movement + per-turn budget, the one-action economy,
    /// the turn HUD, NPC AI turns, and permadeath. This increment is testable in ANY area
    /// via the /tbstart, /endturn, /tbend chat commands — no grid arena art required yet.
    /// Freeze pattern mirrors HoloCom (EffectCutsceneImmobilize + SetCommandable(false)).
    /// </summary>
    public static class TurnBased
    {
        private const string FreezeEffectTag = "TB_FREEZE";

        private class Encounter
        {
            public uint Area;
            public List<uint> Order = new();   // combatants in initiative order, highest first
            public int CurrentIndex;
            public int Round = 1;
        }

        // One encounter per area (the arena); plus a reverse lookup creature -> its encounter area.
        private static readonly Dictionary<uint, Encounter> _encountersByArea = new();
        private static readonly Dictionary<uint, uint> _creatureToArea = new();

        /// <summary>
        /// True if the creature is currently part of a turn-based encounter.
        /// </summary>
        public static bool IsInCombat(uint creature)
        {
            return _creatureToArea.ContainsKey(creature);
        }

        /// <summary>
        /// True if it is currently this creature's turn to act.
        /// </summary>
        public static bool IsActiveTurn(uint creature)
        {
            if (!_creatureToArea.TryGetValue(creature, out var area)) return false;
            if (!_encountersByArea.TryGetValue(area, out var enc)) return false;
            return enc.Order.Count > 0 && enc.Order[enc.CurrentIndex] == creature;
        }

        /// <summary>
        /// Initiative = effective Agility + Perception (GetAbilityScore includes gear/buffs) + d10.
        /// </summary>
        private static int RollInitiative(uint creature)
        {
            var agility = GetAbilityScore(creature, AbilityType.Agility);
            var perception = GetAbilityScore(creature, AbilityType.Perception);
            return agility + perception + Random.D10(1);
        }

        /// <summary>
        /// Starts a turn-based encounter in the given area, including every living PC and NPC creature present.
        /// </summary>
        public static void StartEncounter(uint area)
        {
            if (!GetIsObjectValid(area) || _encountersByArea.ContainsKey(area))
                return;

            var combatants = new List<(uint creature, int initiative)>();
            var obj = GetFirstObjectInArea(area);
            while (GetIsObjectValid(obj))
            {
                if (GetObjectType(obj) == ObjectType.Creature && !GetIsDM(obj) && !GetIsDead(obj))
                {
                    combatants.Add((obj, RollInitiative(obj)));
                }
                obj = GetNextObjectInArea(area);
            }

            if (combatants.Count == 0)
                return;

            var enc = new Encounter
            {
                Area = area,
                Order = combatants.OrderByDescending(c => c.initiative).Select(c => c.creature).ToList(),
                CurrentIndex = 0,
                Round = 1
            };
            _encountersByArea[area] = enc;

            foreach (var c in enc.Order)
            {
                _creatureToArea[c] = area;
                Freeze(c);
            }

            AnnounceOrder(enc);
            BeginTurn(enc);
        }

        /// <summary>
        /// Ends the encounter in the given area, unfreezing all combatants.
        /// </summary>
        public static void EndEncounter(uint area)
        {
            if (!_encountersByArea.TryGetValue(area, out var enc))
                return;

            foreach (var c in enc.Order)
            {
                Unfreeze(c);
                _creatureToArea.Remove(c);
            }
            _encountersByArea.Remove(area);

            foreach (var player in Area.GetPlayersInArea(area))
                SendMessageToPC(player, "The turn-based encounter has ended.");
        }

        /// <summary>
        /// Ends the given creature's turn and advances to the next combatant (only if it is their turn).
        /// </summary>
        public static void EndTurn(uint creature)
        {
            if (!_creatureToArea.TryGetValue(creature, out var area)) return;
            if (!_encountersByArea.TryGetValue(area, out var enc)) return;
            if (enc.Order.Count == 0) return;
            if (enc.Order[enc.CurrentIndex] != creature) return; // not this creature's turn

            AdvanceTurn(enc);
        }

        private static void AdvanceTurn(Encounter enc)
        {
            // Re-freeze whoever just acted.
            var previous = enc.Order[enc.CurrentIndex];
            if (GetIsObjectValid(previous))
                Freeze(previous);

            // Advance to the next living combatant; wrapping starts a new round.
            var safety = 0;
            do
            {
                enc.CurrentIndex++;
                if (enc.CurrentIndex >= enc.Order.Count)
                {
                    enc.CurrentIndex = 0;
                    enc.Round++;
                    foreach (var player in Area.GetPlayersInArea(enc.Area))
                        SendMessageToPC(player, $"--- Round {enc.Round} ---");
                }
                safety++;
            }
            while (safety <= enc.Order.Count &&
                   (!GetIsObjectValid(enc.Order[enc.CurrentIndex]) || GetIsDead(enc.Order[enc.CurrentIndex])));

            BeginTurn(enc);
        }

        private static void BeginTurn(Encounter enc)
        {
            var active = enc.Order[enc.CurrentIndex];
            if (!GetIsObjectValid(active))
                return;

            // NPC turns are driven by AI in a later increment. For now they auto-pass so the loop advances.
            if (!GetIsPC(active))
            {
                foreach (var player in Area.GetPlayersInArea(enc.Area))
                    SendMessageToPC(player, $"{GetName(active)} (NPC) - no AI turn yet, passing.");
                DelayCommand(1.0f, () => AdvanceTurn(enc));
                return;
            }

            Unfreeze(active);
            SendMessageToPC(active, "It is YOUR turn. Type /endturn when finished.");
            foreach (var player in Area.GetPlayersInArea(enc.Area))
            {
                if (player != active)
                    SendMessageToPC(player, $"It is {GetName(active)}'s turn.");
            }
        }

        private static void AnnounceOrder(Encounter enc)
        {
            var lines = enc.Order.Select((c, i) => $"{i + 1}. {GetName(c)}");
            var message = "Turn-based combat begins. Initiative order:\n" + string.Join("\n", lines);
            foreach (var player in Area.GetPlayersInArea(enc.Area))
                SendMessageToPC(player, message);
        }

        private static void Freeze(uint creature)
        {
            SetCommandable(false, creature);
            // Movement lock: cutscene immobilize is unresistable; supernatural + permanent so it persists until removed.
            var effect = TagEffect(SupernaturalEffect(EffectCutsceneImmobilize()), FreezeEffectTag);
            ApplyEffectToObject(DurationType.Permanent, effect, creature);
        }

        private static void Unfreeze(uint creature)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectTag(effect) == FreezeEffectTag)
                    RemoveEffect(creature, effect);
            }
            SetCommandable(true, creature);
        }
    }
}
