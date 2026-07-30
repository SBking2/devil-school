
using System;
using System.Collections.Generic;

namespace EGame
{
    public class WorldNodeSelector : AbstractWorldBehaviorNode
    {
        private readonly string _NodeID;
        private readonly List<AbstractWorldBehaviorNode> _Children = new List<AbstractWorldBehaviorNode>();
        private AbstractWorldBehaviorNode _CurrentNode;

        public override string ID => _NodeID;

        protected override IEnumerable<AbstractWorldBehaviorNode> Children => _Children;

        public WorldNodeSelector(string id)
        {
            _NodeID = id;
        }

        public override void OnEnter(WorldBehaviorContext context)
        {
            _CurrentNode = null;
        }

        protected override WorldBehaviorStatus OnTick(WorldBehaviorContext context)
        {
            for (int i = 0; i < _Children.Count; i++)
            {
                var child = _Children[i];
                var status = child.Tick(context);

                if (status == WorldBehaviorStatus.Failure)
                    continue;

                if (_CurrentNode != child)
                {
                    _CurrentNode?.Abort(context);
                    _CurrentNode = child;
                }

                if (status != WorldBehaviorStatus.Running)
                    _CurrentNode = null;

                return status;
            }

            _CurrentNode?.Abort(context);
            _CurrentNode = null;
            return WorldBehaviorStatus.Failure;
        }

        public override void OnExit(WorldBehaviorContext context)
        {
            _CurrentNode?.Abort(context);
            _CurrentNode = null;
        }

        public void AddBranch(AbstractWorldBehaviorNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            _Children.Add(node);
        }
    }
}
