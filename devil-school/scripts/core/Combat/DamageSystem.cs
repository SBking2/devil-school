
namespace EGame
{
    public class DamageSystem
    {
        public static DamageSystem Instance { get; } = new DamageSystem();

        private Log.Logger _Logger = new Log.Logger(Log.LogType.Combat);

        public void ReportHit(DamageInfo info)
        {
            if (info.HitObject is INDamageable damageable)
            {
                damageable.TakeDamage(info);
                _Logger.Debug($"命中目标 {info.HitObject?.Name}");
            }   
            else
                _Logger.Debug($"命中了非可伤害目标 {info.HitObject?.Name}");
        }
    }
}
