
using System.Collections.Generic;

namespace EGame
{
    // 按优先级依次尝试子节点，第一个不是 Failure 的结果就是整体结果，比它优先级低的子节点全部被打断
    public class AgentBehaviourSelector : AbstractAgentBehaviourNode
    {
        private readonly List<AbstractAgentBehaviourNode> _Children = new List<AbstractAgentBehaviourNode>();

        public AgentBehaviourSelector(IReadOnlyList<AbstractAgentBehaviourNode> children)
        {
            _Children.AddRange(children);
        }

        public void Add(AbstractAgentBehaviourNode child)
        {
            _Children.Add(child);
        }

        protected override BehaviourStatus OnDecide(NAgent agent)
        {
            for (int i = 0; i < _Children.Count; i++)
            {
                BehaviourStatus status = _Children[i].Decide(agent);
                if (status != BehaviourStatus.Failure)
                {
                    AbortFrom(i + 1, agent);
                    return status;
                }
            }
            return BehaviourStatus.Failure;
        }

        protected override void OnProcess(NAgent agent, double dt)
        {
            for (int i = 0; i < _Children.Count; i++)
                _Children[i].Process(agent, dt);
        }

        public override void Abort(NAgent agent)
        {
            AbortFrom(0, agent);
            base.Abort(agent);
        }

        private void AbortFrom(int index, NAgent agent)
        {
            for (int i = index; i < _Children.Count; i++)
                _Children[i].Abort(agent);
        }
    }
}
