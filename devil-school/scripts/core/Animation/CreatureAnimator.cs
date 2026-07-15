
namespace EGame
{
    public class CreatureAnimator
    {
        private readonly EGSpineSprite _SpineController;
        private AnimState _CurrentState;

        private readonly AnimState _AnyState;   //CallTrigger的时候优先查这个状态

        public CreatureAnimator(EGSpineSprite owner, AnimState init_state)
        {
            _SpineController = owner;
            _CurrentState = init_state;
            _AnyState = new AnimState("any");
        }

        public void CallTrigger(string trigger)
        {
            
        }

        public void PlayAnimation(AnimState state)
        {

        }

        public void SetNextAnimation(AnimState state)
        {

        }
    }
}