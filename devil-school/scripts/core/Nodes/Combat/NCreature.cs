using Godot;
namespace EGame
{
	public partial class NCreature : Control
	{
		private static readonly string NCREATURE_PREFAB_PATH = "scenes/combat/creature.tscn";
		public Creature Data {get; protected set;}

		private NCreatureVisual _NCreatureVisual;

		private NCreatureStateDisplay _StateDisplay;
		public static NCreature Create(Creature creature)
		{
			var instance = SceneHelper.LoadScene<NCreature>(NCREATURE_PREFAB_PATH);
			instance.Data = creature;
			instance._NCreatureVisual = creature.CreateVisuals();
			return instance;
		}

		public override void _Ready()
		{
			base._Ready();

			_StateDisplay = GetNode<NCreatureStateDisplay>("%StateDisplay");
			_StateDisplay.SetCreature(this);

			if(_NCreatureVisual != null)
			{
				AddChild(_NCreatureVisual);
				MoveChild(_NCreatureVisual, 0);
			}
		}
	}
}
