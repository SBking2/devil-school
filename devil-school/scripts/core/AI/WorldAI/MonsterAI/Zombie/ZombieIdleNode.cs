using Godot;
namespace EGame
{
    public class ZombieIdleNode : WorldNodeAction
    {
        public ZombieIdleNode() : base("idle")
        {

        }

        public override void OnEnter(WorldBehaviorContext context)
        {
            context.Owner.TargetMoveDir = Vector3.Zero;
        }

        protected override WorldBehaviorStatus OnTick(WorldBehaviorContext context)
        {
            if (RunningTime > ZombieAI.IdleTime)
                return WorldBehaviorStatus.Success;
            
            return WorldBehaviorStatus.Running;
        }
    }
}
