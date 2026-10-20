
using Godot;
using System.Collections.Generic;

namespace EGame
{
    public class RangedRecoilCfg
    {
        public static RangedRecoilCfg None = new RangedRecoilCfg()
        {
            AnimRecoilList = new List<Vector3>(),
            AnimRecoilLoopCount = 0,
            AnimRecoilMaxAngle = Vector3.Zero,
            AnimRecoilStopSpeed = 8f,
            AnimRecoilStopTime = 0.15,
            AnimRecoilRecoverTime = 0.3,

            CameraRecoilMaxAngle = Vector3.Zero,
            CameraRecoilScale = 0.45f,
            CameraRecoilStopSpeed = 12f,

            WeaponRecoilList = new List<Vector3>(),
            WeaponRecoilScale = new Vector3(0.3f, 0.5f, 0f),
            WeaponRecoilStopSpeed = 10f,
            WeaponRecoilStopTime = 0.15,
            WeaponRecoilRecoverTime = 0.3,
        };

        public List<Vector3> AnimRecoilList = new List<Vector3>();
        public int AnimRecoilLoopCount = 10;
        public Vector3 AnimRecoilMaxAngle = new Vector3(0.05f, 0.05f, 0f);
        public float AnimRecoilStopSpeed = 8f;
        public double AnimRecoilStopTime = 0.15;
        public double AnimRecoilRecoverTime = 0.3;

        public Vector3 CameraRecoilMaxAngle = new Vector3(0.2f, 0.3f, 0);
        public float CameraRecoilScale = 0.5f;
        public float CameraRecoilStopSpeed = 8f;

        public List<Vector3> WeaponRecoilList = new List<Vector3>();
        public Vector3 WeaponRecoilScale = Vector3.One * 0.4f;
        public float WeaponRecoilStopSpeed = 8f;
        public double WeaponRecoilStopTime = 0.15;
        public double WeaponRecoilRecoverTime = 0.3;
    }
}