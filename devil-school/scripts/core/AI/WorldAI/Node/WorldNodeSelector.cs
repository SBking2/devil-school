
using System;
using System.Collections.Generic;

namespace EGame
{
    public class WorldNodeSelector : AbstractWorldBehaviorNode
    {
        private readonly string _NodeID;
        private readonly List<AbstractWorldBehaviorNode> _Children = new List<AbstractWorldBehaviorNode>();
        private int _CurrentIndex;

        public override string ID => _NodeID;

        protected override IEnumerable<AbstractWorldBehaviorNode> Children => _Children;

        public WorldNodeSelector(string id)
        {
            _NodeID = id;
        }

        public override void OnEnter(WorldBehaviorContext context)
        {
            _CurrentIndex = 0;
        }

        protected override WorldBehaviorStatus OnTick(WorldBehaviorContext context)
        {
            while (_CurrentIndex < _Children.Count)
            {
                var status = _Children[_CurrentIndex].Tick(context);

                if (status == WorldBehaviorStatus.Success)
                    return WorldBehaviorStatus.Success;

                if (status == WorldBehaviorStatus.Running)
                    return WorldBehaviorStatus.Running;

                _CurrentIndex++;
            }

            return WorldBehaviorStatus.Failure;
        }

        public void AddBranch(AbstractWorldBehaviorNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            _Children.Add(node);
            node.BindTree(Tree);
        }

        public void AddBrach(AbstractWorldBehaviorNode node)
        {
            AddBranch(node);
        }
    }
}
