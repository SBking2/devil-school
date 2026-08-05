

using System;
using Godot;

namespace EGame
{
    public static class BitSerializationUtils
    {
        //把低位(右边)当作起点,返回一个连续bits歌1，起点为start_bit的掩码
        public static int GetBitMask(int bits, int start_bit)
        {
            if (bits > 8 || start_bit + bits > 8)
                throw new InvalidOperationException();

            return (1 << bits) - 1 << start_bit;
        }

        //取出buffer中从start_pos开始的bit_length位，返回一个byte（低位对齐）
        public static byte GetBitInByte(byte[] buffer, int start_pos, int bit_length)
        {
            if(bit_length > 8 || start_pos + bit_length > buffer.Length * 8)
                throw new InvalidOperationException();

            int byte_index = start_pos / 8;
            int low_length = start_pos % 8;

            //情况1:开始位置正好是一个字节的结束，因此直接提取下一个字节的低bit_length位即可
            if (low_length == 0)
                return (byte)(buffer[byte_index] & GetBitMask(bit_length, 0));

            //情况2:需要提取的bit在同一个字节内
            int high_lengh = 8 - low_length;
            if (high_lengh >= bit_length)
                return (byte)((buffer[byte_index] >> low_length) & GetBitMask(bit_length, 0));

            //情况3:跨字节读取,上一个字节拼在低位，下一个字节拼在高位
            var byte1 = (byte)((buffer[byte_index] >> low_length) & GetBitMask(high_lengh, 0));
            var byte2 = (byte)((buffer[byte_index + 1] << high_lengh) & GetBitMask(bit_length - high_lengh, high_lengh));
            return (byte)((byte1 & GetBitMask(high_lengh, 0)) | (byte2 & GetBitMask(bit_length - high_lengh, high_lengh)));
        }

        //bit级别的Copy,注意字节内都说从低位开始copy
        public static void WriteBytes(byte[] source_buffer, byte[] destination_buffer, int d_start_pos, int copy_length)
        {
            int write_length;
            //一个一个字节的处理
            for(int i = 0; i < copy_length; i += write_length)
            {
                int index = (d_start_pos + i) / 8;
                int low_length = (d_start_pos + i) % 8;
                write_length = Mathf.Min(copy_length - i, 8 - low_length);

                //获取到了当前字节可写write_length，则开始准备数据
                var data = GetBitInByte(source_buffer, i, write_length);

                //获取原字节中干净的末尾
                var clear_des_byte = (byte)(destination_buffer[index] & GetBitMask(low_length, 0));

                //把获取到的可写bit插入到末尾上
                destination_buffer[index] = (byte)(clear_des_byte | (data << low_length));
            }
        }
       
        //同Write，同样是一个写，一个读
        public static void ReadBits(byte[] source_buffer, byte[] destination_buffer, int s_start_pos, int read_length)
        {
            int can_read_length;
            for(int i = 0; i < read_length; i += can_read_length)
            {
                int index = i / 8;
                int low_length = i % 8;
                can_read_length = Mathf.Min(read_length - i, 8 - low_length);

                var data = GetBitInByte(source_buffer, s_start_pos + i, can_read_length);

                var clear_des_byte = (byte)(destination_buffer[index] & GetBitMask(low_length, 0));
                destination_buffer[index] = (byte)(clear_des_byte | (data << low_length));
            }
        }
    }
}