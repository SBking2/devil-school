
namespace EGame
{
    public interface INCharacter : INDamageable, INAttackable
    {
        public CharacterModel Data { get; }
        public void BuildAnimator(CreatureAnimator animator);
        public void AnimTrigger(string trigger);
        public void Die();
    }
}