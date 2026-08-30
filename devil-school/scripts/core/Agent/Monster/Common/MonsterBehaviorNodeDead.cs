
using Godot;

namespace EGame
{
    // 死了就永远待在这个分支：动画只播一次，之后一直不能动，不会再掉回 chase/patrol
    public class MonsterBehaviorNodeDead : AbstractAgentBehaviorNode
    {
        private bool _IsDead;

        protected override BehaviorStatus OnTick(NAgent agent, double dt)
        {
            if (agent.HasEvent("Dead"))
                _IsDead = true;

            if (!_IsDead)
                return BehaviorStatus.Failure;

            if (RunningTime <= 0)
                agent.AnimTrigger(AnimationConfig.DeadTrigger);

            agent.Intent.WishDir = Vector3.Zero;
            agent.Velocity = new Vector3(0, agent.Velocity.Y, 0);
            return BehaviorStatus.Running;
        }
    }
}
