
using System;
using Godot;

namespace EGame
{
    public class PacketWriter
    {
        private byte[] _TempBuffer = new byte[16];  //当存入一个数据的时候，先把这个数据序列化存入这个buffer才方便统一进行字节拷贝

        private byte[] _Buffer = new byte[64];  //用于存储接收到的完整数

        private int _BitPosition = 0;  //当前读取到的bit位置
        private int BytePosition => (int)((float)_BitPosition / 8f);

        public void Reset()
        {
            _BitPosition = 0;
        }

        public void WriteBool(bool value)
        {
            _TempBuffer[0] = (byte)(value ? 1 : 0);
            EnsureBufferCapacity(1);
        }

        public void WirteInt()
        {

        }

        public void WriteUInt()
        {

        }

        public void WriteFloat()
        {

        }

        public void WriteDouble()
        {

        }

        private void EnsureBufferCapacity(int need_bit_length)
        {
            int current_length = _Buffer.Length;
            int need_min_length = Mathf.CeilToInt((float)(_BitPosition + need_bit_length) / 8f);
            int need_length = _Buffer.Length;

            while (need_length < need_min_length)
                need_length *= 2;

            if (need_length != current_length)
            {
                byte[] new_buffer = new byte[need_length];
                Array.Copy(_Buffer, new_buffer, need_min_length);
                _Buffer = new_buffer;
            }
        }
    }
}