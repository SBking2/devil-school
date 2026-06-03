
using Godot;

namespace EGame
{
    /// <summary>
    /// 管理整个游戏的启动等等(应用级别)
    /// </summary>
    public partial class NGame : Control
    {
        public static NGame Instance { get; private set; }

        public override void _EnterTree()
        {
            base._EnterTree();
            Logger.Debug("Game Start!");
            Instance = this;
        }
    }
}