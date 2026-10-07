
using Godot;

namespace EGame
{
    public class MonsterBehaviourNodeIdle : AbstractAgentBehaviourNode
    {
        private const double IdleDuration = 2.0;

        protected override BehaviourStatus OnDecide(NAgent agent)
        {
            return RunningTime >= IdleDuration ? BehaviourStatus.Success : BehaviourStatus.Running;
        }

        protected override void OnEnter(NAgent agent)
        {
            agent.AnimTrigger(AnimationConfig.IdleTrigger);
        }

        protected override void OnProcess(NAgent agent, double dt)
        {
            agent.Intent.WishDir = Vector3.Zero;
        }
    }
}
