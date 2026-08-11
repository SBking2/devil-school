
namespace EGame
{
    public class PlayerMovementContext
    {
        public PlayerMovementContext(NEnvCreature creature)
        {
            Owner = creature;
        }

        public NEnvCreature Owner { get; }
    }
}