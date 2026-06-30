using System;

namespace EGame
{
    public class RandomNumberGenerator
    {
        private Random _Random;
        private int _Counter;
        private uint _Seed;

        public RandomNumberGenerator(uint seed = 0u, int counter = 0)
        {
            _Random = new Random((int)seed);
            _Counter = 0;
            _Seed = seed;
            FastConsumer(counter);
        }

        public RandomNumberGenerator(uint seed, string name):this(seed + name.ToDeterMinisticHashCode())
        {
            
        }


        //快速的销毁掉随机数，以达到存档当前的位置
        private void FastConsumer(int target_counter)
        {
            if(_Counter > target_counter)
                throw new InvalidOperationException($"Invalid Random Counter : {target_counter}");

            while(_Counter < target_counter)
            {
                _Counter++;
                _Random.Next();
            }
        }
    }
}