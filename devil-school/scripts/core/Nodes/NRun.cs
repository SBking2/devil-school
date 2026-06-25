
using Godot;

namespace EGame
{
	//开始游戏之后（进入存档之后）
	public partial class NRun : Control
	{
		private static readonly string RUN_SCENE_PATH = "run";
		private NSceneContainer _RoomContainer;

		public static NRun Instance => NGame.Instance?.RunNode;
		public NCombatRoom CombatRoomNode => _RoomContainer.CurrentScene as NCombatRoom;

		public static NRun Create()
		{
			var run = SceneHelper.LoadScene<Control>(RUN_SCENE_PATH);
			return run as NRun;
		}

		public override void _Ready()
		{
			base._Ready();
			_RoomContainer = GetNode<NSceneContainer>("%RoomContainer");
		}
		
		public void SetCurrentRoom(Control control)
		{
			_RoomContainer.SetScene(control);
		}
	}
}
