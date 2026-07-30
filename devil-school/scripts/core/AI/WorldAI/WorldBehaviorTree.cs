
using System;
using System.Collections.Generic;

namespace EGame
{
    public class WorldBehaviorTree
    {
        private const int MAX_NODE_LOG_COUNT = 256;

        private readonly AbstractWorldBehaviorNode _Root;
        private readonly NEnvCreature _Owner;
        private readonly Rng _Rng;
        private bool _NeedTick = true;

        public AbstractWorldBehaviorNode Root => _Root;
        public WorldBehaviorStatus LastStatus { get; private set; } = WorldBehaviorStatus.Failure;
        public Dictionary<string, object> Blackboard { get; } = new Dictionary<string, object>();
        public List<string> NodeLog { get; } = new List<string>();

        public WorldBehaviorTree(AbstractWorldBehaviorNode root, NEnvCreature owner, Rng rng = null)
        {
            this._Root = root;
            _Owner = owner;
            _Rng = rng ?? Rng.RealRandom;
        }

        public void Update(double delta)
        {
            if (_NeedTick == false)
                return;

            var context = CreateContext();
            context.Delta = delta;

            TickRoot(context);
        }

        public void NotifyEvent(WorldAIEvent event_id, object payload = null)
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

            Logger.VeryDebug($"World AI Enter Node {node_id} By {_Owner.Data.CharacterModel.ID.ToString()}");
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
