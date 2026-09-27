
namespace EGame
{
    public class PlayerMoveStateRun : PlayerMoveStateGround
    {
        public override string StateName => PlayerConfig.MoveRun;

        protected override float GetSpeed(NPlayer player) => player.RunSpeed;
        protected override float GetBobRate(NPlayer player) => player.RunBobRate;
    }
}
