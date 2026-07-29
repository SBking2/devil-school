
using Godot;

namespace EGame
{
    public class ZombieChaseNode : WorldNodeAction
    {
        public ZombieChaseNode() : base("chase")
        {

        }

        protected override WorldBehaviorStatus OnTick(WorldBehaviorContext context)
        {
            return WorldBehaviorStatus.Failure;
        }

        public override void OnExit(WorldBehaviorContext context)
        {
            
        }
    }
}
