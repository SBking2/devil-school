
using System;
using System.Collections.Generic;

namespace EGame
{
    public class WorldBehaviorTree
    {
        private const int MAX_NODE_LOG_COUNT = 256;

        private AbstractWorldBehaviorNode _Root;
        private readonly Creature _Owner;
        private bool _NeedTick = true;

        public AbstractWorldBehaviorNode Root => _Root;
        public WorldBehaviorStatus LastStatus { get; private set; } = WorldBehaviorStatus.Failure;
        public Dictionary<string, object> Blackboard { get; } = new Dictionary<string, object>();
        public List<string> NodeLog { get; } = new List<string>();

        public WorldBehaviorTree(AbstractWorldBehaviorNode root, Creature owner)
        {
            SetRoot(root);
            _Owner = owner;
        }

        public void Update(double delta)
        {
            if (_NeedTick == false)
                return;

            var context = CreateContext();
            context.Delta = delta;

            TickRoot(context);
        }

        public void NotifyEvent(string event_id, object payload = null)
        {
            if (_Owner == null)
                return;

            var context = CreateContext();
            context.EventID = event_id;
            context.EventPayload = payload;

            _Root.OnTreeEvent(context);
            RequestEvaluate(context);
        }

        private void RequestEvaluate(WorldBehaviorContext context)
        {
            _Root.Abort(context);
            TickRoot(context);
        }

        private void SetRoot(AbstractWorldBehaviorNode root)
        {
            _Root = root ?? throw new ArgumentNullException(nameof(root));
            _Root.BindTree(this);
        }

        private void TickRoot(WorldBehaviorContext context)
        {
            LastStatus = _Root.Tick(context);
            _NeedTick = LastStatus == WorldBehaviorStatus.Running;
        }

        public void RecordNode(string node_id)
        {
            if (string.IsNullOrEmpty(node_id))
                return;

            NodeLog.Add(node_id);
            if (NodeLog.Count > MAX_NODE_LOG_COUNT)
                NodeLog.RemoveAt(0);
        }

        public void WakeUp()
        {
            _NeedTick = true;
        }

        private WorldBehaviorContext CreateContext()
        {
            return new WorldBehaviorContext(_Owner, null, this);
        }
    }
}
