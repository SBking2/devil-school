
namespace EGame
{
	public abstract class MonsterModel : AbstractModel
	{
		public virtual int MaxHP => 10;

		protected virtual string _VisualsPath => $"creature_visuals/" + ID.Entry.ToLowerInvariant();

		public NCreatureVisual CreateVisual()
		{
			return SceneHelper.LoadScene<NCreatureVisual>(_VisualsPath);
		}
	}
}
