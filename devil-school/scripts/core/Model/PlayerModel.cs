
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

            var pistol_switch = new AnimState("ArmsRig|pistol_reload_bake", 0.0f, false);
            var pistol_idle = new AnimState("ArmsRig|pistol_idle_bake", 0.1f, true);
            var pistol_fire = new AnimState("ArmsRig|pistol_shot_bake", 0.0f, false);
            pistol_fire.NextState = pistol_idle;
            pistol_switch.NextState = pistol_idle;

            var sword_idle = new AnimState("ArmsRig|Sword_Idle_Bake", 0.1f, true);
            var sword_attack1 = new AnimState("ArmsRig|Sword_Attack1_Bake", 0.0f, false);
            var sword_attack2 = new AnimState("ArmsRig|Sword_Attack2_Bake", 0.0f, false);
            var sword_attack3 = new AnimState("ArmsRig|Sword_Attack3_Bake", 0.0f, false);
            sword_attack1.NextState = sword_idle;
            sword_attack2.NextState = sword_idle;
            sword_attack3.NextState = sword_idle;

            var ans = new CreatureAnimator(hand_idle);

            // 每种武器类型对应的 trigger 名字统一从 WeaponConfig 查，跟武器 Model 那边用的是同一份，不会对不上
            ans.AddAnyBranch(WeaponConfig.GetSwitchAnimTrigger(WeaponModel.WeaponType.Hand), hand_switch_state);
            ans.AddAnyBranch(WeaponConfig.GetSwitchAnimTrigger(WeaponModel.WeaponType.Pistol), pistol_switch);
            ans.AddAnyBranch(WeaponConfig.GetSwitchAnimTrigger(WeaponModel.WeaponType.Sword), sword_idle);

            ans.AddAnyBranch(WeaponConfig.GetAttackAnimTrigger(WeaponModel.WeaponType.Hand, 0), hand_fire);
            ans.AddAnyBranch(WeaponConfig.GetAttackAnimTrigger(WeaponModel.WeaponType.Pistol, 0), pistol_fire);
            ans.AddAnyBranch(WeaponConfig.GetAttackAnimTrigger(WeaponModel.WeaponType.Sword, 0), sword_attack1);
            ans.AddAnyBranch(WeaponConfig.GetAttackAnimTrigger(WeaponModel.WeaponType.Sword, 1), sword_attack2);
            ans.AddAnyBranch(WeaponConfig.GetAttackAnimTrigger(WeaponModel.WeaponType.Sword, 2), sword_attack3);
            return ans;
        }
    }
}
