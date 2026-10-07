
using Godot;

namespace EGame
{
    // 挨打之后：播一次受伤动画，并且这段时间不能动。
    // TookDamage 事件只在触发的那次决策里可见，所以用自己的倒计时撑住持续时间，倒计时走完就要求立刻重新决策
    public class MonsterBehaviourNodeHurt : AbstractAgentBehaviourNode
    {
        private double _HurtTimer;

        protected override BehaviourStatus OnDecide(NAgent agent)
        {
            if (agent.HasEvent("TookDamage"))
            {
                _HurtTimer = GetHitTime();
                if (IsActive)
                    Restart(agent);
            }

            return _HurtTimer > 0 ? BehaviourStatus.Running : BehaviourStatus.Failure;
        }

        protected override void OnEnter(NAgent agent)
        {
            agent.AnimTrigger(AnimationConfig.HurtTrigger);
        }

        protected override void OnProcess(NAgent agent, double dt)
        {
            _HurtTimer -= dt;

            agent.Intent.WishDir = Vector3.Zero;
            agent.Velocity = new Vector3(0, agent.Velocity.Y, 0);    // 清掉挨打前积累的水平动量，不然要靠摩擦力慢慢衰减，会滑一小段

            if (_HurtTimer <= 0)
                agent.RequestDecision();
        }
        
        protected virtual float GetHitTime()
        {
            return 1f;
        }
    }
}
