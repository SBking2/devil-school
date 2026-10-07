
namespace EGame
{
    // 判断本次决策有没有收到指定事件，配合 NAgent.NotifyEvent 使用
    public class AgentBehaviourEventCondition : AbstractAgentBehaviourDecorator
    {
        private readonly string _EventName;

        public AgentBehaviourEventCondition(string eventName, AbstractAgentBehaviourNode child) : base(child)
        {
            _EventName = eventName;
        }

        protected override bool ShouldRun(NAgent agent)
        {
            return agent.HasEvent(_EventName);
        }
    }
}
