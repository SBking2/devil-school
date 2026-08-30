
namespace EGame
{
    public class PlayerModel : CharacterModel
    {
        public virtual float RunSpeed => 12f;
        public virtual float CrouchSpeed => 3f;

        protected override CreatureAnimator BuildAnimator(INCharacter character)
        {
            var hand_idle = new AnimState("ArmsRig|guard_idle", 0.1f, true);
            var hand_switch_state = new AnimState("ArmsRig|guard_draw", 0.0f, false);
            var hand_fire = new AnimState("ArmsRig|jab_R", 0.0f, false);
            hand_switch_state.NextState = hand_idle;
            hand_fire.NextState = hand_idle;

            var pistol_switch = new AnimState("ArmsRig|finger_gun_fix", 0.0f, false);
            var pistol_idle = new AnimState("ArmsRig|finger_gun_idle", 0.1f, true);
            var pistol_fire = new AnimState("ArmsRig|finger_gun_fire", 0.0f, false);
            pistol_fire.NextState = pistol_idle;
            pistol_switch.NextState = pistol_idle;

            var ans = new CreatureAnimator(hand_idle);
            ans.AddAnyBranch("hand_switch", hand_switch_state);
            ans.AddAnyBranch("pistol_switch", pistol_switch);
            ans.AddAnyBranch("pistol_fire", pistol_fire);
            ans.AddAnyBranch("hand_fire", hand_fire);
            return ans;
        }
    }
}
