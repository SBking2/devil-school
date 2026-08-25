
namespace EGame
{
    // 纯判断叶子节点，只需回答条件是否成立
    public abstract class AbstractAgentBehaviorCondition : AbstractAgentBehaviorNode
    {
        protected abstract bool Check(NAgent agent, double dt);

        protected sealed override BehaviorStatus OnTick(NAgent agent, double dt)
        {
            return Check(agent, dt) ? BehaviorStatus.Success : BehaviorStatus.Failure;
        }
    }
}
