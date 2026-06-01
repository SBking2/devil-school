
using System.Threading.Tasks;

namespace EGame
{
	/// <summary>
	/// 只负责执行
	/// </summary>
	public class ActionExecutor
	{
		private ActionQueueSet _QueueSet;

		private TaskCompletionSource _RunningTCS;   //用于给外部await ActionExectuor执行命令的信号

		public ActionQueueSet QueueSet
		{
			get
			{
				return _QueueSet;
			}
		}

		public bool IsRunning
		{
			get
			{
				if (_RunningTCS != null)
					return !_RunningTCS.Task.IsCompleted;
				return false;
			}
		}

		/// <summary>
		/// 一个让外部等待的Task
		/// </summary>
		public Task ExecuteTask
		{
			get
			{
				if (_RunningTCS != null)
					return _RunningTCS.Task;
				return Task.CompletedTask;
			}
		}

		public ActionExecutor(ActionQueueSet queue_set)
		{
			_QueueSet = queue_set;
			_QueueSet.OnQueueChanged += Run;
		}
		
		public async Task Execute()
		{
			_RunningTCS = new TaskCompletionSource();
			GameAction ready_game_action = _QueueSet.GetReadyAction();
			while(ready_game_action != null)
			{
				await ready_game_action.Excute();
				ready_game_action = _QueueSet.GetReadyAction();
			}
			_RunningTCS.SetResult();
			Logger.Debug("Action Exectuor Stop!");
		}

		private void Run()
		{
			if(IsRunning == false)
			{
				Logger.Debug("Action Exectuor Execute!");
				TaskHelper.RunSafely(Execute());
			}
		}
	}
}
