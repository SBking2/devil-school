
namespace EGame
{
    public abstract class AbstractAgentBehaviorNode
    {
        public abstract BehaviorStatus Tick(NAgent agent, double dt);
    }
}
