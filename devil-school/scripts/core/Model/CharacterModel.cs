
using Godot;
using System;

namespace EGame
{
    public abstract class CharacterModel : AbstractModel
    {
        public virtual int MaxHP => 10;
        public virtual float MoveSpeed => 8f;
    }
}
