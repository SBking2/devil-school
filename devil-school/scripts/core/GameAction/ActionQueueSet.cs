
using System;
using System.Collections;
using System.Collections.Generic;

namespace EGame
{
	public class ActionQueueSet
	{
		/////////////////////////////////////////////////////////////////////////////////////////////////////////
		//////////                                  Action Queue
		/////////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// 管理队列内的GameAction，包括压入，弹出等等
		/// </summary>
		private class ActionQueue
		{
			private List<GameAction> _Queue = new List<GameAction>();
			public IReadOnlyList<GameAction> Actions
			{
				get
				{
					return _Queue;
				}
			}

			public GameAction EnQueue(GameAction action)
			{
				_Queue.Add(action);
				return action;
			}

			public GameAction PopQueue()
			{
				if(_Queue.Count > 0)
				{
					var action = _Queue[0];
					_Queue.RemoveAt(0);
					return action;
				}
				return null;
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////////////
		//////////                                  Action Queue Set
		/////////////////////////////////////////////////////////////////////////////////////////////////////////

		public event Action OnQueueChanged;

		private List<ActionQueue> _ActionQueues = new List<ActionQueue>();

		public ActionQueueSet(int player_count)
		{
			_ActionQueues.Clear();
			for (int i = 0; i < player_count; i++)
				_ActionQueues.Add(new ActionQueue());
		}

		public GameAction GetReadyAction()
		{
			foreach(var queue in _ActionQueues)
			{
				if (queue.Actions.Count > 0)
					return queue.Actions[0];
			}
			return null;
		}

		public void EnQueue(GameAction action)
		{
			_ActionQueues[0].EnQueue(action);
			action.OnTaskCompleted += PopQueue;
			if (OnQueueChanged != null)
				OnQueueChanged();
		}

		public void PopQueue(GameAction action)
		{
			if (_ActionQueues[0].Actions.Count > 0)
				_ActionQueues[0].PopQueue();
			action.OnTaskCompleted -= PopQueue;
		}
	}
}
