
using System.Collections.Generic;

namespace EGame
{
    // 子节点全部 Success 才算成功，中途 Failure/Running 就停在那一步
    public class AgentBehaviorSequence : AbstractAgentBehaviorNode
    {
        private readonly List<AbstractAgentBehaviorNode> _Children = new List<AbstractAgentBehaviorNode>();

        public AgentBehaviorSequence(IReadOnlyList<AbstractAgentBehaviorNode> children)
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
                if (status != BehaviorStatus.Success)
                {
                    ResetFrom(i + 1);
                    return status;
                }
            }
            return BehaviorStatus.Success;
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
