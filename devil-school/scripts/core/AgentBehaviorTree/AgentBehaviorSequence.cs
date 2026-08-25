
namespace EGame
{
    // 依次执行子节点，全部 Success 才算这个 Sequence 成功——
    // 用来表达"一串必须按顺序都做到的步骤"，中途任何一步 Failure/Running，整个序列就停在那一步。
    // 跟 Selector 一样，每次 Tick 都从第一个子节点重新开始
    public class AgentBehaviorSequence : AbstractAgentBehaviorNode
    {
        private readonly AbstractAgentBehaviorNode[] _Children;

        public AgentBehaviorSequence(params AbstractAgentBehaviorNode[] children)
        {
            _Children = children;
        }

        public override BehaviorStatus Tick(NAgent agent, double dt)
        {
            foreach (var child in _Children)
            {
                var status = child.Tick(agent, dt);
                if (status != BehaviorStatus.Success)
                    return status;
            }
            return BehaviorStatus.Success;
        }
    }
}
