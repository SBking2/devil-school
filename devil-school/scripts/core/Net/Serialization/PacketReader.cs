
using System;
using System.Buffers.Binary;
using Godot;

namespace EGame
{
    public class PacketReader
    {
        private readonly byte[] _TempBuffer = new byte[16];
        private byte[] _Buffer;
        private int _BitPosition;

        public PacketReader(byte[] buffer)
        {
            Reset(buffer);
        }

        public void Reset(byte[] buffer)
        {
            _Buffer = buffer;
            _BitPosition = 0;
        }

        public bool ReadBool()
        {
            Array.Clear(_TempBuffer);
            BitSerializationUtils.ReadBits(_Buffer, _TempBuffer, _BitPosition, 1);
            _BitPosition += 1;
            return _TempBuffer[0] != 0;
        }
        
        public int ReadInt(int bits = 32)
        {
            Array.Clear(_TempBuffer);
            BitSerializationUtils.ReadBits(_Buffer, _TempBuffer, _BitPosition, bits);
            _BitPosition += bits;
            return BinaryPrimitives.ReadInt32LittleEndian(_TempBuffer.AsSpan());
        }

        public uint ReadUInt(int bits = 32)
        {
            Array.Clear(_TempBuffer);
            BitSerializationUtils.ReadBits(_Buffer, _TempBuffer, _BitPosition, bits);
            _BitPosition += bits;
            return BinaryPrimitives.ReadUInt32LittleEndian(_TempBuffer.AsSpan());
        }

        public short ReadShort(int bits = 16)
        {
            Array.Clear(_TempBuffer);
            BitSerializationUtils.ReadBits(_Buffer, _TempBuffer, _BitPosition, bits);
            _BitPosition += bits;
            return BinaryPrimitives.ReadInt16LittleEndian(_TempBuffer.AsSpan());
        }

        public ushort WriteUShort(int bits = 16)
        {
            Array.Clear(_TempBuffer);
            BitSerializationUtils.ReadBits(_Buffer, _TempBuffer, _BitPosition, bits);
            _BitPosition += bits;
            return BinaryPrimitives.ReadUInt16LittleEndian(_TempBuffer.AsSpan());
        }

        public long WriteLong(int bits = 64)
        {
            Array.Clear(_TempBuffer);
            BitSerializationUtils.ReadBits(_Buffer, _TempBuffer, _BitPosition, bits);
            _BitPosition += bits;
            return BinaryPrimitives.ReadInt64LittleEndian(_TempBuffer.AsSpan());
        }

        public ulong WriteULong(int bits = 64)
        {
            Array.Clear(_TempBuffer);
            BitSerializationUtils.ReadBits(_Buffer, _TempBuffer, _BitPosition, bits);
            _BitPosition += bits;
            return BinaryPrimitives.ReadUInt64LittleEndian(_TempBuffer.AsSpan());
        }

        public float ReadFloat(QuantizeParam? quantize)
        {
            if (quantize != null)
            {
                uint split = ReadUInt();
                return (float)(((double)(quantize.Value.Max - quantize.Value.Min)) * ((double)split / Mathf.Pow(2, quantize.Value.Bits))) + quantize.Value.Min;
            }
            else
            {
                Array.Clear(_TempBuffer);
                BitSerializationUtils.ReadBits(_Buffer, _TempBuffer, _BitPosition, 32);
                _BitPosition += 32;
                return BinaryPrimitives.ReadSingleLittleEndian(_TempBuffer.AsSpan());
            }
        }

        public double ReadDouble(int bits = 64)
        {
            Array.Clear(_TempBuffer);
            BitSerializationUtils.ReadBits(_Buffer, _TempBuffer, _BitPosition, bits);
            _BitPosition += bits;
            return BinaryPrimitives.ReadDoubleLittleEndian(_TempBuffer.AsSpan());
        }
    }
}