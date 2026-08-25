
namespace EGame
{
    public abstract class AbstractAgentState
    {
        public abstract string StateName { get; }

        public virtual void OnEnter(NAgent agent)
        {

        }

        public virtual void OnProcess(NAgent agent, double dt)
        {

        }

        public virtual void OnPhysicalProcess(NAgent agent, double dt)
        {

        }

        public virtual void OnExit(NAgent agent)
        {

        }
    }
}