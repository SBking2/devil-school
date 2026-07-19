using Godot;
namespace EGame
{
    /// <summary>
    /// Visual部分只关心纯渲染的组件和功能
    /// </summary>
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