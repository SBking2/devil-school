
namespace EGame
{
    public class Enviroment
    {
        public EnviromentModel EnviromentModel { get; }

        public Enviroment(EnviromentModel model)
        {
            EnviromentModel = model;
        }

        public void EnterEnviroment()
        {
            var n_enviroment = NEnviroment.Create(this);
            NRun.Instance.SetCurrentScene(n_enviroment);
        }
    }
}