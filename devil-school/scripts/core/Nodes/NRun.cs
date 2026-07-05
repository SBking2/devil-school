
using Godot;

namespace EGame
{
	//开始游戏之后（进入存档之后）
	public partial class NRun : Control
	{
		private static readonly string RUN_SCENE_PATH = "run";
		private NSceneContainer _SceneContainer;
		public static NRun Instance => NGame.Instance?.RunNode;
		public NCombatRoom CombatRoomNode => _SceneContainer.CurrentScene as NCombatRoom;
		public NEnviroment EnviromentNode => _SceneContainer.CurrentScene as NEnviroment;

		public static NRun Create()
		{
			var run = SceneHelper.LoadScene<Control>(RUN_SCENE_PATH);
			return run as NRun;
		}
		
		public override void _Ready()
		{
			base._Ready();
			_SceneContainer = GetNode<NSceneContainer>("%SceneContainer");
		}
		
		public void SetCurrentScene(Control control)
		{
			_SceneContainer.SetScene(control);
		}
	}
}
