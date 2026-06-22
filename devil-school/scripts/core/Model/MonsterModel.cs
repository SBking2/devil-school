
namespace EGame
{
	public abstract class MonsterModel : AbstractModel
	{
		public virtual int MaxHP => 10;

		protected virtual string VisualsPath => $"creature_visuals/" + ID.Entry.ToLowerInvariant();

		public NCreatureVisual CreateVisual()
		{
			return SceneHelper.LoadScene<NCreatureVisual>(VisualsPath);
		}
	}
}
