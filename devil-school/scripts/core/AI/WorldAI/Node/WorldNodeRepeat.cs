namespace EGame
{
    public class WorldNodeRepeat : WorldNodeDecorator
    {
        public WorldNodeRepeat(string id, AbstractWorldBehaviorNode child) : base(id, child)
        {

        }

        protected override WorldBehaviorStatus DecorateStatus(WorldBehaviorContext context, WorldBehaviorStatus status)
        {
            if (status == WorldBehaviorStatus.Success)
                return WorldBehaviorStatus.Running;

            return status;
        }
    }
}
