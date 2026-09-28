
using Godot;

namespace EGame
{
    // 自由环游：独占模式，关掉重力和碰撞，直接改 GlobalPosition 飞行，不走 MoveAndSlide。
    // 由 DevConsole 的 fly 命令随时切换进出，不是玩法的一部分
    public class PlayerModeStateFly : PlayerState
    {
        public override string StateName => PlayerConfig.ModeFly;

        private const float _FlySpeed = 8f;
        private const float _FlySpeedFast = 24f;    // 按住 Shift（RUN 键）加速

        public override void OnEnter(NPlayer player)
        {
            base.OnEnter(player);
            player.GravityEnabled = false;
            player.CollisionEnabled = false;
            player.Velocity = Vector3.Zero;
        }

        public override void OnExit(NPlayer player)
        {
            base.OnExit(player);
            player.GravityEnabled = true;
            player.CollisionEnabled = true;
        }

        public override void OnPhysicalProcess(NPlayer player, double dt)
        {
            base.OnPhysicalProcess(player, dt);

            Vector3 direction = player.FlyDirection;

            // 上升/下降单独用跳跃/蹲键控制，跟摄像机俯仰叠加
            if (Input.IsActionPressed(EGInput.JUMP))
                direction += Vector3.Up;
            if (Input.IsActionPressed(EGInput.CROUCH))
                direction += Vector3.Down;

            if (direction.LengthSquared() < 0.0001f)
                return;

            float speed = Input.IsActionPressed(EGInput.RUN) ? _FlySpeedFast : _FlySpeed;
            player.GlobalPosition += direction.Normalized() * speed * (float)dt;
        }
    }
}
