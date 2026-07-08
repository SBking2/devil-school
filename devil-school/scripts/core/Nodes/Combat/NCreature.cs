using Godot;
namespace EGame
{
	public partial class NCreature : Control 
	{
		private static readonly string NCREATURE_PREFAB_PATH = "combat/creature";
		public Creature Data { get; protected set;}

		private NCreatureVisual _Visual;

		private NCreatureStateDisplay _StateDisplay;
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
			_StateDisplay.SetCreature(this);

			_Visual = Data.CreateVisuals();
			if(_Visual != null)
			{
				AddChild(_Visual);
				MoveChild(_Visual, 0);
			}
		}
	}
}
