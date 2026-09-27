
namespace EGame
{
    public class PlayerMoveStateWalk : PlayerMoveStateGround
    {
        public override string StateName => PlayerConfig.MoveWalk;

        protected override float GetSpeed(NPlayer player) => player.WalkSpeed;
        protected override float GetBobRate(NPlayer player) => player.WalkBobRate;
    }
}
