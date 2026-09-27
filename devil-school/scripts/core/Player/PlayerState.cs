
namespace EGame
{
    public abstract class PlayerState
    {
        public abstract string StateName { get; }

        protected double _RunningTime = 0f;

        public virtual void OnEnter(NPlayer player)
        {
            _RunningTime = 0f;
        }

        public virtual void OnProcess(NPlayer player, double dt)
        {
            _RunningTime += dt;
        }

        public virtual void OnPhysicalProcess(NPlayer player, double dt)
        {

        }

        public virtual void OnExit(NPlayer player)
        {

        }
    }
}
