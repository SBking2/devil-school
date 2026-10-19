
using Godot;
using System;
using System.Collections.Generic;
using static Godot.Image;

namespace EGame
{
    public partial class NWeapon : Node3D
    {
        [Export] public RotationCurve[] AttackCameraRotationCurves;

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

        // PlayerActionStateMelee 维护，当前打到第几段连击，命中时用这个查这一下的伤害
        public int CurrentComboIndex { get; set; }
        public NMeleeCast MeleeCast { get; private set; }
        public Node3D ShootPos => _Owner.ShootPos;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                        创建
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        public NPlayer Player => _Owner;

        private NPlayer _Owner;

        public static NWeapon Create(NPlayer player, RangedWeaponModel model)
        {
            var instance = SceneHelper.LoadScene<NWeapon>(model.PrefabName);
            instance.RangedData = model;
            instance._Owner = player;
            return instance;
        }

        public static NWeapon Create(NPlayer player, MeleeWeaponModel model)
        {
            var instance = SceneHelper.LoadScene<NWeapon>(model.PrefabName);
            instance.MeleeData = model;
            instance._Owner = player;
            return instance;
        }

        public override void _Ready()
        {
            base._Ready();
            MeleeCast = GetNodeOrNull<NMeleeCast>("%ShapeCast");
            if (MeleeCast != null)
                MeleeCast.OnHit += OnMeleeHit;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                        装备与动画
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Equip()
        {
            this.SetActive(true);
        }

        public void UnEquip()
        {
            this.SetActive(false);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                        近战
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void OnMeleeHit(Node3D target, Vector3 point, Vector3 normal)
        {
            int damage = this.MeleeData.GetDamage(this.CurrentComboIndex);
            var damageInfo = new DamageInfo(_Owner, target, point, normal, _Owner.Data, damage);
            DamageSystem.Instance.ReportHit(damageInfo);
            SoundManager.Instance.Play("sword_hit");

            PlayMeleeHitEffect(point);
        }

        private void PlayMeleeHitEffect(Vector3 position)
        {
            var effect = PoolManager.Instance.Get("effect/greate_sword_hit");
            GpuParticles3D e = effect as GpuParticles3D;
            e.GlobalPosition = position;
            e.Restart();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////                                        远程
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void OnRangedHit(Node3D target, Vector3 hit_point, Vector3 hit_normal)
        {
            var damageInfo = new DamageInfo(_Owner, target, hit_point, hit_normal, _Owner.Data, RangedData.Attack);
            DamageSystem.Instance.ReportHit(damageInfo);
            if(target is INDamageable)
            {
                SoundManager.Instance.Play("fire_hit");
            }
            else
            {
                var decal = PoolManager.Instance.Get("effect/bullet_decal");
                decal.Position = hit_point;
                Quaternion rotation = new Quaternion(Vector3.Up, hit_normal.Normalized());
                decal.Quaternion = rotation;
                Timer.Instance.SetTimerTask(5.0f, () =>
                {
                    PoolManager.Instance.Push("effect/bullet_decal", decal);
                });
            }
        }

        // 一次开火打 _ShotgunPelletCount 颗弹丸，每颗各自算一份伤害
        private const int _ShotgunPelletCount = 5;
        private const float _ShotgunSpreadDegrees = 4f;

        public void FireInternal()
        {
            this.Player.AnimTrigger(RangedData.GetAttackTrigger());
            this.Player.OnFireHandleRecoil();
            SoundManager.Instance.Play(this.RangedData.FireSoundName);
            FireRanged();
        }

        public void FireShotgunInternal()
        {
            this.Player.AnimTrigger(RangedData.GetAttackTrigger());
            SoundManager.Instance.Play("fire");
            FireShotgun();
        }

        private void FireRanged()
        {
            var from = ShootPos.GlobalPosition;
            var to = from + (ShootPos.GlobalTransform.Basis.Z) * RangedData.Range;
            uint mask = (uint)(CollisionMask.GrandMask | CollisionMask.MonsterMask);

            if (CollisionDetection.FindRayTarget(GetWorld3D(), from, to, mask, out Node3D hitObject, out Vector3 hitPoint, out Vector3 hitNormal))
            {
                OnRangedHit(hitObject, hitPoint, hitNormal);
            }
        }

        private void FireShotgun()
        {
            Vector3 origin = ShootPos.GlobalPosition;
            Vector3 forward = -ShootPos.GlobalTransform.Basis.Z;
            Vector3 right = ShootPos.GlobalTransform.Basis.X;
            Vector3 up = ShootPos.GlobalTransform.Basis.Y;
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
