
using Godot;

namespace EGame
{
    public class MonsterBehaviourNodePatrol : AbstractAgentBehaviourNode
    {
        private const float PatrolRadius = 50f;
        private const float ArriveDistance = 0.5f;

        private Vector3 _Target;

        protected override BehaviourStatus OnDecide(NAgent agent)
        {
            if (!IsActive)
                return BehaviourStatus.Running;

            return HorizontalToTarget(agent).Length() <= ArriveDistance ? BehaviourStatus.Success : BehaviourStatus.Running;
        }

        protected override void OnEnter(NAgent agent)
        {
            agent.AnimTrigger(AnimationConfig.WalkTrigger);
            _Target = agent.GlobalPosition + new Vector3(
                (float)GD.RandRange(-PatrolRadius, PatrolRadius), 0,
                (float)GD.RandRange(-PatrolRadius, PatrolRadius));
        }

        protected override void OnProcess(NAgent agent, double dt)
        {
            Vector3 toTarget = HorizontalToTarget(agent);
            agent.Intent.WishDir = toTarget.LengthSquared() > 0.0001f ? toTarget.Normalized() : Vector3.Zero;
        }

        protected override void OnExit(NAgent agent)
        {
            agent.Intent.WishDir = Vector3.Zero;
        }

        private Vector3 HorizontalToTarget(NAgent agent)
        {
            Vector3 toTarget = _Target - agent.GlobalPosition;
            toTarget.Y = 0;
            return toTarget;
        }
    }
}
