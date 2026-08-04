

using System;
using Godot;

namespace EGame
{
    public static class BitSerializationUtils
    {
        //bit级别的Copy
        public static void WriteBytes(byte[] source_buffer, byte[] destination_buffer, int d_start_pos, int copy_length, int s_start_pos = 0)
        {
            int des_length = destination_buffer.Length;
            int source_length = source_buffer.Length;

            int expected_des_length = Mathf.CeilToInt((float)d_start_pos + copy_length / 8f);
            int expected_source_length = Mathf.CeilToInt((float)s_start_pos + copy_length / 8f);

            if (expected_des_length > des_length)
                throw new InvalidOperationException($"Desitination doesn't have enough capacity! Length : {des_length} Expected : {expected_des_length}");

            if (expected_source_length > source_length)
                throw new InvalidOperationException($"Source doesn't have enough capacity! Length : {source_length} Expected : {expected_source_length}");
        }
    }
}