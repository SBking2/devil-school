
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
                LockEndTime = 0.133f,
                ReleaseInputEndTime = 0.433f,
                ComboEndTime = 0.666f,
                Duration = 1.332f,
                HitboxOpenTime = 0.099f,
                HitboxCloseTime = 0.198f,
                Damage = 2,
            },
            new MeleeComboStep()
            {
                LockEndTime = 0.149f,
                ReleaseInputEndTime = 0.3f,
                ComboEndTime = 0.599f,
                Duration = 1.332f,
                HitboxOpenTime = 0.033f,
                HitboxCloseTime = 0.133f,
                Damage = 2,
            },
            new MeleeComboStep()
            {
                LockEndTime = 0.699f,
                ReleaseInputEndTime = 0.699f,
                ComboEndTime = 0.699f,
                Duration = 1.332f,
                HitboxOpenTime = 0.266f,
                HitboxCloseTime = 0.323f,
                Damage = 2,
            },
        };
    }
}