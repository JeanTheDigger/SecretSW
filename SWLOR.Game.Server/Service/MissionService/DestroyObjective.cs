namespace SWLOR.Game.Server.Service.MissionService
{
    /// <summary>
    /// Destroy a specific placeable identified by tag (e.g. a generator). Completes on its destruction.
    /// </summary>
    public class DestroyObjective : MissionObjective
    {
        private readonly string _placeableTag;

        public DestroyObjective(string placeableTag)
        {
            _placeableTag = placeableTag;
        }

        public override string Description => $"Destroy the target [{_placeableTag}]";

        public override void OnPlaceableDestroyed(uint placeable)
        {
            if (IsComplete) return;
            if (GetTag(placeable) == _placeableTag)
                IsComplete = true;
        }
    }
}
