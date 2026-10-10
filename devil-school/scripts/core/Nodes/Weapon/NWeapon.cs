
using Godot;
using System;
using System.Collections.Generic;

namespace EGame
{
    public partial class NWeapon : Node3D
    {
        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                        Create
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static NWeapon Create(NPlayer player, RangedWeaponModel model)
        {
            var instance = SceneHelper.LoadScene<NWeapon>(model.PrefabName);
            instance.RangedData = model;
            instance._Owner = player;

            instance.AttackCollision.BodyEntered += (Node3D body) =>
            {
                instance.OnMeleeHit(body);
            };

            return instance;
        }

        public static NWeapon Create(NPlayer player, MeleeWeaponModel model)
        {
            var instance = SceneHelper.LoadScene<NWeapon>(model.PrefabName);
            instance.MeleeData = model;
            instance._Owner = player;

            instance.AttackCollision.BodyEntered += (Node3D body) =>
            {
                instance.OnMeleeHit(body);
            };

            return instance;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                        Data
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        private Camera3D _RealCamera;
        private NPlayer _Owner;

        // 远程和近战是两套完全独立的 Model，一把武器只会用到其中一个，另一个是 null
        public RangedWeaponModel RangedData { get; private set; }
        public MeleeWeaponModel MeleeData { get; private set; }
        public WeaponModel Data
        {
            get
            {
                if(RangedData == null)
                    return MeleeData as WeaponModel;

                return RangedData as WeaponModel;
            }
        }

        // 近战武器身上开关的攻击判定区域，不是每把武器都有（远程武器就是 null）
        public Area3D AttackCollision { get; private set; }

        // PlayerActionStateMelee 维护，当前打到第几段连击，AttackCollision 命中时用这个查这一下的伤害
        public int CurrentComboIndex { get; set; }

        public Node3D ShootPos => _RealCamera;

        public override void _Ready()
        {
            base._Ready();
            _RealCamera = _Owner.GetNode<Camera3D>("%RealCamera");

            AttackCollision = GetNodeOrNull<Area3D>("%AttackCollision");
            if (AttackCollision != null)
                AttackCollision.Monitoring = false;    // 默认关着，只有攻击开窗那段时间才打开
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                        Equip
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Equip()
        {
            this.SetActive(true);
        }

        public void UnEquip()
        {
            this.SetActive(false);
        }

        public void TriggerAnim(string trigger)
        {
            _Owner.AnimTrigger(trigger);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                        Ranged Attack
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        // 近战武器的 AttackCollision 扫到目标时调用，伤害按这一下命中时的连击段数取
        private void OnMeleeHit(Node3D body)
        {
            Log.VeryDebug($"[MeleeHit] 打到了: {body.Name}");
            int damage = this.MeleeData.GetDamage(this.CurrentComboIndex);
            var damageInfo = new DamageInfo(_Owner, body, body.GlobalPosition, Vector3.Up, _Owner.Data, damage);
            DamageSystem.Instance.ReportHit(damageInfo);

            PlayMeleeHitEffect();
        }

        private void PlayMeleeHitEffect()
        {
            var effect = PoolManager.Instance.Get("effect/greate_sword_hit");
            GpuParticles3D e = effect as GpuParticles3D;
            e.GlobalPosition = this.GlobalPosition;
            e.Restart();
        }

        // 远程：什么时候开火由玩家的动作层状态决定，进入 Fire 状态时调用一次
        public void FireInternal()
        {
            TriggerAnim(WeaponConfig.GetAttackAnimTrigger(RangedData.Type, 0));
            SoundManager.Instance.Play("fire");
            FireRanged();
        }

        private void FireRanged()
        {
            var from = ShootPos.GlobalPosition;
            var to = from + (-_RealCamera.GlobalTransform.Basis.Z) * RangedData.Range;
            uint mask = (uint)(CollisionMask.GrandMask | CollisionMask.MonsterMask);

            if (CollisionDetection.FindRayTarget(GetWorld3D(), from, to, mask, out Node3D hitObject, out Vector3 hitPoint, out Vector3 hitNormal))
            {
                var damageInfo = new DamageInfo(_Owner, hitObject, hitPoint, hitNormal, _Owner.Data, RangedData.Attack);
                DamageSystem.Instance.ReportHit(damageInfo);
            }
        }

        // 霰弹枪：一次开火打 _ShotgunPelletCount 颗弹丸，每颗各自算一份伤害
        private const int _ShotgunPelletCount = 5;
        private const float _ShotgunSpreadDegrees = 4f;

        public void FireShotgunInternal()
        {
            TriggerAnim(WeaponConfig.GetAttackAnimTrigger(RangedData.Type, 0));
            SoundManager.Instance.Play("fire");
            FireShotgun();
        }

        private void FireShotgun()
        {
            Vector3 origin = ShootPos.GlobalPosition;
            Vector3 forward = -_RealCamera.GlobalTransform.Basis.Z;
            Vector3 right = _RealCamera.GlobalTransform.Basis.X;
            Vector3 up = _RealCamera.GlobalTransform.Basis.Y;
            uint mask = (uint)(CollisionMask.GrandMask | CollisionMask.MonsterMask);

            var hits = CollisionDetection.FindShotgunTargets(GetWorld3D(), origin, forward, right, up, RangedData.Range, mask, _ShotgunPelletCount, _ShotgunSpreadDegrees);
            foreach (var hit in hits)
            {
                var damageInfo = new DamageInfo(_Owner, hit.HitObject, hit.HitPoint, hit.HitNormal, _Owner.Data, RangedData.Attack);
                DamageSystem.Instance.ReportHit(damageInfo);
            }
        }
    }
}
