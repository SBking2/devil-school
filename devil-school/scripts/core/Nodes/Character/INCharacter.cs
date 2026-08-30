
namespace EGame
{
    public interface INCharacter : INDamageable
    {
        public CharacterModel Data { get; }
        public void BuildAnimator(CreatureAnimator animator);
        public void AnimTrigger(string trigger);
        protected void OnDead();
    }
}