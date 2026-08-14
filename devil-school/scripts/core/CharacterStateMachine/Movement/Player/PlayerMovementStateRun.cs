namespace EGame
{
    public class PlayerMovementStateRun : PlayerMovementStateMoveBase
    {
        public override string StateName => "run";

        public override void OnEnter(CharacterMovementContext context)
        {
            context.Owner.SetAnimTrigger("run");
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

            if (context.Owner.Intent.MoveDir.Length() <= 0.1f)
            {
                context.StateMachine.ChangeState("idle");
                return;
            }

            // 蹲下会取消疾跑，回退到走路（钻蹲这件事本身由 NEnvCreature 统一处理，跟这里在哪个状态无关）
            if (context.Owner.IsCrouching || context.Owner.Intent.WantsRun == false)
                context.StateMachine.ChangeState("walk");
        }

        protected override float GetMoveSpeed(CharacterMovementContext context)
        {
            float speed = context.Owner.Data.CharacterModel.RunSpeed;
            return ApplyCrouchMultiplier(context, speed);
        }
    }
}
