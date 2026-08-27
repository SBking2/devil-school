
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

        private Camera3D _RealCamera;
        private Node3D _Muzzle;
        private Node3D _Recoil;    // 开火后坐只作用在这个子节点上，武器摇摆由 NPlayerController 直接驱动对应节点的 Transform

        private readonly float _SwitchTime = 0.3f;
        private readonly float _ReloadingTime = 1.5f;
        private readonly float _FireGap = 0.2f;

        private readonly int _ClipSize = 25;    //弹匣大小
        private int _ReserveAmmo = 12;         //剩余弹数
        private int _CurrentAmmo = 24;

        private float _Timer = 0f;

        public INCharacter Shooter { get; private set; }

        // 命中检测用哪些层，实例可以按需覆盖；默认打环境+怪物+玩家
        public uint HitMask { get; set; } = (uint)(CollisionMask.GrandMask | CollisionMask.MonsterMask | CollisionMask.PlayerMask);

        private const int _Damage = 10;    // 单发伤害

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////                                      开火后坐：时间窗口模型
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private readonly float _RecoilKickDegrees = 3.0f;
        private readonly float _MuzzleKickTime = 0.08f;      // 每次开火，kick_end_time 往后顶多少——叠加速度
        private readonly float _MuzzleKickMaxTime = 0.35f;   // kick_end_time 最多能顶到多远的将来——叠加总上限

        private double _KickEndTime;

        // 每次开火调用一次：把 kick_end_time 往后"顶"一段增量，而不是重置成固定窗口——
        // 这样连续快速开火会一次次把 kick_end_time 往后推，直到撞上 _MuzzleKickMaxTime 才封顶，
        // 效果是越打越往上顶、顶满为止，不是每次都顶到同一个高度
        private void ApplyMuzzleKick()
        {
            double now = Time.GetTicksMsec() / 1000.0;
            if (_KickEndTime < now) _KickEndTime = now;
            _KickEndTime += _MuzzleKickTime;
            if (_KickEndTime > now + _MuzzleKickMaxTime) _KickEndTime = now + _MuzzleKickMaxTime;
        }

        // 每帧读取"kick_end_time 距离现在还有多久"，换算成当前应该顶到的角度——
        // 操作的是武器模型自己的局部旋转，不影响玩家真实瞄准方向
        private float CurrentRecoilAngle()
        {
            double now = Time.GetTicksMsec() / 1000.0;
            double remaining = _KickEndTime - now;
            if (remaining <= 0) return 0;
            if (remaining > _MuzzleKickMaxTime) remaining = _MuzzleKickMaxTime;
            float amount = (float)(remaining / _MuzzleKickMaxTime);
            return _RecoilKickDegrees * amount;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 做射线检测
        /// </summary>
        private void FireInternal()
        {
            var space_state = GetWorld3D().DirectSpaceState;
            var from = _Muzzle.GlobalPosition;
            var to = from + (-_RealCamera.GlobalTransform.Basis.Z) * 100f;

            var query = PhysicsRayQueryParameters3D.Create(from, to);
            query.CollisionMask = HitMask;

            var result = space_state.IntersectRay(query);
            if(result.Count > 0)
            {
                Node3D hitObject = (Node3D)result["collider"];
                Vector3 hitPoint = (Vector3)result["position"];
                Vector3 hitNormal = (Vector3)result["normal"];

                var damageInfo = new DamageInfo(hitObject, hitPoint, hitNormal, Shooter, _Damage);
                DamageSystem.Instance.ReportHit(damageInfo);
            }

            ApplyMuzzleKick();
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

        public override void _Ready()
        {
            base._Ready();

            _RealCamera = GetNode<Camera3D>("%RealCamera");
            _Muzzle = _RealCamera;    // 目前枪口射线就是从摄像机原点发出的，没有单独的枪口节点
            _Recoil = GetNodeOrNull<Node3D>("%Recoil");

            Shooter = GetOwner() as INCharacter;    // player.tscn 里 WeaponHolder 的 Owner 就是 PlayerController

            Equip();
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

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);

            if (_CurrentState == WeaponState.UnEquiped || _Recoil == null)
                return;

            _Recoil.RotationDegrees = new Vector3(-CurrentRecoilAngle(), 0, 0);
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
