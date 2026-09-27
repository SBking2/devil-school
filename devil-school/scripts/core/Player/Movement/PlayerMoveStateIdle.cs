
namespace EGame
{
    public class PlayerMoveStateIdle : PlayerMoveStateGround
    {
        public override string StateName => PlayerConfig.MoveIdle;

        protected override float GetSpeed(NPlayer player) => player.WalkSpeed;
        protected override float GetBobRate(NPlayer player) => player.WalkBobRate;
    }
}
