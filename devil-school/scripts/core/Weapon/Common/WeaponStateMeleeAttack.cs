
namespace EGame
{
    // 近战攻击：阶段轨道 lock -> release_input -> combo -> duration(结束) 决定连击怎么走；
    // 碰撞体开关是另一套独立轨道；两条轨道现在都是从这一段攻击开始算起的绝对时间点，共用同一个 _StepTime
    public class WeaponStateMeleeAttack : WeaponState
    {
        public override string StateName => WeaponConfig.MeleeAttack;

        private enum Phase { Lock, ReleaseInput, Combo, Exit }

        private Phase _Phase;
        private double _StepTime;
        private int _ComboIndex;
        private bool _BufferedNextAttack;
        private bool _HitboxOpen;

        public override void OnEnter(NWeapon weapon)
        {
            base.OnEnter(weapon);
            _ComboIndex = 0;
            EnterStep(weapon);
        }

        public override void OnProcess(NWeapon weapon, double dt)
        {
            base.OnProcess(weapon, dt);
            _StepTime += dt;

            var model = weapon.MeleeData;
            switch (_Phase)
            {
                case Phase.Lock:
                    if (_StepTime > model.GetLockEndTime(_ComboIndex))
                        _Phase = Phase.ReleaseInput;
                    break;

                case Phase.ReleaseInput:
                    if (weapon.Intent.JustPressed)
                        _BufferedNextAttack = true;
                    if (_StepTime > model.GetReleaseInputEndTime(_ComboIndex))
                        _Phase = Phase.Combo;
                    break;

                case Phase.Combo:
                    bool wants_next = _BufferedNextAttack || weapon.Intent.JustPressed;
                    if (wants_next)
                    {
                        // 打到最后一段还按了攻击键，就绕回第一段重新开始，不是卡住不动
                        _ComboIndex = (_ComboIndex + 1) % model.ComboCount;
                        EnterStep(weapon);
                    }
                    else if (_StepTime > model.GetComboEndTime(_ComboIndex))
                    {
                        _Phase = Phase.Exit;
                    }
                    break;
                    
                case Phase.Exit:
                    if (weapon.Intent.JustPressed)
                    {
                        // exit 阶段这一下攻击本身还没结束，能打，但连击链已经断了，只能重新从第一段开始，不算续上
                        _ComboIndex = 0;
                        EnterStep(weapon);
                    }
                    else if (_StepTime > model.GetDuration(_ComboIndex))
                    {
                        weapon.ChangeState(WeaponConfig.Idle);
                    }
                    break;
            }

            UpdateHitbox(weapon, model);
        }

        private void UpdateHitbox(NWeapon weapon, MeleeWeaponModel model)
        {
            bool should_open = _StepTime >= model.GetHitboxOpenTime(_ComboIndex) && _StepTime < model.GetHitboxCloseTime(_ComboIndex);
            SetHitboxOpen(weapon, should_open);
        }

        private void SetHitboxOpen(NWeapon weapon, bool open)
        {
            if (open == _HitboxOpen)
                return;

            _HitboxOpen = open;
            if (weapon.AttackCollision != null)
                weapon.AttackCollision.Monitoring = open;
        }

        // 每次真正开始一段新的连击（包括从头开始或者连到下一段）都要重置这一段的绝对时间线
        private void EnterStep(NWeapon weapon)
        {
            _Phase = Phase.Lock;
            _StepTime = 0;
            _BufferedNextAttack = false;
            weapon.CurrentComboIndex = _ComboIndex;
            SetHitboxOpen(weapon, false);
            weapon.TriggerAnim(WeaponConfig.GetAttackAnimTrigger(weapon.MeleeData.Type, _ComboIndex));
            Log.VeryDebug($"Attack Anim : {WeaponConfig.GetAttackAnimTrigger(weapon.MeleeData.Type, _ComboIndex)}!");
        }
    }
}
