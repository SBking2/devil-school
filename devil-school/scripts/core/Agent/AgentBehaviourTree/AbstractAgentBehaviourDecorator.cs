
namespace EGame
{
    // 包一个子节点，每次决策都重新判断 ShouldRun——条件一旦不成立就打断子节点，不管子节点是不是正在 Running
    public abstract class AbstractAgentBehaviourDecorator : AbstractAgentBehaviourNode
    {
        protected readonly AbstractAgentBehaviourNode Child;

        protected AbstractAgentBehaviourDecorator(AbstractAgentBehaviourNode child)
        {
            Child = child;
        }

        protected abstract bool ShouldRun(NAgent agent);

        protected sealed override BehaviourStatus OnDecide(NAgent agent)
        {
            if (!ShouldRun(agent))
            {
                Child.Abort(agent);
                return BehaviourStatus.Failure;
            }
            return Child.Decide(agent);
        }

        protected sealed override void OnProcess(NAgent agent, double dt)
        {
            Child.Process(agent, dt);
        }

        public override void Abort(NAgent agent)
        {
            Child.Abort(agent);
            base.Abort(agent);
        }
    }
}
