namespace SWLOR.Game.Server.Service.MissionService
{
    /// <summary>
    /// Kill a specific target creature identified by tag (e.g. a boss). Completes on its death.
    /// </summary>
    public class AssassinateObjective : MissionObjective
    {
        private readonly string _targetTag;

        public AssassinateObjective(string targetTag)
        {
            _targetTag = targetTag;
        }

        public override string Description => $"Assassinate the target [{_targetTag}]";

        public override void OnCreatureKilled(uint creature)
        {
            if (IsComplete) return;
            if (GetTag(creature) == _targetTag)
                IsComplete = true;
        }
    }
}
