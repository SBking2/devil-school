
using System.Collections.Generic;

namespace EGame
{
    // Sequence with memory：Running 就停在当前子节点，下次直接从这继续，不重新跑前面已经 Success 的子节点
    public class AgentBehaviorSequence : AbstractAgentBehaviorNode
    {
        private readonly List<AbstractAgentBehaviorNode> _Children = new List<AbstractAgentBehaviorNode>();
        private int _CurrentChild;

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
            while (_CurrentChild < _Children.Count)
            {
                var status = _Children[_CurrentChild].Tick(agent, dt);
                if (status == BehaviorStatus.Running)
                    return BehaviorStatus.Running;

                if (status == BehaviorStatus.Failure)
                {
                    Restart();
                    return BehaviorStatus.Failure;
                }

                _CurrentChild++;
            }

            Restart();
            return BehaviorStatus.Success;
        }

        public override void ResetRunning()
        {
            base.ResetRunning();
            Restart();
        }

        private void Restart()
        {
            for (int i = 0; i < _Children.Count; i++)
                _Children[i].ResetRunning();
            _CurrentChild = 0;
        }
    }
}
