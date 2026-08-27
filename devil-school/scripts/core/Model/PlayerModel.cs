
namespace EGame
{
    public class PlayerModel : CharacterModel
    {
        protected override CreatureAnimator BuildAnimator(INCharacter character)
        {
            var idle_state = new AnimState("ArmsRig|guard_idle", 0.1f, true);
            return new CreatureAnimator(idle_state);
        }
    }
}
