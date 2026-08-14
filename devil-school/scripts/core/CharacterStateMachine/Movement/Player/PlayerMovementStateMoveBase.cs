
namespace EGame
{
    public abstract class PlayerMovementStateMoveBase : AbstractCharacterMovementState
    {
        public override void OnPhysicalUpdate(CharacterMovementContext context, double delta)
        {
            var move = context.Owner.Intent.MoveDir.Normalized() * GetMoveSpeed(context);

            var velocity = context.Owner.Velocity;
            velocity.X = move.X;
            velocity.Z = move.Z;
            context.Owner.Velocity = velocity;

            context.Owner.MoveAndSlide();
        }

        protected virtual float GetMoveSpeed(CharacterMovementContext context)
        {
            float speed = context.Owner.Data.CharacterModel.MoveSpeed;
            return ApplyCrouchMultiplier(context, speed);
        }

        // 蹲伏不再是一个独立状态，而是叠加在任何移动状态上的速度修饰，子类算完自己的速度后都应该经过这一步
        protected float ApplyCrouchMultiplier(CharacterMovementContext context, float speed)
        {
            if (context.Owner.IsCrouching)
                speed *= NEnvCreature.CROUCH_SPEED_MULTIPLIER;

            return speed;
        }
    }
}
