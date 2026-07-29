
using System;
using System.Collections.Generic;

namespace EGame
{
    public abstract class WorldNodeDecorator : AbstractWorldBehaviorNode
    {
        private readonly string _NodeID;
        private readonly AbstractWorldBehaviorNode _Child;

        public override string ID => _NodeID;

        protected AbstractWorldBehaviorNode Child => _Child;

        protected override IEnumerable<AbstractWorldBehaviorNode> Children
        {
            get
            {
                yield return _Child;
            }
        }

        protected WorldNodeDecorator(string id, AbstractWorldBehaviorNode child)
        {
            _NodeID = id;
            _Child = child ?? throw new ArgumentNullException(nameof(child));
        }

        protected override WorldBehaviorStatus OnTick(WorldBehaviorContext context)
        {
            var status = _Child.Tick(context);
            return DecorateStatus(context, status);
        }

        protected virtual WorldBehaviorStatus DecorateStatus(WorldBehaviorContext context, WorldBehaviorStatus status)
        {
            return status;
        }
    }
}
