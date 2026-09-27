
namespace EGame
{
    public class PlayerMoveStateCrouch : PlayerMoveStateGround
    {
        public override string StateName => PlayerConfig.MoveCrouch;

        protected override float GetSpeed(NPlayer player) => player.CrouchSpeed;
        protected override float GetBobRate(NPlayer player) => player.CrouchBobRate;
    }
}
