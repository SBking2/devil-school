
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
        public int AnimRecoilLoopCount;
        public Vector3 AnimRecoilMaxAngle;
        public float AnimRecoilStopSpeed;
        public double AnimRecoilStopTime;
        public double AnimRecoilRecoverTime;

        public Vector3 CameraRecoilMaxAngle;
        public float CameraRecoilScale;
        public float CameraRecoilStopSpeed;

        public List<Vector3> WeaponRecoilList = new List<Vector3>();
        public Vector3 WeaponRecoilScale;
        public float WeaponRecoilStopSpeed;
        public double WeaponRecoilStopTime;
        public double WeaponRecoilRecoverTime;
    }
}