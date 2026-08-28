
namespace EGame
{
    // 判断本次 TreeTick 有没有收到指定事件，配合 NAgent.NotifyEvent 使用
    public class AgentBehaviorEventCondition : AbstractAgentBehaviorDecorator
    {
        private readonly string _EventName;

        public AgentBehaviorEventCondition(string eventName, AbstractAgentBehaviorNode child) : base(child)
        {
            _EventName = eventName;
        }

        protected override bool ShouldRun(NAgent agent, double dt)
        {
            return agent.HasEvent(_EventName);
        }
    }
}
