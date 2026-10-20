
using Godot;
using System.Collections.Generic;


namespace EGame
{
    public class SniperModel : RangedWeaponModel
    {
        public override WeaponType Type => WeaponType.Sniper;
        public override string ParentName => "hand_r";
        public override string FireSoundName => "awp_fire";
        public override int Attack => 50;

        private RangedRecoilCfg _RangedCfg = new RangedRecoilCfg()
        {
            AnimRecoilList = new List<Vector3>() {
                new Vector3(-6f, 0f, 0f)
            },
            AnimRecoilMaxAngle = new Vector3(2f, 0.5f, 0f),
            CameraRecoilMaxAngle = new Vector3(0.2f, 0.3f, 0),
            CameraRecoilScale = 1f,
            WeaponRecoilList = new List<Vector3>()
            {
                new Vector3(0f, 0.01f, -0.2f)
            },
        };

        public override RangedRecoilCfg RecoilCfg => _RangedCfg;
    }
}