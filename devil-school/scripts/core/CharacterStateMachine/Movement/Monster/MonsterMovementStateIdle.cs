namespace EGame
{
    public class MonsterMovementStateIdle : MonsterMovementStateMoveBase
    {
        public override string StateName => "idle";

        public override void OnEnter(CharacterMovementContext context)
        {
            context.Owner.SetAnimTrigger("idle");
        }

        public override void OnUpdate(CharacterMovementContext context, double delta)
        {
            base.OnUpdate(context, delta);

            if (context.Owner.Intent.MoveDir.Length() > 0.1f)
                context.StateMachine.ChangeState("walk");
        }
    }
}
