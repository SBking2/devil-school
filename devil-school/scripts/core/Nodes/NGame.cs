
using Godot;
using System.Collections.Generic;

namespace EGame
{
	/// <summary>
	/// 管理整个游戏的启动等等(应用级别)
	/// </summary>
	public partial class NGame : Node
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
			Settins.LogLevel = Log.LogLevel.Debug;
		}

		public override void _Ready()
		{
			base._Ready();
			_RootSceneContainer = GetNode<NSceneContainer>("%RootSceneContainer");

			//Debug
			StartSinglePlayerGame();
			RunManager.Instance.EnterEnviroment<InitWorldModel>();
		}
		
		public void EnterMainMenu()
		{
			var main_menu = NMainMenu.Create();
			_RootSceneContainer.SetScene(main_menu);
		}

		public void StartSinglePlayerGame()
		{
			var player = Player.CreatureForNewRun(ModelDB.Player<RobotModel>().MutableClone() as PlayerModel, 0, 10);
			var runstate = RunState.CreateForNewRun(new List<Player>() { player });
			RunManager.Instance.SetUpForNewRun(runstate);

			var run = NRun.Create();
			_RootSceneContainer.SetScene(run);
		}
	}
}
