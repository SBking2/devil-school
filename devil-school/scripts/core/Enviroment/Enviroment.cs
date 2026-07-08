
namespace EGame
{
    public class Enviroment
    {
        public EnviromentModel EnviromentModel { get; }
        public EnviromentState EnvState { get; }

        public Enviroment(EnviromentModel model)
        {
            EnviromentModel = model;
            EnvState = new EnviromentState();

            var players = RunManager.Instance.RunState.Players;
            foreach (var player in players)
                EnvState.AddPlayer(player);
        }

        public void EnterEnviroment()
        {
            var n_enviroment = NEnviroment.Create(this);
            NRun.Instance.SetCurrentScene(n_enviroment);
        }
    }
}