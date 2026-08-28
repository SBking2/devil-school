
namespace EGame
{
    public abstract class AbstractAgentBehaviorNode
    {
        public double RunningTime { get; private set; }

        public BehaviorStatus Tick(NAgent agent, double dt)
        {
            var status = OnTick(agent, dt);
            RunningTime = status == BehaviorStatus.Running ? RunningTime + dt : 0;
            return status;
        }

        protected abstract BehaviorStatus OnTick(NAgent agent, double dt);

        public virtual void ResetRunning()
        {
            RunningTime = 0;
        }
    }
}
