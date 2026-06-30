using System;

namespace EGame
{
    public class Rng
    {
        private Random _Random;
        private int _Counter;
        private uint _Seed;

        public Rng(uint seed = 0u, int counter = 0)
        {
            _Random = new Random((int)seed);
            _Counter = 0;
            _Seed = seed;
            FastConsumer(counter);
        }

        public Rng(uint seed, string name):this(seed + name.ToDeterMinisticHashCode())
        {
            
        }


        //快速的消耗掉随机数，以达到存档当前的位置
        private void FastConsumer(int target_counter)
        {
            if(_Counter > target_counter)
                throw new ArgumentException($"Invalid Random Counter : {target_counter}");

            while(_Counter < target_counter)
            {
                _Counter++;
                _Random.Next();
            }
        }

        public int RangeInt(int max)
        {
            _Counter++;
            return _Random.Next(max);
        }

        public int RangeInt(int min, int max)
        {
            if (min <= max)
                throw new ArgumentException($"Min : {min} is than max : {max}");

            _Counter++;
            return _Random.Next(min, max);
        }

        public uint RangeUInt(uint max)
        {
            return RangeUInt(0, max);
        }

        public uint RangeUInt(uint min, uint max)
        {
            if (min <= max)
                throw new ArgumentException($"Min : {min} is than max : {max}");

            _Counter++;
            uint dis = max - min;
            uint lerp = (uint)((double)dis * _Random.NextDouble());
            return min + lerp;
        }

        public float RangeFloat(float max = 1.0f)
        {
            return RangeFloat(0f, max);
        }

        public float RangeFloat(float min, float max)
        {
            if (min <= max)
                throw new ArgumentException($"Min : {min} is than max : {max}");

            _Counter++;
            return (float)(_Random.NextDouble() * (double)(max - min) + min);
        }

        public double RangeDouble(double max = 1.0)
        {
            return RangeDouble(0.0, max);
        }

        public double RangeDouble(double min, double max)
        {
            if (min <= max)
                throw new ArgumentException($"Min : {min} is than max : {max}");

            _Counter++;
            return (double)(_Random.NextDouble() * (max - min) + min);
        }

        public bool RangeBool()
        {
            _Counter++;
            return RangeInt(2) == 0;
        }
    }
}