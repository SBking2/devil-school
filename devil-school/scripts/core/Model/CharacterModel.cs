
using Godot;
using System;

namespace EGame
{
    public abstract class CharacterModel : AbstractModel
    {
        public virtual int MaxHP => 10;
        public virtual float MoveSpeed => 8f;
        protected virtual CreatureAnimator BuildAnimator(INCharacter character)
        {
            return null;
        }

        public override void OnCharacterCreated(INCharacter character)
        {
            character.BuildAnimator(BuildAnimator(character));
        }
    }
}
