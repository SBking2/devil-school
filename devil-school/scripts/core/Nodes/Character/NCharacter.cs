
using Godot;

namespace EGame
{
    public partial class NCharacterController : CharacterBody3D, INCharacter
    {
        public Creature Data { get; private set; }
    }
}