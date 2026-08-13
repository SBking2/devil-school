
namespace EGame
{
    public abstract class PlayerMovementStateMoveBase : AbstractCharacterMovementState
    {
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
