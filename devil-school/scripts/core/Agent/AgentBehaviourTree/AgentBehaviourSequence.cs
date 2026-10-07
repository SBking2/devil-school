
using System.Collections.Generic;

namespace EGame
{
    // Sequence with memory：Running 就停在当前子节点，下次直接从这继续，不重新跑前面已经 Success 的子节点
    public class AgentBehaviourSequence : AbstractAgentBehaviourNode
    {
        private readonly List<AbstractAgentBehaviourNode> _Children = new List<AbstractAgentBehaviourNode>();
        private int _CurrentChild;

        public AgentBehaviourSequence(IReadOnlyList<AbstractAgentBehaviourNode> children)
        {
            _Children.AddRange(children);
        }

        public void Add(AbstractAgentBehaviourNode child)
        {
            _Children.Add(child);
        }

        protected override BehaviourStatus OnDecide(NAgent agent)
        {
            while (_CurrentChild < _Children.Count)
            {
                BehaviourStatus status = _Children[_CurrentChild].Decide(agent);
                if (status == BehaviourStatus.Running)
                    return BehaviourStatus.Running;

                if (status == BehaviourStatus.Failure)
                {
                    Restart(agent);
                    return BehaviourStatus.Failure;
                }

                _CurrentChild++;
            }

            Restart(agent);
            return BehaviourStatus.Success;
        }

        protected override void OnProcess(NAgent agent, double dt)
        {
            for (int i = 0; i < _Children.Count; i++)
                _Children[i].Process(agent, dt);
        }

        public override void Abort(NAgent agent)
        {
            Restart(agent);
            base.Abort(agent);
        }

        private void Restart(NAgent agent)
        {
            for (int i = 0; i < _Children.Count; i++)
                _Children[i].Abort(agent);
            _CurrentChild = 0;
        }
    }
}
