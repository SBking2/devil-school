
namespace EGame
{
    public class SceneConsoleCmd : AbstractConsoleCmd
    {
        public override string CmdName => "scene";
        public override string Args => "<string:scene_type>";
        public override bool DebugOnly => false;
        public override CmdResult Execute(string[] args)
        {
            if(args.Length < 1)
                return new CmdResult(false, "Must supply the scene type as the first argument!");
            else
            {
                return new CmdResult(true, $"Set scene type to {args[0]}");
            }
        }
    }
}