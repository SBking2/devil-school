
namespace EGame
{
    public class WorldCmd : AbstractConsoleCmd
    {
        public override string CmdName => "world";

        public override string Args => "<string:world_name>";

        public override CmdResult Execute(string[] args)
        {
            if (NRun.Instance == null)
                return new CmdResult(false, "Must be running game!");

            if (args.Length < 1)
                return new CmdResult(false, "Must has at least one argument!");

            var world_name = args[0];
            world_name += "Model";
            var result = RunManager.Instance.DebugEnterEnviroment(world_name);
            var msg = result ? $"Successed enter scene : {world_name}" : $"Unknow scene : {world_name}";
            return new CmdResult(result, msg);
        }
    }
}