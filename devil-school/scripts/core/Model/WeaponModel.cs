
namespace EGame
{
    // 武器公共基类：远程(RangedWeaponModel)和近战(MeleeWeaponModel)在这里分叉，只放两边都要用的东西
    public abstract class WeaponModel : AbstractModel
    {
        // 武器类型：动画 trigger 名字统一按这个从 WeaponConfig 里查，不用每把武器自己声明一遍
        public enum WeaponType
        {
            Hand,
            Pistol,
            Sword,
        }

        public abstract string ParentName { get; }
        public abstract WeaponType Type { get; }
        public virtual string PrefabName => "weapon/" + ID.Entry.ToLowerInvariant();
        public virtual float SwitchTime => 0.3f;
        public string SwitchAnimTrigger => WeaponConfig.GetSwitchAnimTrigger(Type);

        public override void OnWeaponCreated(NWeapon weapon)
        {
            BuildStateMachine(weapon);
        }

        protected abstract void BuildStateMachine(NWeapon weapon);
    }
}
