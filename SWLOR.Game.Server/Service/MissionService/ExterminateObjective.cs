namespace SWLOR.Game.Server.Service.MissionService
{
    /// <summary>
    /// Kill a required number of creatures whose tag matches. Advances on each matching death.
    /// </summary>
    public class ExterminateObjective : MissionObjective
    {
        private readonly string _enemyTag;
        private readonly int _required;
        private int _killed;

        public ExterminateObjective(string enemyTag, int required)
        {
            _enemyTag = enemyTag;
            _required = required < 1 ? 1 : required;
        }

        public override string Description => $"Eliminate enemies [{_enemyTag}] ({_killed}/{_required})";

        public override void OnCreatureKilled(uint creature)
        {
            if (IsComplete) return;
            if (GetTag(creature) != _enemyTag) return;

            _killed++;
            if (_killed >= _required)
                IsComplete = true;
        }
    }
}
