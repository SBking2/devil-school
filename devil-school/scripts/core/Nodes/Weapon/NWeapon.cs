
using Godot;

namespace EGame
{
    public partial class NWeapon : Node3D
    {
        public enum WeaponState
        {
            Switch,
            Idle,
            Fire,
            Reloading,
            UnEquiped
        }

        private WeaponState _CurrentState = WeaponState.UnEquiped;

        [Export] private Camera3D _RealCamera;
        [Export] private Node3D _Muzzle;

        private readonly float _SwitchTime = 0.3f;
        private readonly float _ReloadingTime = 1.5f;
        private readonly float _FireGap = 0.2f;

        private readonly int _ClipSize = 25;    //弹匣大小
        private int _ReserveAmmo = 12;         //剩余弹数
        private int _CurrentAmmo = 24;

        private float _Timer = 0f;
        
        /// <summary>
        /// 做射线检测
        /// </summary>
        private void FireInternal()
        {
            var space_state = GetWorld3D().DirectSpaceState;
            var from = _Muzzle.GlobalPosition;
            var to = from + (-_RealCamera.GlobalTransform.Basis.Z) * 100f;
            
            var query = PhysicsRayQueryParameters3D.Create(from, to);
            query.CollisionMask = CollisionMask.GrandMask;

            var result = space_state.IntersectRay(query);
            if(result.Count > 0)
            {
                Node3D hitObject = (Node3D)result["collider"];
                GD.Print($"hit {hitObject.Name}");
            }
        }

        private bool TryFire()
        {
            if(_CurrentState == WeaponState.Idle && _CurrentAmmo > 0)
            {
                _CurrentState = WeaponState.Fire;
                FireInternal();
                _CurrentAmmo--;
                return true;
            }

            if (_CurrentState == WeaponState.Idle && _CurrentAmmo <= 0 && _ReserveAmmo > 0)
                StartReload();

            return false;
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            
            if(_CurrentState != WeaponState.UnEquiped)
            {
                _Timer += (float)delta;
                HandleStateChange();

                if (Input.IsActionPressed(EGInput.FIRE))
                    TryFire();
            }
        }

        public void Equip()
        {
            _Timer = 0f;
            _CurrentState = WeaponState.Switch;
        }

        public void UnEquip()
        {
            _Timer = 0f;
            _CurrentState = WeaponState.UnEquiped;
        }

        private void StartReload()
        {
            _Timer = 0f;
            _CurrentState = WeaponState.Reloading;
        }

        private void ReloadInternal()
        {
            int need_ammo = _ClipSize - _CurrentAmmo;
            need_ammo = Mathf.Min(need_ammo, _ReserveAmmo);
            _ReserveAmmo -= need_ammo;
            _CurrentAmmo += need_ammo;
        }

        private void HandleStateChange()
        {
            switch (_CurrentState)
            {
                case WeaponState.Switch:
                    if(_Timer > _SwitchTime)
                    {
                        _Timer = 0f;
                        _CurrentState = WeaponState.Idle;
                    }
                    break;
                case WeaponState.Reloading:
                    if( _Timer > _ReloadingTime)
                    {
                        _Timer = 0f;
                        _CurrentState = WeaponState.Idle;
                        ReloadInternal();
                    }
                    break;
                case WeaponState.Fire:
                    if(_Timer > _FireGap)
                    {
                        _CurrentState = WeaponState.Idle;
                        _Timer = 0f;
                    }
                    break;
            }
        }
    }
}