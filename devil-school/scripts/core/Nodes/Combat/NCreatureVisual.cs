using Godot;
namespace EGame
{
    public partial class NCreatureVisual : Control
    {
        public EGSpineSprite SpineSprite { get; private set; }
        public bool IsSpine => SpineSprite != null;

        private Node2D _SpineBody;
        
        public override void _Ready()
        {
            base._Ready();
            _SpineBody = GetNode<Node2D>("%SpineBody");

            if(_SpineBody != null)
            {
                SpineSprite = new EGSpineSprite(_SpineBody);
            }
        }
    }
}