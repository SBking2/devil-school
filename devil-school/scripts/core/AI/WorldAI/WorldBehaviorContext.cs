
using System.Collections.Generic;

namespace EGame
{
    public class WorldBehaviorContext
    {
        public NEnvCreature Owner { get; }
        public Rng Rng { get; }
        public WorldBehaviorTree Tree { get; }
        public Dictionary<string, object> Blackboard => Tree.Blackboard;
        public AbstractWorldBehaviorNode ActiveNode { get; set; }
        public double ActiveNodeRunningTime => ActiveNode?.RunningTime ?? 0.0;
        public int ActiveNodeRunningTickCount => ActiveNode?.RunningTickCount ?? 0;

        public double Delta { get; set; }
        public WorldAIEvent EventID { get; set; }
        public object EventPayload { get; set; }

        public WorldBehaviorContext(NEnvCreature owner, Rng rng, WorldBehaviorTree tree)
        {
            Owner = owner;
            Rng = rng ?? Rng.RealRandom;
            Tree = tree;
        }
    }
}
