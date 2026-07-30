
namespace EGame
{
    public class ZombieHasTargetNode : WorldNodeCondition
    {
        public ZombieHasTargetNode() : base("has_chase_target")
        {

        }

        public override void OnTreeEvent(WorldBehaviorContext context)
        {
            base.OnTreeEvent(context);
            
            if(context.EventID == WorldAIEvent.FindTarget)
            {
                NEnvCreature target = context.EventPayload as NEnvCreature;
                context.Blackboard[ZombieAI.TargetKey] = target;
            }
            else if(context.EventID == WorldAIEvent.MissingTarget)
            {
                if(context.Blackboard.ContainsKey(ZombieAI.TargetKey))
                    context.Blackboard.Remove(ZombieAI.TargetKey);
            }
        }

        protected override bool CheckCondition(WorldBehaviorContext context)
        {
            return context.Blackboard.ContainsKey(ZombieAI.TargetKey);
        }
    }
}
