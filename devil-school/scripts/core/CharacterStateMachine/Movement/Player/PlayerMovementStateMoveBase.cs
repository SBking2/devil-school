
namespace EGame
{
    public abstract class PlayerMovementStateMoveBase : AbstractCharacterMovementState
    {
        public override void OnPhysicalUpdate(CharacterMovementContext context, double delta)
        {
            var move = context.Owner.TargetMoveDir;
            context.Owner.Velocity = move.Normalized() * context.Owner.Data.CharacterModel.MoveSpeed;
            context.Owner.MoveAndSlide();
        }
    }
}
