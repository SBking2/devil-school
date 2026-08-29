
namespace EGame
{
    public class PlayerModel : CharacterModel
    {
        public virtual float RunSpeed => 12f;
        public virtual float CrouchSpeed => 3f;

        protected override CreatureAnimator BuildAnimator(INCharacter character)
        {
            var idle_state = new AnimState("ArmsRig|guard_idle", 0.1f, true);

            var hand_switch_state = new AnimState("ArmsRig|guard_draw", 0.0f, false);
            hand_switch_state.NextState = idle_state;

            var ans = new CreatureAnimator(idle_state);
            ans.AddAnyBranch("hand_switch", hand_switch_state);
            return ans;
        }
    }
}
