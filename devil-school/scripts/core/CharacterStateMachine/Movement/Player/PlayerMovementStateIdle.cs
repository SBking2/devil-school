namespace EGame
{
    public class PlayerMovementStateIdle : PlayerMovementStateMoveBase
    {
        public override string StateName => "idle";

        public override void OnEnter(CharacterMovementContext context)
        {
            context.Owner.SetAnimTrigger("idle");
        }

        public override void OnUpdate(CharacterMovementContext context, double delta)
        {
            base.OnUpdate(context, delta);

            if (context.Owner.IsGround == false)
            {
                context.StateMachine.ChangeState("fall");
                return;
            }

            if (context.Owner.Intent.WantsJump)
            {
                context.StateMachine.ChangeState("jump");
                return;
            }

            if (context.Owner.Intent.MoveDir.Length() > 0.1f)
            {
                bool wants_run = context.Owner.Intent.WantsRun && context.Owner.IsCrouching == false;
                context.StateMachine.ChangeState(wants_run ? "run" : "walk");
            }
        }
    }
}
