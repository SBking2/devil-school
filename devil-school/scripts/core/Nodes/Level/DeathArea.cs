
using Godot;

namespace EGame
{
    public partial class DeathArea : Area3D
    {
        public override void _Ready()
        {
            base._Ready();
            BodyEntered += KillCharacter;
        }

        private void KillCharacter(Node3D body)
        {
            Log.Debug($"Try Kill {body.Name}");
            if (body != null)
            {
                var character = body as INCharacter;
                if (character != null)
                {
                    character.Die();
                }    
            }
        }
    }
}