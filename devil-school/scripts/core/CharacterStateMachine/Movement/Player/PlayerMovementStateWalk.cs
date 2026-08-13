namespace EGame
{
    public class PlayerMovementStateWalk : PlayerMovementStateMoveBase
    {
        public override string StateName => "walk";

        public override void OnEnter(CharacterMovementContext context)
        {
            context.Owner.SetAnimTrigger("walk");
        }

        public override void OnUpdate(CharacterMovementContext context, double delta)
        {
            base.OnUpdate(context, delta);

            if (context.Owner.Intent.WantsCrouch)
            {
                context.StateMachine.ChangeState("crouch");
                return;
            }

            if (context.Owner.Intent.MoveDir.Length() <= 0.1f)
                context.StateMachine.ChangeState("idle");
        }
    }
}
