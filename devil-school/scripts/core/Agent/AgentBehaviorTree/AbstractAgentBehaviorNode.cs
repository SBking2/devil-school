
namespace EGame
{
    public abstract class AbstractAgentBehaviorNode
    {
        public double RunningTime { get; private set; }

        // 上一次 Tick 是不是 Running——判断"刚进入"要用这个，不能用 RunningTime<=0：
        // NotifyEvent 触发的那次插队 Tick 会传 dt=0，RunningTime 加 0 还是 0，会把紧接着的下一次正常 Tick 也误判成"刚进入"
        private bool _WasRunningLastTick;
        protected bool IsFirstTick => !_WasRunningLastTick;

        public BehaviorStatus Tick(NAgent agent, double dt)
        {
            var status = OnTick(agent, dt);
            RunningTime = status == BehaviorStatus.Running ? RunningTime + dt : 0;
            _WasRunningLastTick = status == BehaviorStatus.Running;
            return status;
        }

        protected abstract BehaviorStatus OnTick(NAgent agent, double dt);

        public virtual void ResetRunning()
        {
            RunningTime = 0;
            _WasRunningLastTick = false;
        }
    }
}
