
using Godot;

namespace EGame
{
    public class MonsterBehaviorNodeIdle : AbstractAgentBehaviorNode
    {
        private const double IdleDuration = 2.0;

        protected override BehaviorStatus OnTick(NAgent agent, double dt)
        {
            if (RunningTime <= 0)
                agent.AnimTrigger(AnimationConfig.IdleTrigger);

            agent.Intent.WishDir = Vector3.Zero;
            return RunningTime >= IdleDuration ? BehaviorStatus.Success : BehaviorStatus.Running;
        }
    }
}
