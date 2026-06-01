
using System.Threading.Tasks;

namespace EGame
{
    public class Delay2Action : GameAction
    {
        protected override async Task GetActionTask()
        {
            Logger.Error("Delay2 Action");
            await Task.Delay(500);
        }
    }
}