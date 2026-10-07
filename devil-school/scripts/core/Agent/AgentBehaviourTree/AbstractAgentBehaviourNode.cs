
namespace EGame
{
    // 一个节点两个部分：决策（Decide，低频）和行为（OnEnter/OnProcess/OnExit，每帧）。
    // 决策返回 Failure 且之前没在跑 = 根本没进去过，什么都不触发
    public abstract class AbstractAgentBehaviourNode
    {
        public double RunningTime { get; private set; }
        protected bool IsActive { get; private set; }

        public BehaviourStatus Decide(NAgent agent)
        {
            BehaviourStatus status = OnDecide(agent);

            if (status == BehaviourStatus.Failure)
            {
                if (IsActive)
                    Exit(agent);
                return status;
            }

            if (!IsActive)
            {
                IsActive = true;
                OnEnter(agent);
            }

            if (status == BehaviourStatus.Success)
                Exit(agent);

            return status;
        }

        public void Process(NAgent agent, double dt)
        {
            if (!IsActive)
                return;

            RunningTime += dt;
            OnProcess(agent, dt);
        }

        // 被上层打断：正在跑就走一遍 OnExit，没在跑什么都不做
        public virtual void Abort(NAgent agent)
        {
            if (IsActive)
                Exit(agent);
        }

        // 正在跑时被再次触发：先退出再重新进入，让 OnEnter 里的东西（比如动画）重新来一遍
        protected void Restart(NAgent agent)
        {
            Exit(agent);
            IsActive = true;
            OnEnter(agent);
        }

        protected abstract BehaviourStatus OnDecide(NAgent agent);

        protected virtual void OnEnter(NAgent agent) { }

        protected virtual void OnProcess(NAgent agent, double dt) { }

        protected virtual void OnExit(NAgent agent) { }

        private void Exit(NAgent agent)
        {
            IsActive = false;
            RunningTime = 0;
            OnExit(agent);
        }
    }
}
