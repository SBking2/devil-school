
using Godot;
using System;
using System.Collections.Generic;

namespace EGame
{
    [GlobalClass]
    public partial class NMeleeCast : ShapeCast3D
    {
        /// <summary>
        /// 对象、碰撞点、法向
        /// </summary>
        public event Action<Node3D, Vector3, Vector3> OnHit;

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                                    扫掠检测
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        private Vector3 _LastGlobalPosition;
        private bool _IsDetecting = false;
        private HashSet<ulong> _HitObj = new HashSet<ulong>();

        public override void _Ready()
        {
            base._Ready();
            Enabled = false;
        }

        public void Start()
        {
            _IsDetecting = true;
            _LastGlobalPosition = GlobalPosition;
            GetShapeEndCenters(out _LastTop, out _LastBottom);
            _HitObj.Clear();
        }

        public void Stop()
        {
            _IsDetecting = false;
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);

            if(_IsDetecting)
            {
                //从当前位置指回上一帧位置（局部坐标），扫的就是这一帧刀刃走过的路径
                TargetPosition = ToLocal(_LastGlobalPosition);

                GetShapeEndCenters(out Vector3 top, out Vector3 bottom);
                DrawSweepQuad(top, bottom);

                //自动检测关了，只能手动检测
                ForceShapecastUpdate();

                if(IsColliding())
                {
                    for(int i = 0; i < GetCollisionCount(); i++)
                    {
                        if(GetCollider(i) is not Node3D target)
                            continue;

                        if(_HitObj.Add(target.GetInstanceId()) == false)
                            continue;

                        OnHit?.Invoke(target, GetCollisionPoint(i), GetCollisionNormal(i));
                    }
                }

                _LastGlobalPosition = this.GlobalPosition;
                _LastTop = top;
                _LastBottom = bottom;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                                    轨迹可视化
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        private Vector3 _LastTop;
        private Vector3 _LastBottom;

        private void GetShapeEndCenters(out Vector3 top, out Vector3 bottom)
        {
            if(Shape is CapsuleShape3D capsule)
            {
                //胶囊局部 Y 轴是轴向，Height 是整体高度，球心在两端各缩进一个半径
                float half = Mathf.Max(capsule.Height * 0.5f - capsule.Radius, 0f);
                top = GlobalTransform * new Vector3(0f, half, 0f);
                bottom = GlobalTransform * new Vector3(0f, -half, 0f);
                return;
            }

            top = GlobalPosition;
            bottom = GlobalPosition;
        }

        private void DrawSweepQuad(Vector3 top, Vector3 bottom)
        {
            CollisionDebugDraw.Quad(_LastTop, top, bottom, _LastBottom);
        }
    }
}
