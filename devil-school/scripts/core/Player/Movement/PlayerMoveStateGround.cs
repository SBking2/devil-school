
namespace EGame
{
    // 地面移动状态的公共部分：起跳/离地转 Air，其余按输入在 Idle/Walk/Run/Crouch 之间切。
    // 各子状态只决定"速度多少"和"视角 Bob 多快"
    public abstract class PlayerMoveStateGround : PlayerState
    {
        protected abstract float GetSpeed(NPlayer player);
        protected abstract float GetBobRate(NPlayer player);

        public override void OnEnter(NPlayer player)
        {
            base.OnEnter(player);
            player.ViewBobEnabled = true;
            player.ViewBobRate = GetBobRate(player);
        }

        public override void OnPhysicalProcess(NPlayer player, double dt)
        {
            base.OnPhysicalProcess(player, dt);
            var layer = player.MovementLayer;

            if (player.DashPressed && player.CanDash)
            {
                player.ModeLayer.ChangeState(PlayerConfig.ModeDash);
                return;
            }

            bool jumped = player.IsOnFloor() && player.JumpPressed;
            if (jumped)
                player.Jump();

            if (jumped || !player.IsOnFloor())
            {
                layer.ChangeState(PlayerConfig.MoveAir);
                return;
            }

            player.MoveOnGround(dt, GetSpeed(player));

            string next = player.ResolveGroundMoveState();
            if (next != StateName)
                layer.ChangeState(next);
        }
    }
}
