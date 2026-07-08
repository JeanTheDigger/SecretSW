using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// How elimination is scored for a two-side match (owned by the match, read by the death intercept and
    /// the EliminateObjective so both agree on "respawn or out").
    /// </summary>
    public enum EliminationMode
    {
        SingleElimination, // one life each — first death takes a player out of the match
        LimitedLives,      // N lives each — a player is out after their Nth death
        SharedTickets      // a side shares a pool of N respawns; once empty, deaths are final
    }

    /// <summary>
    /// Outcome of registering a player death in a match.
    /// </summary>
    public enum SideDeathResult
    {
        NotParticipant, // the dead PC is on no side / no match here — handle death normally
        Respawn,        // the PC has a life/ticket left — respawn them in the arena
        Eliminated      // the PC is out of the match (no respawn)
    }

    /// <summary>
    /// Two-side hostility engine for PvP missions/arenas. A "side" is a Mission-owned team (a set of PC
    /// UUIDs plus a list of allied NPC objects) that is DELIBERATELY orthogonal to the NWN party — a side is
    /// bigger than a party (its NPC reinforcements can't be party members) and overloading the party would
    /// leak kill-credit and put enemies on the party bar. This service only owns the RELATIONS: within a
    /// side everyone reads friendly, across sides everyone (and their companions) reads engine-HOSTILE, so
    /// PCs can attack each other and hostile-target abilities/AI "just work". Party force-grouping (for HUD /
    /// party-scoped kill credit / Enmity friendly-fire suppression) is a separate, later increment.
    ///
    /// Everything is scoped to a single area (the instance) and ephemeral: the area is set Full-PvP + the
    /// friendly-fire flag on start, and every personal-reputation relationship is torn down on end so
    /// hostility never leaks into the open world.
    ///
    /// Relations are keyed by PC UUID (a reconnecting PC is a NEW object id) and re-asserted centrally from
    /// OnAreaEnter, which fires on walk-in, teleport-in, and login-into-instance — so reconnect self-heals.
    /// </summary>
    public static class Side
    {
        /// <summary>
        /// Area local flag marking an active two-side match (cheap check for other systems, e.g. the death
        /// intercept). The authoritative state is <see cref="_matchesByArea"/>.
        /// </summary>
        public const string TwoSideAreaVariable = "MISSION_TWO_SIDE";

        /// <summary>
        /// Area local flag enabling friendly-fire AoE splash. Kept as a literal here (matching the value the
        /// Ability friendly-fire reader uses) so the Side engine does NOT depend on the friendly-fire AoE
        /// feature, which ships with the combat-upgrade branch. Setting this is forward-compatible and simply
        /// inert until that feature is present.
        /// </summary>
        public const string FriendlyFireAreaVariable = "MISSION_FRIENDLY_FIRE";

        private class SideData
        {
            public readonly HashSet<string> PlayerIds = new(); // PC UUIDs
            public readonly List<uint> Npcs = new();           // allied NPC object ids
            public Location SpawnLocation;                      // team respawn point (null = respawn in place)
            public int Tickets;                                 // remaining shared respawn tickets (SharedTickets)
        }

        private class Match
        {
            public uint Area;
            public PvPSetting OriginalPvP;
            public readonly Dictionary<string, SideData> Sides = new();

            // Match life-state (single source of truth for the death intercept + EliminateObjective).
            public EliminationMode Mode = EliminationMode.SingleElimination;
            public int LivesOrTickets = 1;
            public readonly Dictionary<string, int> LivesRemaining = new();   // PC UUID -> lives left (per-player modes)
            public readonly Dictionary<string, HashSet<string>> EliminatedBySide = new(); // side -> eliminated UUIDs

            // When true, an elimination taken inside a turn-based encounter is PERMADEATH (see the Death
            // intercept). Default false; only ever set by an explicit consent act (DM-declared / LETHAL mission).
            public bool Lethal;
        }

        private static readonly Dictionary<uint, Match> _matchesByArea = new();

        /// <summary>
        /// True if the given area currently has an active two-side match.
        /// </summary>
        public static bool HasMatch(uint area) => _matchesByArea.ContainsKey(area);

        /// <summary>
        /// True if the area's active match is flagged LETHAL — i.e. an elimination taken inside a turn-based
        /// encounter permanently retires the character. False when there is no match. Read by the Death intercept.
        /// </summary>
        public static bool IsLethalMatch(uint area) =>
            _matchesByArea.TryGetValue(area, out var match) && match.Lethal;

        /// <summary>
        /// Returns the side name a player belongs to in the area's match (by UUID, so it survives reconnect),
        /// or null if there is no match or the player is on no side. Read by PvP win-condition objectives.
        /// </summary>
        public static string GetPlayerSide(uint area, uint player)
        {
            return _matchesByArea.TryGetValue(area, out var match) ? FindPlayerSide(match, player) : null;
        }

        /// <summary>
        /// Returns the names of all sides registered in the area's match (empty if no match).
        /// </summary>
        public static IReadOnlyCollection<string> GetSideNames(uint area)
        {
            return _matchesByArea.TryGetValue(area, out var match)
                ? new List<string>(match.Sides.Keys)
                : new List<string>();
        }

        /// <summary>
        /// Returns the number of PCs registered to a side (its roster size, by UUID). 0 if no match/side.
        /// </summary>
        public static int GetSidePlayerCount(uint area, string sideName)
        {
            if (_matchesByArea.TryGetValue(area, out var match) && match.Sides.TryGetValue(sideName, out var side))
                return side.PlayerIds.Count;

            return 0;
        }

        /// <summary>
        /// Sets a side's team respawn point. Called at match spin-up (or via the DM test harness). If unset,
        /// a respawning player is resurrected in place.
        /// </summary>
        public static void SetSpawn(uint area, string sideName, Location location)
        {
            if (_matchesByArea.TryGetValue(area, out var match))
                GetOrAddSide(match, sideName).SpawnLocation = location;
        }

        /// <summary>
        /// Gets the respawn location for a player's side, if one was set. False if no match / no side / no spawn.
        /// </summary>
        public static bool TryGetRespawnLocation(uint area, uint player, out Location location)
        {
            location = null;
            if (!_matchesByArea.TryGetValue(area, out var match))
                return false;

            var side = FindPlayerSide(match, player);
            if (side == null || match.Sides[side].SpawnLocation == null)
                return false;

            location = match.Sides[side].SpawnLocation;
            return true;
        }

        /// <summary>
        /// Registers a player death against the match's life-state and reports whether they respawn or are out.
        /// Single source of truth consumed by BOTH the Death intercept (respawn vs. medcenter) and the
        /// EliminateObjective (win check). A player already eliminated stays Eliminated.
        /// </summary>
        public static SideDeathResult RegisterDeath(uint area, uint player)
        {
            if (!_matchesByArea.TryGetValue(area, out var match))
                return SideDeathResult.NotParticipant;

            var side = FindPlayerSide(match, player);
            if (side == null)
                return SideDeathResult.NotParticipant;

            var uuid = GetObjectUUID(player);
            if (IsEliminated(match, side, uuid))
                return SideDeathResult.Eliminated;

            switch (match.Mode)
            {
                case EliminationMode.SharedTickets:
                    var pool = match.Sides[side].Tickets;
                    if (pool > 0)
                    {
                        match.Sides[side].Tickets = pool - 1; // spend a shared respawn, player stays in
                        return SideDeathResult.Respawn;
                    }
                    Eliminate(match, side, uuid); // pool empty — this death is final
                    return SideDeathResult.Eliminated;

                default: // SingleElimination / LimitedLives
                    var startingLives = match.Mode == EliminationMode.SingleElimination ? 1 : match.LivesOrTickets;
                    var lives = (match.LivesRemaining.TryGetValue(uuid, out var l) ? l : startingLives) - 1;
                    match.LivesRemaining[uuid] = lives;
                    if (lives > 0)
                        return SideDeathResult.Respawn;
                    Eliminate(match, side, uuid);
                    return SideDeathResult.Eliminated;
            }
        }

        /// <summary>
        /// Returns the sole side still in the fight (all others fully eliminated), or null if the match is not
        /// yet decided. Requires at least two rostered sides. Read by the EliminateObjective on heartbeat.
        /// </summary>
        public static string GetMatchWinner(uint area)
        {
            if (!_matchesByArea.TryGetValue(area, out var match) || match.Sides.Count < 2)
                return null;

            string survivor = null;
            var sidesStillIn = 0;

            foreach (var kvp in match.Sides)
            {
                var rosterSize = kvp.Value.PlayerIds.Count;
                if (rosterSize == 0)
                    continue; // an empty side isn't in the fight

                if (EliminatedCount(match, kvp.Key) < rosterSize)
                {
                    sidesStillIn++;
                    survivor = kvp.Key;
                }
            }

            return sidesStillIn == 1 ? survivor : null;
        }

        private static void Eliminate(Match match, string side, string uuid)
        {
            if (!match.EliminatedBySide.TryGetValue(side, out var set))
            {
                set = new HashSet<string>();
                match.EliminatedBySide[side] = set;
            }

            set.Add(uuid);
        }

        private static bool IsEliminated(Match match, string side, string uuid)
        {
            return match.EliminatedBySide.TryGetValue(side, out var set) && set.Contains(uuid);
        }

        private static int EliminatedCount(Match match, string side)
        {
            return match.EliminatedBySide.TryGetValue(side, out var set) ? set.Count : 0;
        }

        /// <summary>
        /// Starts a two-side match in an area: flips it to Full-PvP (so PCs can damage PCs, which also allows
        /// within-side friendly fire), enables friendly-fire AoE, and flags it. Idempotent.
        /// </summary>
        public static void StartMatch(uint area, EliminationMode mode = EliminationMode.SingleElimination, int livesOrTickets = 1, bool lethal = false)
        {
            if (!GetIsObjectValid(area) || _matchesByArea.ContainsKey(area))
                return;

            var match = new Match
            {
                Area = area,
                OriginalPvP = AreaPlugin.GetPVPSetting(area),
                Mode = mode,
                LivesOrTickets = livesOrTickets <= 0 ? 1 : livesOrTickets,
                Lethal = lethal
            };
            _matchesByArea[area] = match;

            AreaPlugin.SetPVPSetting(area, PvPSetting.FullPvP);
            SetLocalBool(area, TwoSideAreaVariable, true);
            // In a two-side match, AoE splash should hit everyone in the blast (positioning matters).
            SetLocalBool(area, FriendlyFireAreaVariable, true);
        }

        /// <summary>
        /// Assigns a player to a side (creating the side if needed) and immediately (re)asserts their
        /// relations against everyone currently present. Membership is stored by UUID so it survives reconnect.
        /// </summary>
        public static void AssignPlayer(uint area, uint player, string sideName)
        {
            if (!_matchesByArea.TryGetValue(area, out var match) || !GetIsPC(player))
                return;

            var uuid = GetObjectUUID(player);

            // Remove from any other side first so a re-assign is clean.
            foreach (var existing in match.Sides.Values)
                existing.PlayerIds.Remove(uuid);

            GetOrAddSide(match, sideName).PlayerIds.Add(uuid);
            ReconcilePlayer(match, area, player);
            RegroupSideParty(match, area, sideName);
        }

        /// <summary>
        /// Registers an NPC as an ally of a side and asserts its relations. The caller MUST have already put
        /// the NPC on a neutral base faction synchronously (e.g. ChangeToStandardFaction(npc,
        /// StandardFaction.Commoner)) so it never own-team aggros before relations are applied.
        /// </summary>
        public static void AddNpc(uint area, uint npc, string sideName)
        {
            if (!_matchesByArea.TryGetValue(area, out var match) || !GetIsObjectValid(npc))
                return;

            var side = GetOrAddSide(match, sideName);
            if (!side.Npcs.Contains(npc))
                side.Npcs.Add(npc);

            ReconcileNpc(match, area, npc, sideName);
        }

        /// <summary>
        /// Ends the match: clears every personal-reputation relationship among the known combatants, resets
        /// each PC's standard faction reputation (mirrors Death.cs), restores the area's original PvP setting,
        /// and clears the flags. Bounded because everything is instance-scoped.
        /// </summary>
        public static void EndMatch(uint area)
        {
            if (!_matchesByArea.TryGetValue(area, out var match))
                return;

            var players = Area.GetPlayersInArea(area);

            // Reset standard faction reputation for PCs (same three factions Death.cs resets) and dissolve
            // the side parties we force-grouped.
            foreach (var player in players)
            {
                SetStandardFactionReputation(StandardFaction.Commoner, 100, player);
                SetStandardFactionReputation(StandardFaction.Merchant, 100, player);
                SetStandardFactionReputation(StandardFaction.Defender, 100, player);

                if (FindPlayerSide(match, player) != null)
                    Party.ForceRemove(player);
            }

            // Clear personal reputation across every combatant + companion pair, both directions.
            var entities = new List<uint>();
            foreach (var player in players)
                entities.AddRange(WithCompanion(player));
            foreach (var side in match.Sides.Values)
                foreach (var npc in side.Npcs)
                    if (GetIsObjectValid(npc))
                        entities.Add(npc);

            foreach (var a in entities)
                foreach (var b in entities)
                    if (a != b)
                        ClearPersonalReputation(b, a);

            AreaPlugin.SetPVPSetting(area, match.OriginalPvP);
            DeleteLocalBool(area, TwoSideAreaVariable);
            DeleteLocalBool(area, FriendlyFireAreaVariable);
            _matchesByArea.Remove(area);
        }

        /// <summary>
        /// Re-asserts relations for a PC on area entry — the master choke-point that also covers reconnect
        /// (login-into-instance fires OnAreaEnter), so a PC who dropped and came back as a new object id gets
        /// their side relations re-applied against everyone present.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void OnAreaEnter()
        {
            var area = OBJECT_SELF;
            if (!_matchesByArea.TryGetValue(area, out var match))
                return;

            var player = GetEnteringObject();
            if (!GetIsPC(player))
                return;

            // Only re-assert for players who are actually on a side.
            var side = FindPlayerSide(match, player);
            if (side != null)
            {
                ReconcilePlayer(match, area, player);
                RegroupSideParty(match, area, side);
            }
        }

        private static SideData GetOrAddSide(Match match, string sideName)
        {
            if (!match.Sides.TryGetValue(sideName, out var side))
            {
                side = new SideData { Tickets = match.LivesOrTickets };
                match.Sides[sideName] = side;
            }

            return side;
        }

        private static string FindPlayerSide(Match match, uint player)
        {
            var uuid = GetObjectUUID(player);
            foreach (var kvp in match.Sides)
            {
                if (kvp.Value.PlayerIds.Contains(uuid))
                    return kvp.Key;
            }

            return null;
        }

        private static void ReconcilePlayer(Match match, uint area, uint player)
        {
            var side = FindPlayerSide(match, player);
            if (side == null)
                return;

            var mine = WithCompanion(player);

            // Against every other registered player in the area (+ their companions).
            foreach (var other in Area.GetPlayersInArea(area))
            {
                if (other == player)
                    continue;

                var otherSide = FindPlayerSide(match, other);
                if (otherSide == null)
                    continue;

                var sameSide = otherSide == side;
                foreach (var a in mine)
                    foreach (var b in WithCompanion(other))
                        SetMutualRelation(a, b, sameSide);
            }

            // Against every allied/enemy NPC in the match.
            foreach (var kvp in match.Sides)
            {
                var sameSide = kvp.Key == side;
                foreach (var npc in kvp.Value.Npcs)
                {
                    if (!GetIsObjectValid(npc))
                        continue;

                    foreach (var a in mine)
                        SetMutualRelation(a, npc, sameSide);
                }
            }
        }

        private static void ReconcileNpc(Match match, uint area, uint npc, string npcSide)
        {
            // Against every registered player present (+ companions).
            foreach (var player in Area.GetPlayersInArea(area))
            {
                var playerSide = FindPlayerSide(match, player);
                if (playerSide == null)
                    continue;

                var sameSide = playerSide == npcSide;
                foreach (var b in WithCompanion(player))
                    SetMutualRelation(npc, b, sameSide);
            }

            // Against every other NPC in the match.
            foreach (var kvp in match.Sides)
            {
                var sameSide = kvp.Key == npcSide;
                foreach (var other in kvp.Value.Npcs)
                {
                    if (other == npc || !GetIsObjectValid(other))
                        continue;

                    SetMutualRelation(npc, other, sameSide);
                }
            }
        }

        /// <summary>
        /// Force-groups a side's present PCs into one NWN party so they share the party HUD, party-scoped
        /// kill credit, Enmity friendly-fire suppression, and party auras. Picks the first present, valid PC
        /// on the side as the party anchor and adds the rest to it; the Party.IsInParty guard + ForceAdd's
        /// idempotency make repeated calls (on each join / area-enter) safe. NPCs are never party-grouped
        /// (engine AddToParty is PC-only) — they are handled entirely by reputation.
        ///
        /// v1 limitation: a PC arriving already in an open-world party, or switching sides mid-match, is not
        /// reconciled here — end and restart the match for a clean regroup.
        /// </summary>
        private static void RegroupSideParty(Match match, uint area, string sideName)
        {
            var members = new List<uint>();
            foreach (var player in Area.GetPlayersInArea(area))
            {
                if (GetIsObjectValid(player) && FindPlayerSide(match, player) == sideName)
                    members.Add(player);
            }

            if (members.Count <= 1)
                return;

            var anchor = members[0];
            for (var i = 1; i < members.Count; i++)
            {
                if (!Party.IsInParty(anchor, members[i]))
                    Party.ForceAdd(anchor, members[i]);
            }
        }

        /// <summary>
        /// Sets mutual personal reputation between two creatures (both directions) to friend or enemy.
        /// bDecays=false (the API default) keeps it indefinite until we ClearPersonalReputation on teardown.
        /// </summary>
        private static void SetMutualRelation(uint a, uint b, bool sameSide)
        {
            if (!GetIsObjectValid(a) || !GetIsObjectValid(b) || a == b)
                return;

            if (sameSide)
            {
                SetIsTemporaryFriend(b, a);
                SetIsTemporaryFriend(a, b);
            }
            else
            {
                SetIsTemporaryEnemy(b, a);
                SetIsTemporaryEnemy(a, b);
            }
        }

        /// <summary>
        /// Expands a creature into itself plus its henchman companion (droid/beast) if present, so a player's
        /// companion inherits the same cross-side relations and can both attack and be attacked.
        /// </summary>
        private static List<uint> WithCompanion(uint creature)
        {
            var list = new List<uint> { creature };

            var companion = GetAssociate(AssociateType.Henchman, creature);
            if (GetIsObjectValid(companion))
                list.Add(companion);

            return list;
        }
    }
}
