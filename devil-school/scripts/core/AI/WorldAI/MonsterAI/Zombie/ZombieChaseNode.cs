
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
            object v = null;
            if(context.Blackboard.TryGetValue(ZombieAI.TargetKey, out v))
            {
                var target = v as NEnvCreature;
                if(target != null)
                {
                    if ((context.Owner.GlobalPosition - target.GlobalPosition).Length() < 3f)
                        return WorldBehaviorStatus.Success;

                    context.Owner.Intent.MoveDir = (target.GlobalPosition - context.Owner.GlobalPosition).Normalized();
                    return WorldBehaviorStatus.Running;
                }
            }

            return WorldBehaviorStatus.Failure;
        }

        public override void OnExit(WorldBehaviorContext context)
        {
            context.Owner.Intent.MoveDir = Vector3.Zero;
        }
    }
}
