
namespace EGame
{
    public class RandomBrachState : BaseMonsterMoveState
    {
        private readonly string _StateID;
        public override string ID => _StateID;
        public RandomBrachState(string id)
        {
            _StateID= id;
        }
    }
}