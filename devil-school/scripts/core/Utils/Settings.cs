
namespace EGame
{
    public static class Settings
    {
        public static Log.LogLevel LogLevel { get; set; } = Log.LogLevel.VeryDebug;
        public static Log.LogType LogType { get; set; } = Log.LogType.None;

        // 把射线、球形检测这类瞬时碰撞查询画出来，用 colvis 命令开关
        public static bool DrawCollisionQueries { get; set; } = false;
    }
}