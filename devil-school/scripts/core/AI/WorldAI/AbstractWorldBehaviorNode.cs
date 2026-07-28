
using System.Collections.Generic;

namespace EGame
{
    public abstract class AbstractWorldBehaviorNode
    {
        private bool _IsStarted;

        public abstract string ID { get; }
        public bool IsStarted => _IsStarted;
        public double RunningTime { get; private set; }
        public int RunningTickCount { get; private set; }

        protected virtual IEnumerable<AbstractWorldBehaviorNode> Children
        {
            get
            {
                yield break;
            }
        }

        public virtual void OnTreeEvent(WorldBehaviorContext context)
        {
            foreach (var child in Children)
                child.OnTreeEvent(context);
        }

        public WorldBehaviorStatus Tick(WorldBehaviorContext context)
        {
            var previous_node = context.ActiveNode;
            context.ActiveNode = this;

            try
            {
                if (_IsStarted == false)
                {
                    if (CanStart(context) == false)
                        return WorldBehaviorStatus.Failure;

                    _IsStarted = true;
                    ResetTimer();
                    context.Tree.RecordNode(ID);
                    OnEnter(context);
                }

                RunningTime += context.Delta;
                RunningTickCount++;

                var status = OnTick(context);
                if (status != WorldBehaviorStatus.Running)
                {
                    OnExit(context);
                    _IsStarted = false;
                }

                return status;
            }
            finally
            {
                context.ActiveNode = previous_node;
            }
        }

        public void Abort(WorldBehaviorContext context)
        {
            foreach (var child in Children)
                child.Abort(context);

            if (_IsStarted == false)
                return;

            var previous_node = context.ActiveNode;
            context.ActiveNode = this;

            try
            {
                OnExit(context);
                _IsStarted = false;
            }
            finally
            {
                context.ActiveNode = previous_node;
            }
        }

        public void ResetTimer()
        {
            RunningTime = 0.0;
            RunningTickCount = 0;
        }

        protected virtual bool CanStart(WorldBehaviorContext context)
        {
            return true;
        }

        public virtual void OnEnter(WorldBehaviorContext context)
        {

        }

        protected abstract WorldBehaviorStatus OnTick(WorldBehaviorContext context);

        public virtual void OnExit(WorldBehaviorContext context)
        {

        }
    }
}
