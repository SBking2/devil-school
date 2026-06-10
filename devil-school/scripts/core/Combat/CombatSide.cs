
using System;

namespace EGame
{
    public enum CombatSide
    {
        None,
        Player,
        Partner,
        Enemy
    }

    public static class CombatSideExtension
    {
        public static CombatSide GetOppositeSide(this CombatSide side)
        {
            return side switch
            {
                CombatSide.None => CombatSide.None,
                CombatSide.Player => CombatSide.Enemy,
                CombatSide.Enemy => CombatSide.Player,
                CombatSide.Partner => CombatSide.Partner,
                _ => throw new ArgumentOutOfRangeException("side", side, null),
            };
        }
    }

}