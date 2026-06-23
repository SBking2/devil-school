
using Godot;

namespace EGame
{
	/// <summary>
	/// 管理整个游戏的启动等等(应用级别)
	/// </summary>
	public partial class NGame : Control
	{
		public static NGame Instance { get; private set; }
		public NMainMenu MainMenuNode => _RootSceneContainer.CurrentScene as NMainMenu;
		public NRun RunNode => _RootSceneContainer.CurrentScene as NRun;

		private NSceneContainer _RootSceneContainer;
	
		public override void _EnterTree()
		{
			base._EnterTree();
			Instance = this;

			ModelDB.OnInit();
			Settins.LogLevel = Logger.LogLevel.Debug;
		}

		public override void _Ready()
		{
			base._Ready();
			_RootSceneContainer = GetNode<NSceneContainer>("%RootSceneContainer");
		}
		
		public void EnterMainMenu()
		{
			var main_menu = NMainMenu.Create();
			_RootSceneContainer.SetScene(main_menu);
		}

		public void StartSinglePlayerGame()
		{
			var player = Player.CreatureForNewRun(ModelDB.Character<PlayerDebugModel>().MutableClone() as CharacterModel, 0, 10);
			var runstate = RunState.CreateForSinglePlayer(player);
			RunManager.Instance.SetUpForSinglePlayer(runstate);

			var run = NRun.Create();
            _RootSceneContainer.SetScene(run);
        }
	}
}
