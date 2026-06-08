
using Godot;

namespace EGame
{
    /// <summary>
    /// 管理整个游戏的启动等等(应用级别)
    /// </summary>
    public partial class NGame : Control
    {
        public static NGame Instance { get; private set; }

        private NSceneContainer _RootSceneContainer;
        
        public override void _EnterTree()
        {
            base._EnterTree();
            Instance = this;

            
        }

        public override void _Ready()
        {
            base._Ready();
            _RootSceneContainer = GetNode<NSceneContainer>("%RootSceneContainer");
            EnterMainMenu();

            Settins.LogLevel = Logger.LogLevel.Debug;
        }

        private void EnterMainMenu()
        {
            var main_menu = LoadManager.LoadScene<Control>("res://scenes/ui/main_menu.tscn");
            _RootSceneContainer.SetScene(main_menu);
        }
    }
}