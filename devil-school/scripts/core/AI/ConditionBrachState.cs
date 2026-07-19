
namespace EGame
{
    public class ConditionBrachState : BaseMonsterMoveState
    {
        public override string ID => _StateID;

        private readonly string _StateID;

        public ConditionBrachState(string id)
        { 
            _StateID = id;
        }
    }
}