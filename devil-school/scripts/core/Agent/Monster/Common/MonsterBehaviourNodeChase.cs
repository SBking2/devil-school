
using Godot;

namespace EGame
{
    public class MonsterBehaviourNodeChase : AbstractAgentBehaviourNode
    {
        protected override BehaviourStatus OnDecide(NAgent agent)
        {
            return NGame.Instance?.PlayerNode == null ? BehaviourStatus.Failure : BehaviourStatus.Running;
        }

        protected override void OnEnter(NAgent agent)
        {
            agent.AnimTrigger(AnimationConfig.WalkTrigger);
        }

        protected override void OnProcess(NAgent agent, double dt)
        {
            var player = NGame.Instance?.PlayerNode;
            if (player == null)
                return;

            Vector3 toPlayer = player.GlobalPosition - agent.GlobalPosition;
            toPlayer.Y = 0;

            agent.Intent.WishDir = toPlayer.Length() > 0.01f ? toPlayer.Normalized() : Vector3.Zero;
        }
    }
}
