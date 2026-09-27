
namespace EGame
{
    // 正常模式：移动层和动作层只在这个模式里有当前状态（两层每帧都由 NPlayer 驱动，没有当前状态就什么都不跑）。
    // 进入时两层都从 Idle 开始，退出时两层停掉
    public class PlayerModeStateNormal : PlayerState
    {
        public override string StateName => PlayerConfig.ModeNormal;

        public override void OnEnter(NPlayer player)
        {
            base.OnEnter(player);
            player.MovementLayer.ChangeState(PlayerConfig.MoveIdle);
            player.ActionLayer.ChangeState(PlayerConfig.ActionIdle);
        }

        public override void OnExit(NPlayer player)
        {
            base.OnExit(player);
            player.MovementLayer.Stop();
            player.ActionLayer.Stop();
        }
    }
}
