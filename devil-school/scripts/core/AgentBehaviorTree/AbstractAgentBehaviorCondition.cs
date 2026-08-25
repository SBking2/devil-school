
namespace EGame
{
    // 纯判断、不做事的叶子节点——子类只需要回答"条件是否成立"，
    // 不用关心 Running，这是 Sequence/Selector 里最常见的叶子类型。
    // 真正"会做事"的叶子（比如移动、攻击）直接继承 AbstractAgentBehaviorNode 就够了，
    // 不需要为它们再单独抽一个 Action 基类——Tick 本身就是它们要实现的全部内容
    public abstract class AbstractAgentBehaviorCondition : AbstractAgentBehaviorNode
    {
        protected abstract bool Check(NAgent agent, double dt);

        public sealed override BehaviorStatus Tick(NAgent agent, double dt)
        {
            return Check(agent, dt) ? BehaviorStatus.Success : BehaviorStatus.Failure;
        }
    }
}
