
using System;

namespace EGame
{
    // 远程武器专用 Model：走射线检测
    public abstract class RangedWeaponModel : WeaponModel
    {
        public RangedWeaponModel()
        {
            _CurrentAmmo = MagazineSize;
            _TotalAmmo = MaxTotalAmmo;
        }

        public virtual int Attack => 2;
        public virtual int MagazineSize => 10;
        public virtual int MaxTotalAmmo => 30;    // 备弹上限，不算弹匣里的
        public virtual float ReloadTime => 2f;
        public virtual float FireTime => 1f;
        public virtual float Range => 100f;

        // 弹药是运行时状态，只有 MutableClone 出来的副本才能改；current/total 任意一个变了都触发同一个事件，
        // 外部（HUD）不关心具体是哪个变了、变成多少，收到通知后自己现取最新值就行
        public event Action OnAmmoChanged;

        private int _CurrentAmmo;
        public int CurrentAmmo
        {
            get => _CurrentAmmo;
            set
            {
                AssertMutable();
                _CurrentAmmo = value;
                OnAmmoChanged?.Invoke();
            }
        }

        private int _TotalAmmo;
        public int TotalAmmo
        {
            get => _TotalAmmo;
            set
            {
                AssertMutable();
                _TotalAmmo = value;
                OnAmmoChanged?.Invoke();
            }
        }
    }
}
