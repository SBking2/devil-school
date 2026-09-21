
namespace EGame
{
    public class GreateSwordModel : MeleeWeaponModel
    {
        public override string ParentName => "hand_r";
        public override WeaponType Type => WeaponType.Sword;

        protected override MeleeComboStep[] ComboSteps => new MeleeComboStep[]
        {
            new MeleeComboStep()
            {
                LockEndTime = 0.233f,
                ReleaseInputEndTime = 0.333f,
                ComboEndTime = 0.5f,
                Duration = 1.333f,
                HitboxOpenTime = 0.166f,
                HitboxCloseTime = 0.266f,
                Damage = 2,
            },
            new MeleeComboStep()
            {
                LockEndTime = 0.166f,
                ReleaseInputEndTime = 0.233f,
                ComboEndTime = 0.4f,
                Duration = 1.333f,
                HitboxOpenTime = 0.133f,
                HitboxCloseTime = 0.233f,
                Damage = 2,
            },
            new MeleeComboStep()
            {
                LockEndTime = 0.666f,
                ReleaseInputEndTime = 0.666f,
                ComboEndTime = 0.666f,
                Duration = 1.333f,
                HitboxOpenTime = 0.133f,
                HitboxCloseTime = 0.2f,
                Damage = 2,
            },
        };
    }
}