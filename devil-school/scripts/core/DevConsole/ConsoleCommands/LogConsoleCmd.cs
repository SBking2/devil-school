
using System;

namespace EGame
{
    public class LogConsoleCmd : AbstractConsoleCmd
    {
        public override string CmdName => "log";
        public override string Args => "<string:log_level>";
        public override bool DebugOnly => false;

        public override CmdResult Execute(string[] args)
        {
            if(args.Length < 1)
                return new CmdResult(false, "Must supply the log level as the first argument!");
            else
            {
                Logger.LogLevel new_level;
                if(fun.TryGetEnum<Logger.LogLevel>(args[0], out new_level))
                {
                    Settins.LogLevel = new_level;
                    return new CmdResult(true, $"Set log level to {new_level}");
                }
                else
                    return new CmdResult(false, "Invalid log level!");
            }
        }
    }
}