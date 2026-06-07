
namespace EGame
{
    public abstract class AbstractConsoleCmd
    {
        public abstract string CmdName { get; }
        public abstract string Args { get; }
        public virtual bool DebugOnly => true;
        public abstract CmdResult Execute(string[] args);
    }
}