
namespace EGame
{
    public class DamageSystem
    {
        public static DamageSystem Instance { get; } = new DamageSystem();

        public void ReportHit(DamageInfo info)
        {
            if (info.HitObject is INDamageable damageable)
            {
                damageable.TakeDamage(info);
                Log.Debug($"命中目标 {info.HitObject?.Name}", type: Log.LogType.Combat);
            }
            else
                Log.Debug($"命中了非可伤害目标 {info.HitObject?.Name}", type: Log.LogType.Combat);
        }
    }
}
