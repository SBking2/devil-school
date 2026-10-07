
namespace EGame
{
    public class DamageSystem
    {
        public static DamageSystem Instance { get; } = new DamageSystem();

        public void ReportHit(DamageInfo info)
        {
            if (info.Target is INDamageable damageable)
            {
                damageable.OnDamage(info);
                //Log.Debug($"命中目标 {info.Target?.Name}", type: Log.LogType.Combat);
            }

            if(info.Attacker is INAttackable attacker)
            {
                attacker.OnAttack(info);
            }
        }
    }
}
