
using Godot;

namespace EGame
{
    public class MonsterBehaviorNodePatrol : AbstractAgentBehaviorNode
    {
        private const float PatrolRadius = 6f;
        private const float ArriveDistance = 0.5f;

        private Vector3? _Target;

        protected override BehaviorStatus OnTick(NAgent agent, double dt)
        {
            _Target ??= agent.GlobalPosition + new Vector3(
                (float)GD.RandRange(-PatrolRadius, PatrolRadius), 0,
                (float)GD.RandRange(-PatrolRadius, PatrolRadius));

            Vector3 toTarget = _Target.Value - agent.GlobalPosition;
            toTarget.Y = 0;

            if (toTarget.Length() <= ArriveDistance)
            {
                _Target = null;
                agent.Intent.WishDir = Vector3.Zero;
                return BehaviorStatus.Success;
            }

            agent.Intent.WishDir = toTarget.Normalized();
            return BehaviorStatus.Running;
        }
    }
}
