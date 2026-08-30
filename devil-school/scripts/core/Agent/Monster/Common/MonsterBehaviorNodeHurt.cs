
using Godot;

namespace EGame
{
    // 挨打之后：播一次受伤动画，并且这段时间不能动。靠自己这份倒计时锁住，
    // 因为 NotifyEvent 触发的事件只在触发的那一帧可见，不能直接拿 HasEvent 当持续状态用
    public class MonsterBehaviorNodeHurt : AbstractAgentBehaviorNode
    {
        private double _HurtTimer;

        protected override BehaviorStatus OnTick(NAgent agent, double dt)
        {
            if (agent.HasEvent("TookDamage"))
                _HurtTimer = GetHitTime();
            else
                _HurtTimer -= dt;

            if (_HurtTimer <= 0)
                return BehaviorStatus.Failure;

            if (RunningTime <= 0)
                agent.AnimTrigger(AnimationConfig.HurtTrigger);

            agent.Intent.WishDir = Vector3.Zero;
            agent.Velocity = new Vector3(0, agent.Velocity.Y, 0);    // 清掉挨打前积累的水平动量，不然要靠摩擦力慢慢衰减，会滑一小段
            return BehaviorStatus.Running;
        }

        protected virtual float GetHitTime()
        {
            return 0.8f;
        }
    }
}
