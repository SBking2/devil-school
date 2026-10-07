
using Godot;

namespace EGame
{
    public class KnightBehaviourAttack : MonsterBehaviourNodeAttack
    {
        protected override Vector3 GetAttackPos(NAgent agent)
        {
            return base.GetAttackPos(agent) + -agent.GlobalBasis.Z * 0.6f + new Vector3(0.0f, 0.85f, 0f);
        }

        protected override float GetTurnStartTime()
        {
            return 0.1f;
        }

        protected override float GetTurnEndTime()
        {
            return 0.5f;
        }

        protected override float GetRange()
        {
            return 1f;
        }
    }
}