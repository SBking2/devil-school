
namespace EGame
{
    public class PlayerMoveStateAir : PlayerState
    {
        public override string StateName => PlayerConfig.MoveAir;

        public override void OnEnter(NPlayer player)
        {
            base.OnEnter(player);
            player.ViewBobEnabled = false;
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

            // 起跳那一帧 IsOnFloor 还是 true，但 Y 速度是向上的，所以要带上速度方向判断，不然会被当成落地
            if (player.IsOnFloor() && player.Velocity.Y <= 0f)
            {
                layer.ChangeState(player.ResolveGroundMoveState());
                return;
            }

            player.MoveInAir(dt);
        }
    }
}
