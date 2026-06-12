using Godot;
namespace EGame
{
	public partial class NCreature : Control
	{
		private static readonly string MCREATURE_PREFAB_PATH = "scenes/combat/creature.tscn";

		public Creature Data {get; protected set;}
		private NCreatureStateDisplay _StateDisplay;

		public static NCreature Create()
		{
			return null;
		}

		public override void _Ready()
		{
			base._Ready();
			_StateDisplay = GetNode<NCreatureStateDisplay>("%StateDisplay");
		}
	}
}
