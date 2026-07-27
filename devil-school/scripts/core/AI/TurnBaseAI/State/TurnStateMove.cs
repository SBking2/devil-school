
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EGame
{
    public class TurnStateMove : AbstractTurnMoveState
    {
        private readonly string _StateID;

        private readonly Func<IEnumerable<Creature>, Task> _MoveTask;
        public override string ID => _StateID;
        public string NextStateID { get; }

        public TurnStateMove(string id, Func<IEnumerable<Creature>, Task> move_task, string next_state_id = null)
        {
            this._StateID = id;
            this._MoveTask = move_task;
            this.NextStateID = next_state_id;
        }

        public override string GetNextState(Creature owner, Rng rng)
        {
            if (string.IsNullOrWhiteSpace(NextStateID))
                throw new InvalidOperationException($"{ID} doesn't have the next state!");

            return NextStateID;
        }

        public async Task ExecuteMove(IEnumerable<Creature> creatures)
        {
            if(_MoveTask == null)
                throw new InvalidOperationException($"{ID} Task is null!");

            await _MoveTask(creatures);
        }
    }
}
