
namespace EGame
{
    public abstract class AbstractCharacterMovementState
    {
        public abstract string StateName { get; }
        
        public virtual void OnEnter(CharacterMovementContext context)
        {
            
        }

        public virtual void OnUpdate(CharacterMovementContext context, double delta)
        {
            
        }

        public virtual void OnPhysicalUpdate(CharacterMovementContext context, double delta)
        {

        }

        public virtual void OnExit(CharacterMovementContext context)
        {

        }
    }
}
