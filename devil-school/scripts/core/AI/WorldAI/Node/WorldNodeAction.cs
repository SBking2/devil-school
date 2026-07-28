
using System;

namespace EGame
{
    public class WorldNodeAction : AbstractWorldBehaviorNode
    {
        private readonly string _NodeID;
        private readonly Func<WorldBehaviorContext, bool> _CanStartFunc;
        private readonly Action<WorldBehaviorContext> _EnterAction;
        private readonly Func<WorldBehaviorContext, WorldBehaviorStatus> _TickFunc;
        private readonly Action<WorldBehaviorContext> _ExitAction;
        private readonly Action<WorldBehaviorContext> _EventAction;

        public override string ID => _NodeID;

        public WorldNodeAction(
            string id,
            Func<WorldBehaviorContext, bool> can_start_func = null,
            Action<WorldBehaviorContext> on_enter = null,
            Func<WorldBehaviorContext, WorldBehaviorStatus> on_tick = null,
            Action<WorldBehaviorContext> on_exit = null,
            Action<WorldBehaviorContext> on_event = null)
        {
            _NodeID = id;
            _CanStartFunc = can_start_func;
            _EnterAction = on_enter;
            _TickFunc = on_tick;
            _ExitAction = on_exit;
            _EventAction = on_event;
        }

        protected override bool CanStart(WorldBehaviorContext context)
        {
            return _CanStartFunc == null || _CanStartFunc.Invoke(context);
        }

        protected override WorldBehaviorStatus OnTick(WorldBehaviorContext context)
        {
            if (_TickFunc == null)
                return WorldBehaviorStatus.Running;

            return _TickFunc.Invoke(context);
        }

        public override void OnEnter(WorldBehaviorContext context)
        {
            _EnterAction?.Invoke(context);
        }

        public override void OnTreeEvent(WorldBehaviorContext context)
        {
            _EventAction?.Invoke(context);
        }

        public override void OnExit(WorldBehaviorContext context)
        {
            _ExitAction?.Invoke(context);
        }
    }
}
