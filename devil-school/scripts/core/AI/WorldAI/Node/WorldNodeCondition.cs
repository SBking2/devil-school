
namespace EGame
{
    public abstract class WorldNodeCondition : AbstractWorldBehaviorNode
    {
        private readonly string _NodeID;

        public override string ID => _NodeID;

        protected WorldNodeCondition(string id)
        {
            _NodeID = id;
        }

        protected override WorldBehaviorStatus OnTick(WorldBehaviorContext context)
        {
            return CheckCondition(context) ? WorldBehaviorStatus.Success : WorldBehaviorStatus.Failure;
        }

        protected abstract bool CheckCondition(WorldBehaviorContext context);
    }
}
