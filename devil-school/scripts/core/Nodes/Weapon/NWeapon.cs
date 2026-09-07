
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
            instance.RangedData = model;
            instance._Owner = player;
            instance.RangedData.OnWeaponCreated(instance);
            return instance;
        }

        public static NWeapon Create(NPlayer player, MeleeModel model)
        {
            var instance = SceneHelper.LoadScene<NWeapon>(model.PrefabName);
            instance.MeleeData = model;
            instance._Owner = player;
            instance.MeleeData.OnWeaponCreated(instance);
            return instance;
        }

        private Camera3D _RealCamera;
        private NPlayer _Owner;

        // 远程和近战是两套完全独立的 Model，一把武器只会用到其中一个，另一个是 null
        public WeaponModel RangedData { get; private set; }
        public MeleeModel MeleeData { get; private set; }

        public override void _Ready()
        {
            base._Ready();
            _RealCamera = _Owner.GetNode<Camera3D>("%RealCamera");
        }

        public Node3D ShootPos => _RealCamera;
        public float SwitchTime => RangedData != null ? RangedData.SwitchTime : MeleeData.SwitchTime;
        public string SwitchAnimTrigger => RangedData != null ? RangedData.SwitchAnimTrigger : MeleeData.SwitchAnimTrigger;
        public WeaponIntent Intent { get; set; } = new WeaponIntent();

        // 按下攻击键之后该进哪个状态：近战走连招状态，远程直接开火
        public string AttackStateName => MeleeData != null ? WeaponConfig.MeleeAttack : WeaponConfig.Fire;

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

        public void TriggerAnim(string trigger)
        {
            _Owner.AnimTrigger(trigger);
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

        // 远程：WeaponStateFire 进入时立刻打一发（近战不走这个状态，见 AttackStateName）
        public void FireInternal()
        {
            TriggerAnim(RangedData.FireAnimTrigger);
            FireRanged();
        }

        private void FireRanged()
        {
            var space_state = GetWorld3D().DirectSpaceState;
            var from = ShootPos.GlobalPosition;
            var to = from + (-_RealCamera.GlobalTransform.Basis.Z) * 100f;

            var query = PhysicsRayQueryParameters3D.Create(from, to);
            query.CollisionMask = RangedData.HitMask;

            var result = space_state.IntersectRay(query);
            if (result.Count > 0)
            {
                Node3D hitObject = (Node3D)result["collider"];
                Vector3 hitPoint = (Vector3)result["position"];
                Vector3 hitNormal = (Vector3)result["normal"];

                var damageInfo = new DamageInfo(hitObject, hitPoint, hitNormal, _Owner.Data, RangedData.Attack);
                DamageSystem.Instance.ReportHit(damageInfo);
            }
        }
    }
}
