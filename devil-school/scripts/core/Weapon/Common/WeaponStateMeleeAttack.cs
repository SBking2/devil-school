
namespace EGame
{
    // 近战攻击：阶段轨道 lock -> 前摇(windup) -> release_input -> combo -> exit 决定连击怎么走；
    // 碰撞体开关是另一套独立轨道，从每段攻击的 lock 开始单独计时，不受阶段切换影响
    public class WeaponStateMeleeAttack : WeaponState
    {
        public override string StateName => WeaponConfig.MeleeAttack;

        private enum Phase { Lock, Windup, ReleaseInput, Combo, Exit }

        private Phase _Phase;
        private double _PhaseTime;
        private double _StepTime;
        private int _ComboIndex;
        private bool _BufferedNextAttack;
        private bool _HitboxOpen;

        public override void OnEnter(NWeapon weapon)
        {
            base.OnEnter(weapon);
            _ComboIndex = 0;
            EnterPhase(weapon, Phase.Lock);
        }

        public override void OnProcess(NWeapon weapon, double dt)
        {
            base.OnProcess(weapon, dt);
            _PhaseTime += dt;
            _StepTime += dt;

            var model = weapon.MeleeData;
            switch (_Phase)
            {
                case Phase.Lock:
                    if (_PhaseTime > model.GetLockTime(_ComboIndex))
                        EnterPhase(weapon, Phase.Windup);
                    break;

                case Phase.Windup:
                    if (_PhaseTime > model.GetWindupTime(_ComboIndex))
                        EnterPhase(weapon, Phase.ReleaseInput);
                    break;

                case Phase.ReleaseInput:
                    if (weapon.Intent.JustPressed)
                        _BufferedNextAttack = true;
                    if (_PhaseTime > model.GetReleaseInputTime(_ComboIndex))
                        EnterPhase(weapon, Phase.Combo);
                    break;

                case Phase.Combo:
                    bool wants_next = _BufferedNextAttack || weapon.Intent.JustPressed;
                    if (wants_next && _ComboIndex < model.ComboCount - 1)
                    {
                        _ComboIndex++;
                        EnterPhase(weapon, Phase.Lock);
                    }
                    else if (_PhaseTime > model.GetComboTime(_ComboIndex))
                    {
                        EnterPhase(weapon, Phase.Exit);
                    }
                    break;

                case Phase.Exit:
                    if (_PhaseTime > model.GetExitTime(_ComboIndex))
                        weapon.ChangeState(WeaponConfig.Idle);
                    break;
            }

            UpdateHitbox(model);
        }

        private void UpdateHitbox(MeleeModel model)
        {
            bool should_open = _StepTime >= model.GetHitboxOpenTime(_ComboIndex) && _StepTime < model.GetHitboxCloseTime(_ComboIndex);
            if (should_open == _HitboxOpen)
                return;

            _HitboxOpen = should_open;
            if (_HitboxOpen)
            {
                // TODO: 开启碰撞体
            }
            else
            {
                // TODO: 关闭碰撞体
            }
        }

        private void EnterPhase(NWeapon weapon, Phase phase)
        {
            _Phase = phase;
            _PhaseTime = 0;

            if (phase == Phase.Lock)
            {
                _StepTime = 0;
                _BufferedNextAttack = false;
                if (_HitboxOpen)
                {
                    _HitboxOpen = false;
                    // TODO: 关闭碰撞体
                }
                weapon.TriggerAnim(weapon.MeleeData.GetAnimTrigger(_ComboIndex));
            }
        }
    }
}
