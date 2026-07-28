
using System.Collections.Generic;

namespace EGame
{
    public class WorldBehaviorContext
    {
        public Creature Owner { get; }
        public Rng Rng { get; }
        public WorldBehaviorTree Tree { get; }
        public Dictionary<string, object> Blackboard => Tree.Blackboard;
        public AbstractWorldBehaviorNode ActiveNode { get; internal set; }
        public double ActiveNodeRunningTime => ActiveNode?.RunningTime ?? 0.0;
        public int ActiveNodeRunningTickCount => ActiveNode?.RunningTickCount ?? 0;

        public double Delta { get; internal set; }
        public string EventID { get; internal set; }
        public object EventPayload { get; internal set; }

        internal WorldBehaviorContext(Creature owner, Rng rng, WorldBehaviorTree tree)
        {
            Owner = owner;
            Rng = rng ?? Rng.RealRandom;
            Tree = tree;
        }
    }
}
