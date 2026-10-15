
using Godot;

namespace EGame
{
    // 近战攻击：阶段轨道 lock -> release_input -> combo -> duration(结束) 决定连击怎么走；
    // 碰撞体开关是另一套独立轨道；两条轨道都是从这一段攻击开始算起的绝对时间点，共用同一个 _StepTime
    public class PlayerActionStateMelee : PlayerState
    {
        public override string StateName => PlayerConfig.ActionMelee;

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                                    连击推进
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        private enum Phase { Lock, ReleaseInput, Combo, Exit }

        // 进入时记下这把武器，退出时要关的是它的碰撞体（切枪时当前武器已经变了）
        private NWeapon _Weapon;
        private Phase _Phase;
        private double _StepTime;
        private int _ComboIndex;
        private bool _BufferedNextAttack;

        public override void OnEnter(NPlayer player)
        {
            base.OnEnter(player);
            _Weapon = player.CurrentWeapon;
            _ComboIndex = 0;
            EnterStep();
        }

        public override void OnProcess(NPlayer player, double dt)
        {
            base.OnProcess(player, dt);
            _StepTime += dt;

            var model = _Weapon.MeleeData;
            switch (_Phase)
            {
                case Phase.Lock:
                    if (_StepTime > model.GetLockEndTime(_ComboIndex))
                        _Phase = Phase.ReleaseInput;
                    break;

                case Phase.ReleaseInput:
                    if (player.Intent.JustPressed)
                        _BufferedNextAttack = true;
                    if (_StepTime > model.GetReleaseInputEndTime(_ComboIndex))
                        _Phase = Phase.Combo;
                    break;

                case Phase.Combo:
                    bool wants_next = _BufferedNextAttack || player.Intent.JustPressed;
                    if (wants_next)
                    {
                        // 打到最后一段还按了攻击键，就绕回第一段重新开始，不是卡住不动
                        _ComboIndex = (_ComboIndex + 1) % model.ComboCount;
                        EnterStep();
                    }
                    else if (_StepTime > model.GetComboEndTime(_ComboIndex))
                    {
                        _Phase = Phase.Exit;
                    }
                    break;

                case Phase.Exit:
                    if (player.Intent.JustPressed)
                    {
                        // exit 阶段这一下攻击本身还没结束，能打，但连击链已经断了，只能重新从第一段开始，不算续上
                        _ComboIndex = 0;
                        EnterStep();
                    }
                    else if (_StepTime > model.GetDuration(_ComboIndex))
                    {
                        player.ActionLayer.ChangeState(PlayerConfig.ActionIdle);
                    }
                    break;
            }

            UpdateHitbox(model);
            UpdateCameraRotation(player);
        }

        public override void OnExit(NPlayer player)
        {
            base.OnExit(player);
            SetHitboxOpen(false);
            ResetCameraRotation(player);
        }

        // 每次真正开始一段新的连击（包括从头开始或者连到下一段）都要重置这一段的绝对时间线
        private void EnterStep()
        {
            _Phase = Phase.Lock;
            _StepTime = 0;
            _BufferedNextAttack = false;
            _Weapon.CurrentComboIndex = _ComboIndex;
            SetHitboxOpen(false);
            StartCameraRotation();
            _Weapon.Player.AnimTrigger(_Weapon.MeleeData.GetAttackTrigger(_ComboIndex));
            SoundManager.Instance.Play("sword_attack1");
            Log.VeryDebug($"Attack Anim : {_Weapon.MeleeData.GetAttackTrigger(_ComboIndex)}!");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                                    镜头旋转
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        private RotationCurve _RotationCurve;

        private void StartCameraRotation()
        {
            var curves = _Weapon.AttackCameraRotationCurves;
            _RotationCurve = curves != null && _ComboIndex < curves.Length ? curves[_ComboIndex] : null;
        }

        // 曲线横轴就是这一招开始后的秒数，直接用 _StepTime 采样，和命中窗口、卡肉共用同一条时间线
        private void UpdateCameraRotation(NPlayer player)
        {
            if (player.CameraAttackEffect == null)
                return;

            if (_RotationCurve == null || _StepTime >= _RotationCurve.Duration)
            {
                player.CameraAttackEffect.Quaternion = Quaternion.Identity;
                return;
            }

            player.CameraAttackEffect.Quaternion = _RotationCurve.Sample((float)_StepTime);
        }

        private void ResetCameraRotation(NPlayer player)
        {
            _RotationCurve = null;
            if (player.CameraAttackEffect != null)
                player.CameraAttackEffect.Quaternion = Quaternion.Identity;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////                                    命中判定窗口
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        private bool _HitboxOpen;

        private void UpdateHitbox(MeleeWeaponModel model)
        {
            bool should_open = _StepTime >= model.GetHitboxOpenTime(_ComboIndex) && _StepTime < model.GetHitboxCloseTime(_ComboIndex);
            SetHitboxOpen(should_open);
        }

        private void SetHitboxOpen(bool open)
        {
            if (open == _HitboxOpen)
                return;

            _HitboxOpen = open;
            if (_Weapon.MeleeCast == null)
                return;

            if (open)
                _Weapon.MeleeCast.Start();
            else
                _Weapon.MeleeCast.Stop();
        }
    }
}
