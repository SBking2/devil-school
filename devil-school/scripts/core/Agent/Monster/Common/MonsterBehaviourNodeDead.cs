
using Godot;

namespace EGame
{
    // 死了就永远待在这个分支：进入后决策一直返回 Running，必须放在根 Selector 最前面，不然被打断后收不到 Dead 事件就进不来了
    public class MonsterBehaviourNodeDead : AbstractAgentBehaviourNode
    {
        protected override BehaviourStatus OnDecide(NAgent agent)
        {
            return IsActive || agent.HasEvent("Dead") ? BehaviourStatus.Running : BehaviourStatus.Failure;
        }

        protected override void OnEnter(NAgent agent)
        {
            agent.AnimTrigger(AnimationConfig.DeadTrigger);
        }

        protected override void OnProcess(NAgent agent, double dt)
        {
            agent.Intent.WishDir = Vector3.Zero;
            agent.Velocity = new Vector3(0, agent.Velocity.Y, 0);
        }
    }
}
