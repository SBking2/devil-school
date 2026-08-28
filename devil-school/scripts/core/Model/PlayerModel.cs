
namespace EGame
{
    public class PlayerModel : CharacterModel
    {
        public virtual float RunSpeed => 12f;
        public virtual float CrouchSpeed => 3f;

        protected override CreatureAnimator BuildAnimator(INCharacter character)
        {
            var idle_state = new AnimState("ArmsRig|guard_idle", 0.1f, true);
            return new CreatureAnimator(idle_state);
        }
    }
}
