
namespace EGame
{
    // 包一个子节点，每次 Tick 都重新判断 ShouldRun——条件一旦不成立就打断子节点，不管子节点是不是正在 Running
    public abstract class AbstractAgentBehaviorDecorator : AbstractAgentBehaviorNode
    {
        protected readonly AbstractAgentBehaviorNode Child;

        protected AbstractAgentBehaviorDecorator(AbstractAgentBehaviorNode child)
        {
            Child = child;
        }

        protected abstract bool ShouldRun(NAgent agent, double dt);

        protected sealed override BehaviorStatus OnTick(NAgent agent, double dt)
        {
            if (!ShouldRun(agent, dt))
            {
                Child.ResetRunning();
                return BehaviorStatus.Failure;
            }
            return Child.Tick(agent, dt);
        }

        public override void ResetRunning()
        {
            base.ResetRunning();
            Child.ResetRunning();
        }
    }
}
