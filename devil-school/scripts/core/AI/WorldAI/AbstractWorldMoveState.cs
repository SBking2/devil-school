
namespace EGame
{
    public abstract class AbstractWorldMoveState
    {
        public abstract string ID { get; }
        public virtual bool IsMove => this is WorldStateMove;

        public abstract string GetNextState(Creature owner, Rng rng);

        public virtual void OnEnter()
        {

        }

        public virtual void OnUpdate(float delta)
        {

        }

        public virtual void OnExit()
        {

        }
    }
}
