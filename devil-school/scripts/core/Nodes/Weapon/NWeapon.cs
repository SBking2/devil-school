
using Godot;
using System;
using System.Collections.Generic;

namespace EGame
{
    public partial class NWeapon : Node3D
    {
        public static NWeapon Create(NPlayer player, WeaponModel model)
        {
            var instance = SceneHelper.LoadScene<NWeapon>(model.PrefabName);
            instance.Data = model;
            instance._Owner = player;
            instance.Data.OnWeaponCreated(instance);
            return instance;
        }

        private Camera3D _RealCamera;
        private NPlayer _Owner;
        public WeaponModel Data { get; private set; }

        public override void _Ready()
        {
            base._Ready();
            _RealCamera = _Owner.GetNode<Camera3D>("%RealCamera");
        }

        public Node3D ShootPos => _RealCamera;
        public float ReloadTime => Data.ReloadTime;
        public float SwitchTime => Data.SwitchTime;
        public float FireTime => Data.FireTime;
        public UInt16 HitMask => Data.HitMask;
        public WeaponIntent Intent { get; set; } = new WeaponIntent();

        public void Equip()
        {
            ChangeState(WeaponConfig.Switch);
            this.SetActive(true);
        }

        public void UnEquip()
        {
            Intent.Reset();
            ChangeState(WeaponConfig.Idle);
            this.SetActive(false);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                        State-Machine
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        private WeaponState _CurState;

        private Dictionary<string, WeaponState> _StateDic = new Dictionary<string, WeaponState>();

        public void BuildStateMachine(IEnumerable<WeaponState> states, WeaponState init_state)
        {
            if (_CurState != null)
                throw new InvalidOperationException("weapon already has state-machine!");

            foreach(var state in states)
                _StateDic.Add(state.StateName, state);

            ChangeState(init_state.StateName);
        }

        public void ChangeState(string name)
        {
            WeaponState new_state = null;
            if(_StateDic.TryGetValue(name, out new_state))
            {
                _CurState?.OnExit(this);
                _CurState = new_state;
                _CurState.OnEnter(this);
            }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            _CurState?.OnProcess(this, delta);
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            _CurState?.OnPhysicalProcess(this, delta);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                        State-Machine
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 做射线检测
        /// </summary>
        public void FireInternal()
        {
            var space_state = GetWorld3D().DirectSpaceState;
            var from = ShootPos.GlobalPosition;
            var to = from + (-_RealCamera.GlobalTransform.Basis.Z) * 100f;

            var query = PhysicsRayQueryParameters3D.Create(from, to);
            query.CollisionMask = HitMask;

            var result = space_state.IntersectRay(query);
            if (result.Count > 0)
            {
                Node3D hitObject = (Node3D)result["collider"];
                Vector3 hitPoint = (Vector3)result["position"];
                Vector3 hitNormal = (Vector3)result["normal"];

                var damageInfo = new DamageInfo(hitObject, hitPoint, hitNormal, _Owner.Data, Data.Attack);
                DamageSystem.Instance.ReportHit(damageInfo);
            }
        }
    }
}
