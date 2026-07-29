
using Godot;

namespace EGame
{
    public class ZombiePatrolNode : WorldNodeAction
    {
        public ZombiePatrolNode() : base("patrol")
        {

        }

        public override void OnEnter(WorldBehaviorContext context)
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
