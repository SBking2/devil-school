
namespace EGame
{
    public abstract class MonsterMovementStateMoveBase : AbstractCharacterMovementState
    {
        public override void OnUpdate(CharacterMovementContext context, double delta)
        {
            if (context.Owner.Intent.MoveDir != Godot.Vector3.Zero)
            {
                var basis = Godot.Basis.LookingAt(-context.Owner.Intent.MoveDir, Godot.Vector3.Up);
                context.Owner.Quaternion = context.Owner.Quaternion.Slerp(basis.GetRotationQuaternion(), (float)delta * 10.0f);
            }
        }

        public override void OnPhysicalUpdate(CharacterMovementContext context, double delta)
        {
            var move = context.Owner.Intent.MoveDir.Normalized() * context.Owner.Data.CharacterModel.MoveSpeed;

            var velocity = context.Owner.Velocity;
            velocity.X = move.X;
            velocity.Z = move.Z;
            context.Owner.Velocity = velocity;

            context.Owner.MoveAndSlide();
        }
    }
}
