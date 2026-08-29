
namespace EGame
{
    public abstract class WeaponState
    {
        public abstract string StateName { get; }

        protected double _RunningTime = 0f;

        public virtual void OnEnter(NWeapon weapon)
        {
            _RunningTime = 0f;
        }

        public virtual void OnProcess(NWeapon weapon, double dt)
        {
            _RunningTime += dt;
        }

        public virtual void OnPhysicalProcess(NWeapon weapon, double dt)
        {

        }

        public virtual void OnExit(NWeapon weapon)
        {

        }
    }
}