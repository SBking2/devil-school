
using Godot;
using System.Collections.Generic;

namespace EGame
{
    public class RifleModel : RangedWeaponModel
    {
        public override WeaponType Type => WeaponType.Rifle;
        public override string ParentName => "hand_r";
        public override float FireTime => 0.05f;
        public override int MaxTotalAmmo => 1000;
        public override int MagazineSize => 100;
        public override string FireSoundName => "m4_fire";
        public override RangedRecoilCfg RecoilCfg => _RecoilCfg;
        private RangedRecoilCfg _RecoilCfg = new RangedRecoilCfg()
        {
            AnimRecoilList = new List<Vector3>()
            {
                new Vector3(-1.35f, 0.35f, 0f),
                new Vector3(-1.5f, 0.3f, 0f),
                new Vector3(-1.65f, 0.2f, 0f),
                // 往右摆
                new Vector3(-1.8f, -0.3f, 0f),
                new Vector3(-1.95f, -0.4f, 0f),
                new Vector3(-2.1f, -0.45f, 0f),
                new Vector3(-2.25f, -0.35f, 0f),
                // 最后 15 发循环：左右分段交替且幅度不等，一轮下来左右基本抵消
                new Vector3(-2.4f, 0.3f, 0f),
                new Vector3(-2.5f, 0.4f, 0f),
                new Vector3(-2.6f, 0.35f, 0f),
                new Vector3(-2.6f, 0.45f, 0f),
                new Vector3(-2.7f, 0.5f, 0f),
                new Vector3(-2.7f, 0.2f, 0f),
                new Vector3(-2.8f, 0.3f, 0f),
                new Vector3(-2.8f, 0.4f, 0f),
                new Vector3(-2.9f, -0.25f, 0f),
                new Vector3(-2.9f, -0.4f, 0f),
                new Vector3(-3.0f, -0.5f, 0f),
                new Vector3(-3.0f, -0.45f, 0f),
                new Vector3(-3.1f, -0.15f, 0f),
                new Vector3(-3.1f, -0.3f, 0f),
                new Vector3(-3.2f, -0.3f, 0f),
            },
            AnimRecoilLoopCount = 15,
            AnimRecoilMaxAngle = new Vector3(0.1f, 0.15f, 0.0f),
            AnimRecoilStopSpeed = 8f,
            AnimRecoilStopTime = 0.15,
            AnimRecoilRecoverTime = 0.8,

            CameraRecoilMaxAngle = new Vector3(0.2f, 0.3f, 0.0f),
            CameraRecoilScale = 0.45f,
            CameraRecoilStopSpeed = 12f,

            WeaponRecoilList = new List<Vector3>()
            {
                new Vector3(0f, 0.002f, -0.03f),
                new Vector3(0.002f, 0.004f, -0.04f),
                new Vector3(-0.002f, 0.004f, -0.05f),
                new Vector3(0.002f, 0.006f, -0.05f),
            },
            WeaponRecoilScale = new Vector3(0.3f, 0.5f, 0f),
            WeaponRecoilStopSpeed = 10f,
            WeaponRecoilStopTime = 0.15,
            WeaponRecoilRecoverTime = 0.8,
        };
    }
}
