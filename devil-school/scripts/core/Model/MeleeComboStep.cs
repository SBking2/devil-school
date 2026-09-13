
namespace EGame
{
    // 一段连击(一招)的完整配置。时间全部是从这一招开始算起的绝对时间点，不是各阶段自己的时长
    public class MeleeComboStep
    {
        public float LockEndTime;
        public float ReleaseInputEndTime;
        public float ComboEndTime;
        public float Duration;    // 这一招整体多长，一过这个点就回到 Idle
        public float HitboxOpenTime;
        public float HitboxCloseTime;
        public int Damage;
    }
}
