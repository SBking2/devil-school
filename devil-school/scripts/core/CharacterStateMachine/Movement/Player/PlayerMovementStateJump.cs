
namespace EGame
{
    public class PlayerMovementStateJump : PlayerMovementStateAirMoveBase
    {
        public override string StateName => "jump";

        // 起跳只给一次向上的冲量，本身只存在一个tick
        public override void OnEnter(CharacterMovementContext context)
        {
            context.Owner.SetAnimTrigger("jump");

            var player_model = context.Owner.Data.CharacterModel as PlayerModel;
            var velocity = context.Owner.Velocity;
            velocity.Y = player_model.JumpSpeed;
            context.Owner.Velocity = velocity;
        }

        public override void OnUpdate(CharacterMovementContext context, double delta)
        {
            base.OnUpdate(context, delta);

            if (TryLandOnGround(context))
                return;

            // 只有过了最高点、开始往下掉才切到fall，起跳上升阶段一直留在jump里
            if (context.Owner.Velocity.Y <= 0f)
                context.StateMachine.ChangeState("fall");
        }
    }
}
