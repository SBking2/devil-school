
using System;
using System.Collections.Generic;

namespace EGame
{
    public class WorldNodeDecorator : AbstractWorldBehaviorNode
    {
        private readonly string _NodeID;
        private readonly AbstractWorldBehaviorNode _Child;
        private readonly Func<WorldBehaviorContext, bool> _CanStartFunc;
        private readonly Func<WorldBehaviorContext, WorldBehaviorStatus, WorldBehaviorStatus> _ResultFunc;
        private readonly Action<WorldBehaviorContext> _EnterAction;
        private readonly Action<WorldBehaviorContext> _ExitAction;
        private readonly Action<WorldBehaviorContext> _EventAction;

        public override string ID => _NodeID;

        protected override IEnumerable<AbstractWorldBehaviorNode> Children
        {
            get
            {
                if (_Child != null)
                    yield return _Child;
            }
        }

        public WorldNodeDecorator(
            string id,
            AbstractWorldBehaviorNode child,
            Func<WorldBehaviorContext, bool> can_start_func = null,
            Func<WorldBehaviorContext, WorldBehaviorStatus, WorldBehaviorStatus> result_func = null,
            Action<WorldBehaviorContext> on_enter = null,
            Action<WorldBehaviorContext> on_exit = null,
            Action<WorldBehaviorContext> on_event = null)
        {
            _NodeID = id;
            _Child = child ?? throw new ArgumentNullException(nameof(child));
            _CanStartFunc = can_start_func;
            _ResultFunc = result_func;
            _EnterAction = on_enter;
            _ExitAction = on_exit;
            _EventAction = on_event;
        }

        protected override bool CanStart(WorldBehaviorContext context)
        {
            return _CanStartFunc == null || _CanStartFunc.Invoke(context);
        }

        public override void OnEnter(WorldBehaviorContext context)
        {
            _EnterAction?.Invoke(context);
        }

        public override void OnTreeEvent(WorldBehaviorContext context)
        {
            _EventAction?.Invoke(context);
            base.OnTreeEvent(context);
        }

        protected override WorldBehaviorStatus OnTick(WorldBehaviorContext context)
        {
            var status = _Child.Tick(context);
            return _ResultFunc == null ? status : _ResultFunc.Invoke(context, status);
        }

        public override void OnExit(WorldBehaviorContext context)
        {
            _ExitAction?.Invoke(context);
        }
    }
}
