
using System;
using System.Collections.Generic;

namespace EGame
{
    public class WorldBehaviorTree
    {
        private const int MAX_NODE_LOG_COUNT = 256;

        private AbstractWorldBehaviorNode _Root;
        private Creature _Owner;
        private Rng _Rng;
        private bool _NeedTick = true;

        public AbstractWorldBehaviorNode Root => _Root;
        public WorldBehaviorStatus LastStatus { get; private set; } = WorldBehaviorStatus.Failure;
        public Dictionary<string, object> Blackboard { get; } = new Dictionary<string, object>();
        public List<string> NodeLog { get; } = new List<string>();

        public WorldBehaviorTree(AbstractWorldBehaviorNode root, Creature owner = null, Rng rng = null)
        {
            SetRoot(root);
            _Owner = owner;
            _Rng = rng ?? Rng.RealRandom;
        }

        public void SetOwner(Creature owner)
        {
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

        public void Update(Creature owner, double delta, Rng rng = null)
        {
            if (owner != null)
                _Owner = owner;

            if (rng != null)
                _Rng = rng;

            Update(delta);
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

        public void NotifyEvent(Creature owner, string event_id, object payload = null, Rng rng = null)
        {
            if (owner != null)
                _Owner = owner;

            if (rng != null)
                _Rng = rng;

            NotifyEvent(event_id, payload);
        }

        private void RequestEvaluate(WorldBehaviorContext context)
        {
            _Root.Abort(context);
            TickRoot(context);
        }

        private void SetRoot(AbstractWorldBehaviorNode root)
        {
            _Root = root ?? throw new ArgumentNullException(nameof(root));
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
            return new WorldBehaviorContext(_Owner, _Rng, this);
        }
    }
}
