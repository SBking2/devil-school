
namespace EGame
{
    public abstract class WeaponState
    {
        public abstract string StateName { get; }
        public virtual void OnEnter(NWeapon weapon)
        {

        }

        public virtual void OnProcess(NWeapon weapon, double dt)
        {

        }

        public virtual void OnPhysicalProcess(NWeapon weapon, double dt)
        {

        }

        public virtual void OnExit(NWeapon weapon)
        {

        }
    }
}