
namespace EGame
{
    public interface INCharacter : INDamageable
    {
        public Creature Data { get; }
        public void BuildAnimator(CreatureAnimator animator);
        public void AnimTrigger(string trigger);
    }
}