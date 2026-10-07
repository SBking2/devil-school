
namespace EGame
{
    public class CollisionVisConsoleCmd : AbstractConsoleCmd
    {
        public override string CmdName => "colvis";
        public override string Args => "[string:on|off]";

        public override CmdResult Execute(string[] args)
        {
            bool enable = args.Length < 1 ? !Settings.DrawCollisionQueries : args[0].ToLowerInvariant() == "on";
            Settings.DrawCollisionQueries = enable;
            return new CmdResult(true, enable ? "Collision query draw on" : "Collision query draw off");
        }
    }
}
