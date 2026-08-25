
namespace EGame
{
    // 依次尝试子节点，第一个不是 Failure 的结果就是整个 Selector 的结果——
    // 用来表达"优先级"：数组靠前的选项优先，前面走不通（Failure）才轮到后面。
    // 每次 Tick 都从第一个子节点重新开始试，不记"上次停在哪个子节点"——
    // 现在还没有具体行为节点用得上"跨帧保持在同一个 Running 子节点"这个需求，先按最简单的来
    public class AgentBehaviorSelector : AbstractAgentBehaviorNode
    {
        private readonly AbstractAgentBehaviorNode[] _Children;

        public AgentBehaviorSelector(params AbstractAgentBehaviorNode[] children)
        {
            _Children = children;
        }

        public override BehaviorStatus Tick(NAgent agent, double dt)
        {
            foreach (var child in _Children)
            {
                var status = child.Tick(agent, dt);
                if (status != BehaviorStatus.Failure)
                    return status;
            }
            return BehaviorStatus.Failure;
        }
    }
}
