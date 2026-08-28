
using Godot;

namespace EGame
{
    public class MonsterBehaviorNodeChase : AbstractAgentBehaviorNode
    {
        protected override BehaviorStatus OnTick(NAgent agent, double dt)
        {
            var player = NGame.Instance?.PlayerNode;
            if (player == null)
                return BehaviorStatus.Failure;

            Vector3 toPlayer = player.GlobalPosition - agent.GlobalPosition;
            toPlayer.Y = 0;

            agent.Intent.WishDir = toPlayer.Length() > 0.01f ? toPlayer.Normalized() : Vector3.Zero;

            return BehaviorStatus.Running;
        }
    }
}
