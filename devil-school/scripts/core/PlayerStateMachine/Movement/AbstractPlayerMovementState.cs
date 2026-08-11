
namespace EGame
{
    public abstract class AbstractPlayerMovementState
    {
        public abstract string StateName { get; }
        
        public virtual void OnEnter(PlayerMovementContext context)
        {
            
        }

        public virtual void OnUpdate(PlayerMovementContext context, double delta)
        {
            
        }

        public virtual void OnExit(PlayerMovementContext context)
        {

        }
    }
}