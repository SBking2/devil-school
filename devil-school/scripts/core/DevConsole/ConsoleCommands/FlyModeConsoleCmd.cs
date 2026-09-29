
namespace EGame
{
    public class FlyModeConsoleCmd : AbstractConsoleCmd
    {
        public override string CmdName => "fly";
        public override string Args => "<string:active>";
        public override bool DebugOnly => false;

        public override CmdResult Execute(string[] args)
        {
            var player = NGame.Instance?.PlayerNode;
            if (player == null)
                return new CmdResult(false, "Player not found!");

            if (args.Length < 1)
                return new CmdResult(false, "Must at least one argument!");

            bool is_flying = args[0] == "on";

            if(is_flying)
            {
                NGame.Instance.IsFlyCheatOn = true;
            }
            else
            {
                NGame.Instance.IsFlyCheatOn = false;
                NGame.Instance.PlayerNode.ResetFly();
            }

            return new CmdResult(true, is_flying ? "Fly mode on! Press Alt to fly " : "Fly mode off");
        }
    }
}
