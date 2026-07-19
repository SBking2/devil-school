
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EGame
{
    public class MoveState : BaseMonsterMoveState
    {
        private readonly string _StateID;

        private readonly Func<IEnumerable<Creature>, Task> _MoveTask;
        public override string ID => _StateID;

        public MoveState(string id, Func<IEnumerable<Creature>, Task> move_task)
        {
            this._StateID = id;
        }

        public override string GetNextState()
        {
            throw new System.NotImplementedException();
        }

        public async Task ExecuteMove(IEnumerable<Creature> creatures)
        {
            if(_MoveTask == null)
                throw new InvalidOperationException($"{ID} Task is null!");

            await _MoveTask(creatures);
        }
    }
}