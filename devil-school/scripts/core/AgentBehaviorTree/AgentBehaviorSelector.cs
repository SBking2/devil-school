
using System.Collections.Generic;

namespace EGame
{
    // 按优先级依次尝试子节点，第一个不是 Failure 的结果就是整体结果
    public class AgentBehaviorSelector : AbstractAgentBehaviorNode
    {
        private readonly List<AbstractAgentBehaviorNode> _Children = new List<AbstractAgentBehaviorNode>();

        public AgentBehaviorSelector(IReadOnlyList<AbstractAgentBehaviorNode> children)
        {
            _Children.AddRange(children);
        }

        public void Add(AbstractAgentBehaviorNode child)
        {
            _Children.Add(child);
        }

        protected override BehaviorStatus OnTick(NAgent agent, double dt)
        {
            for (int i = 0; i < _Children.Count; i++)
            {
                var status = _Children[i].Tick(agent, dt);
                if (status != BehaviorStatus.Failure)
                {
                    ResetFrom(i + 1);
                    return status;
                }
            }
            return BehaviorStatus.Failure;
        }

        public override void ResetRunning()
        {
            base.ResetRunning();
            ResetFrom(0);
        }

        private void ResetFrom(int index)
        {
            for (int i = index; i < _Children.Count; i++)
                _Children[i].ResetRunning();
        }
    }
}
