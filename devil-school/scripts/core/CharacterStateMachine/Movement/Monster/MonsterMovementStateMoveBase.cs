
namespace EGame
{
    public abstract class MonsterMovementStateMoveBase : AbstractCharacterMovementState
    {
        public override void OnUpdate(CharacterMovementContext context, double delta)
        {
            if (context.Owner.TargetMoveDir != Godot.Vector3.Zero)
            {
                var basis = Godot.Basis.LookingAt(-context.Owner.TargetMoveDir, Godot.Vector3.Up);
                context.Owner.Quaternion = context.Owner.Quaternion.Slerp(basis.GetRotationQuaternion(), (float)delta * 10.0f);
            }
        }

        public override void OnPhysicalUpdate(CharacterMovementContext context, double delta)
        {
            var move = context.Owner.TargetMoveDir;
            context.Owner.Velocity = move.Normalized() * context.Owner.Data.CharacterModel.MoveSpeed;
            context.Owner.MoveAndSlide();
        }
    }
}
