
using Godot;

namespace EGame
{
    public partial class NWeapon : Node3D
    {
        private Camera3D _RealCamera;
        private Node3D _Muzzle;

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            if(@event is InputEventMouseButton btn && btn.IsActionPressed(EGInput.FIRE))
            {
                Fire();
            }
        }

        /// <summary>
        /// 做射线检测
        /// </summary>
        private void Fire()
        {
            var space_state = GetWorld3D().DirectSpaceState;
            var from = _RealCamera.GlobalPosition;
            var to = from + (-_RealCamera.GlobalTransform.Basis.Z) * 100f;

            var query = PhysicsRayQueryParameters3D.Create(from, to);
            query.CollisionMask = 1;

            var result = space_state.IntersectRay(query);
            if(result.Count > 0)
            {
                Node3D hitObject = (Node3D)result["collider"];
                GD.Print($"hit {hitObject.Name}");
            }
        }
    }
}