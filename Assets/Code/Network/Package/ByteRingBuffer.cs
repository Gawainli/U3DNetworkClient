using System;

namespace MiniGame.Network
{
    public class ByteRingBuffer : BaseRingBuffer<byte>
    {
        public ByteRingBuffer(int capacity, bool allowOverwrite = true) : base(capacity, allowOverwrite)
        {
        }
        
        public void WriteInt32(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            Write(bytes);
        }
        
        public int ReadInt32()
        {
            var bytes = Read(4);
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}