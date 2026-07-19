
namespace EGame
{
    public abstract class BaseMonsterMoveState
    {
        public abstract string ID { get; }

        //是否是行为节点
        public virtual bool IsMove => this is MoveState;

        public abstract string GetNextState();

        public virtual void OnEnter()
        {

        }

        public virtual void OnExit()
        {

        }
    }
}