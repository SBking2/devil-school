using Godot;
namespace EGame
{
	public partial class NCreature : Control 
	{
		private static readonly string NCREATURE_PREFAB_PATH = "combat/creature";
		public Creature Data { get; protected set;}

		private NCreatureVisual _Visual;

		private NCreatureStateDisplay _StateDisplay;

		public Control _VisualParent; 

		public static NCreature Create(Creature creature)
		{
			var instance = SceneHelper.LoadScene<NCreature>(NCREATURE_PREFAB_PATH);
			instance.Data = creature;
			return instance;
		}

		public override void _Ready()
		{
			base._Ready();

			_StateDisplay = GetNode<NCreatureStateDisplay>("%StateDisplay");
            _VisualParent = GetNode<Control>("%VisualParent");

			_StateDisplay.SetCreature(this);

			//创建Visual
			_Visual = Data.CreateVisuals();
			if(_Visual != null)
			{
                var parent = _VisualParent == null ? this : _VisualParent;
                parent.AddChild(_Visual);
                parent.MoveChild(_Visual, 0);
			}
		}
	}
}
