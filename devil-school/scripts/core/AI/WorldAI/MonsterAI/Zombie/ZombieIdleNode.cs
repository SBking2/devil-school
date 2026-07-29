
namespace EGame
{
    public class ZombieIdleNode : WorldNodeAction
    {
        public ZombieIdleNode() : base("idle")
        {

        }

        public override void OnEnter(WorldBehaviorContext context)
        {

        }

        protected override WorldBehaviorStatus OnTick(WorldBehaviorContext context)
        {
            return WorldBehaviorStatus.Failure;
        }
    }
}
