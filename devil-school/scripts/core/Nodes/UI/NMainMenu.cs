
using Godot;

namespace EGame
{
    public partial class NMainMenu : Control
    {
        private const string MAINMENU_SCENE_PATH = "ui/main_menu";
        public static NMainMenu Instance => NGame.Instance?.MainMenuNode;
        public static NMainMenu Create()
        {
            var result = SceneHelper.LoadScene<Control>(MAINMENU_SCENE_PATH);
            return result as NMainMenu;
        }
    }
}