using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DeveConnecteuze.Network
{
    public static class BitConverterFloat
    {
        public static byte[] GetBytes(float value)
        {
            var floatArray1 = new float[] { value };

            var byteArray = new byte[floatArray1.Length * 4];
            Buffer.BlockCopy(floatArray1, 0, byteArray, 0, byteArray.Length);

            return byteArray;
        }

        public static float ToFloat(byte[] value, int startIndex)
        {
            var floatArray2 = new float[1];
            Buffer.BlockCopy(value, startIndex, floatArray2, 0, 4);

            return floatArray2[0];
        }
    }
}
