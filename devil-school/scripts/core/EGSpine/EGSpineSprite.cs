using Godot;

namespace EGame
{
    public class EGSpineSprite : EGSpineBinding
    {
        protected override string SpineClassName => "SpineSprite";
        public EGSpineSprite(Variant obj) : base(obj) { }
    }
}