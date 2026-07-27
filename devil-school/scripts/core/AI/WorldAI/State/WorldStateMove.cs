
using System;

namespace EGame
{
    public class WorldStateMove : AbstractWorldMoveState
    {
        private readonly string _StateID;
        private readonly Func<string> _NextStateFunc;
        private readonly Action _EnterAction;
        private readonly Action<float> _UpdateAction;
        private readonly Action _ExitAction;

        public override string ID => _StateID;

        public WorldStateMove(
            string id,
            Func<string> next_state_func = null,
            Action on_enter = null,
            Action<float> on_update = null,
            Action on_exit = null)
        {
            _StateID = id;
            _NextStateFunc = next_state_func;
            _EnterAction = on_enter;
            _UpdateAction = on_update;
            _ExitAction = on_exit;
        }

        public override string GetNextState(Creature owner, Rng rng)
        {
            return _NextStateFunc?.Invoke();
        }

        public override void OnEnter()
        {
            _EnterAction?.Invoke();
        }

        public override void OnUpdate(float delta)
        {
            _UpdateAction?.Invoke(delta);
        }

        public override void OnExit()
        {
            _ExitAction?.Invoke();
        }
    }
}
