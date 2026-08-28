
namespace EGame
{
    public abstract class WeaponModel : AbstractModel
    {
        public abstract string PrefabName { get; }
        public abstract int Attack { get; }
    }
}