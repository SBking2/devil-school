
namespace EGame
{
    public abstract class AbstractTurnMoveState
    {
        public abstract string ID { get; }

        //是否是行为节点
        public virtual bool IsMove => this is TurnStateMove;

        public abstract string GetNextState(Creature owner, Rng rng);

        public virtual void OnEnter()
        {

        }

        public virtual void OnExit()
        {

        }
    }
}
