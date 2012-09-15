using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeveConnecteuze.Network
{
    public class DeveOutgoingMessage
    {
        private byte[] bytes = new byte[4]; //First for bytes for length in int

        public DeveOutgoingMessage()
        {
            WriteBytes(new byte[1] { (byte)DeveMessageType.Data });
        }

        public DeveOutgoingMessage(DeveMessageType messageType)
        {
            WriteBytes(new byte[1] { (byte)messageType });
        }

        private void IncreaseLengthByteArray(int length)
        {
            if (bytes == null)
            {
                bytes = new byte[length];
            }
            else
            {
                Array.Resize<Byte>(ref bytes, bytes.Length + length);
            }
        }

        public void WriteBytes(byte[] b)
        {
            IncreaseLengthByteArray(b.Length);
            int startPos = bytes.Length - b.Length;
            int current = 0;
            for (int i = startPos; i < bytes.Length; i++)
            {
                bytes[i] = b[current];
                current++;
            }
        }

        public void WriteString(String value)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            byte[] b = encoding.GetBytes(value);

            WriteInt32(b.Length); //Write the String length
         
            WriteBytes(b);
        }

        public void WriteInt32(Int32 value)
        {
            //IncreaseLengthByteArray(4);
            byte[] b = BitConverter.GetBytes(value);
            WriteBytes(b);
        }

        internal byte[] GetBytes()
        {
            byte[] lengthInBytes = BitConverter.GetBytes(bytes.Length - 4);

            bytes[0] = lengthInBytes[0];
            bytes[1] = lengthInBytes[1];
            bytes[2] = lengthInBytes[2];
            bytes[3] = lengthInBytes[3];

            return bytes;
        }
    }
}
