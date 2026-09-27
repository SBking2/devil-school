
using Godot;

namespace EGame
{
    // 冲刺：独占模式。进入时 Normal 已经退出，移动层和动作层都被停掉，所以冲刺期间不响应移动输入、不能开枪；
    // 时间到了切回 Normal，两层从 Idle 重新开始。退出时把速度直接钉到 PostDashSpeed，
    // 不依赖 ApplyAcceleration/摩擦去慢慢降——那两个是不是正在按方向键会导致衰减快慢不一样，
    // 这里要的是"冲刺一结束就摔一下"的确定手感，跟输入状态无关
    public class PlayerModeStateDash : PlayerState
    {
        public override string StateName => PlayerConfig.ModeDash;

        private const float DashSpeed = 18f;
        private const double DashDuration = 0.2;
        private const float PostDashSpeed = 8f;

        private Vector3 _Direction;

        public override void OnEnter(NPlayer player)
        {
            base.OnEnter(player);

            // 有方向输入就往输入方向冲，没有就往正前方冲
            var wish = player.WishDirection;
            _Direction = wish.LengthSquared() > 0.0001f ? wish.Normalized() : player.ForwardDirection;

            player.StartDashCooldown();
            player.ViewBobEnabled = false;
            player.GravityEnabled = false;
            player.Intent.Reset();
            ApplyDashVelocity(player);
        }

        public override void OnPhysicalProcess(NPlayer player, double dt)
        {
            base.OnPhysicalProcess(player, dt);
            ApplyDashVelocity(player);
        }

        public override void OnExit(NPlayer player)
        {
            base.OnExit(player);
            player.GravityEnabled = true;
            player.Velocity = new Vector3(_Direction.X * PostDashSpeed, player.Velocity.Y, _Direction.Z * PostDashSpeed);
        }

        public override void OnProcess(NPlayer player, double dt)
        {
            base.OnProcess(player, dt);

            if (_RunningTime > DashDuration)
                player.ModeLayer.ChangeState(PlayerConfig.ModeNormal);
        }

        private void ApplyDashVelocity(NPlayer player)
        {
            player.Velocity = new Vector3(_Direction.X * DashSpeed, 0f, _Direction.Z * DashSpeed);
        }
    }
}
