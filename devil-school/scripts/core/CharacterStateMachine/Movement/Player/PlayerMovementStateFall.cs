
namespace EGame
{
    public class PlayerMovementStateFall : PlayerMovementStateAirMoveBase
    {
        public override string StateName => "fall";

        public override void OnEnter(CharacterMovementContext context)
        {
            context.Owner.SetAnimTrigger("fall");
        }

        public override void OnUpdate(CharacterMovementContext context, double delta)
        {
            base.OnUpdate(context, delta);
            TryLandOnGround(context);
        }
    }
}
