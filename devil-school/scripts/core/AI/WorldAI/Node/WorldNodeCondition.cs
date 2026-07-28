
using System;

namespace EGame
{
    public class WorldNodeCondition : AbstractWorldBehaviorNode
    {
        private readonly string _NodeID;
        private readonly Func<WorldBehaviorContext, bool> _ConditionFunc;

        public override string ID => _NodeID;

        public WorldNodeCondition(string id, Func<WorldBehaviorContext, bool> condition_func)
        {
            _NodeID = id;
            _ConditionFunc = condition_func;
        }

        public WorldNodeCondition(string id, Func<bool> condition_func)
            : this(id, (_) => condition_func.Invoke())
        {

        }

        protected override WorldBehaviorStatus OnTick(WorldBehaviorContext context)
        {
            if (_ConditionFunc == null)
                throw new InvalidOperationException($"Condition Node : {ID} doesn't have condition!");

            return _ConditionFunc.Invoke(context) ? WorldBehaviorStatus.Success : WorldBehaviorStatus.Failure;
        }
    }
}
