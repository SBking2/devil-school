
using System;
using System.Buffers.Binary;
using Godot;

namespace EGame
{
    public class PacketWriter
    {
        private readonly byte[] _TempBuffer = new byte[16];  //当存入一个数据的时候，先把这个数据序列化存入这个buffer才方便统一进行字节拷贝

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
            BitSerializationUtils.WriteBytes(_TempBuffer, _Buffer, _BitPosition, 1);
            _BitPosition += 1;
        }

        public void WirteInt(int val, int bits = 32)
        {
            EnsureBufferCapacity(bits);
            BinaryPrimitives.WriteInt32LittleEndian(_TempBuffer.AsSpan(), val);
            BitSerializationUtils.WriteBytes(_TempBuffer, _Buffer, _BitPosition, bits);
            _BitPosition += bits;
        }

        public void WriteUInt(uint val, int bits = 32)
        {
            EnsureBufferCapacity(bits);
            BinaryPrimitives.WriteUInt32LittleEndian(_TempBuffer.AsSpan(), val);
            BitSerializationUtils.WriteBytes(_TempBuffer, _Buffer, _BitPosition, bits);
            _BitPosition += bits;
        }

        public void WriteShort(short val, int bits = 16)
        {
            EnsureBufferCapacity(bits);
            BinaryPrimitives.WriteInt16LittleEndian(_TempBuffer.AsSpan(), val);
            BitSerializationUtils.WriteBytes(_TempBuffer, _Buffer, _BitPosition, bits);
            _BitPosition += bits;
        }

        public void WriteUShort(ushort val, int bits = 16)
        {
            EnsureBufferCapacity(bits);
            BinaryPrimitives.WriteUInt16LittleEndian(_TempBuffer.AsSpan(), val);
            BitSerializationUtils.WriteBytes(_TempBuffer, _Buffer, _BitPosition, bits);
            _BitPosition += bits;
        }

        public void WriteLong(long val, int bits = 64)
        {
            EnsureBufferCapacity(bits);
            BinaryPrimitives.WriteInt64LittleEndian(_TempBuffer.AsSpan(), val);
            BitSerializationUtils.WriteBytes(_TempBuffer, _Buffer, _BitPosition, bits);
            _BitPosition += bits;
        }

        public void WriteULong(ulong val, int bits = 64)
        {
            EnsureBufferCapacity(bits);
            BinaryPrimitives.WriteUInt64LittleEndian(_TempBuffer.AsSpan(), val);
            BitSerializationUtils.WriteBytes(_TempBuffer, _Buffer, _BitPosition, bits);
            _BitPosition += bits;
        }

        public void WriteFloat(float val, QuantizeParam? quantize)
        {
            if(quantize != null)
            {
                uint v = Quantize(val, quantize.Value.Min, quantize.Value.Max, quantize.Value.Bits);
                WriteUInt(v, quantize.Value.Bits);
            }
            else
            {
                EnsureBufferCapacity(32);
                BinaryPrimitives.WriteSingleLittleEndian(_TempBuffer.AsSpan(), val);
                BitSerializationUtils.WriteBytes(_TempBuffer, _Buffer, _BitPosition, 32);
                _BitPosition += 32;
            }
        }

        public void WriteDouble(double val, int bits = 64)
        {
            EnsureBufferCapacity(bits);
            BinaryPrimitives.WriteDoubleLittleEndian(_TempBuffer.AsSpan(), val);
            BitSerializationUtils.WriteBytes(_TempBuffer, _Buffer, _BitPosition, bits);
            _BitPosition += bits;
        }

        //把float映射到区间为0 - Pow(2, bits)的整数上
        public static uint Quantize(float val, float min, float max, int bits)
        {
            return (uint)((double)((val - min) / (max - min)) * Mathf.Pow(2, bits));
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