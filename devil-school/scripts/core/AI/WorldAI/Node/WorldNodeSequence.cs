
using System;
using System.Collections.Generic;

namespace EGame
{
    public class WorldNodeSequence : AbstractWorldBehaviorNode
    {
        private readonly string _NodeID;
        private readonly List<AbstractWorldBehaviorNode> _Children = new List<AbstractWorldBehaviorNode>();
        private int _CurrentIndex;

        public override string ID => _NodeID;

        protected override IEnumerable<AbstractWorldBehaviorNode> Children => _Children;

        public WorldNodeSequence(string id)
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

                if (status == WorldBehaviorStatus.Failure)
                    return WorldBehaviorStatus.Failure;

                if (status == WorldBehaviorStatus.Running)
                    return WorldBehaviorStatus.Running;

                _CurrentIndex++;
            }

            return WorldBehaviorStatus.Success;
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
