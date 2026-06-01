
using System.Threading.Tasks;

namespace EGame
{
    public class DelayAction : GameAction
    {
        protected override async Task GetActionTask()
        {
            Logger.Debug("Delay Action !");
            await Task.Delay(1000);
        }
    }
}