using System;
using System.Threading;

namespace MiniGame.Network
{
    public class BaseRingBuffer<T>
    {
        private readonly T[] _buffer;
        private long _head;
        private long _tail;
        private long _count;
        private readonly bool _allowOverwrite;

        // 统计数据
        private long _totalBytesWritten;
        private long _totalBytesRead;

        private readonly object _syncLock = new(); // 锁对象，用于确保线程安全

        public BaseRingBuffer(int capacity, bool allowOverwrite = false)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be greater than zero.", nameof(capacity));

            _buffer = new T[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
            _allowOverwrite = allowOverwrite;
        }

        // 获取缓冲区中可用的数据长度
        public long Count => Interlocked.Read(ref _count);

        public long Head
        {
            get => Interlocked.Read(ref _head);

            set
            {
                lock (_syncLock)
                {
                    if (value < 0 || value >= Capacity)
                    {
                        throw new ArgumentOutOfRangeException();
                    }

                    _head = value;
                    if (_head == _tail)
                    {
                        _tail = (_tail + 1) % Capacity;
                    }

                    //update _count
                    if (_tail >= _head)
                    {
                        _count = _tail - _head;
                    }
                    else
                    {
                        _count = Capacity - _head + _tail;
                    }
                }
            }
        }

        public long FreeSpace
        {
            get
            {
                lock (_syncLock)
                {
                    return Capacity - _count;
                }
            }
        }

        public long Tail => Interlocked.Read(ref _tail);

        // 获取缓冲区容量
        public int Capacity => _buffer.Length;

        // 获取总写入字节数
        public long TotalBytesWritten => Interlocked.Read(ref _totalBytesWritten);

        // 获取总读取字节数
        public long TotalBytesRead => Interlocked.Read(ref _totalBytesRead);

        // 往缓冲区写入字节数组
        public void Write(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            Write(data, 0, data.Length);
        }

        public void Write(byte[] data, int offset, int length)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0 || length < 0 || offset + length > data.Length)
                throw new ArgumentOutOfRangeException();

            lock (_syncLock)
            {
                if (length > Capacity - _count)
                {
                    if (_allowOverwrite)
                    {
                        var overwriteBytes = length - (Capacity - _count);
                        _head = (_head + overwriteBytes) % Capacity;
                        _count = Math.Min(_count + length, Capacity); // 动态更新 _count
                    }
                    else
                    {
                        throw new InvalidOperationException("Buffer overflow");
                    }
                }
                else
                {
                    _count += length;
                }

                var bytesToEnd = Capacity - _tail;
                if (length <= bytesToEnd)
                {
                    Array.Copy(data, offset, _buffer, _tail, length);
                }
                else
                {
                    Array.Copy(data, offset, _buffer, _tail, bytesToEnd);
                    Array.Copy(data, offset + bytesToEnd, _buffer, 0, length - bytesToEnd);
                }

                _tail = (_tail + length) % Capacity;
                Interlocked.Add(ref _totalBytesWritten, length);
            }
        }

        // 从缓冲区读取字节到目标数组
        private long Read(byte[] destination, int offset, long length)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (offset < 0 || length < 0 || offset + length > destination.Length)
                throw new ArgumentOutOfRangeException();

            lock (_syncLock)
            {
                if (_count == 0)
                    return 0;

                var bytesToRead = Math.Min(length, _count);
                var bytesToEnd = Capacity - _head;

                if (bytesToRead <= bytesToEnd)
                {
                    Array.Copy(_buffer, _head, destination, offset, bytesToRead);
                }
                else
                {
                    Array.Copy(_buffer, _head, destination, offset, bytesToEnd);
                    Array.Copy(_buffer, 0, destination, offset + bytesToEnd, bytesToRead - bytesToEnd);
                }

                _head = (_head + bytesToRead) % Capacity;
                _count -= bytesToRead;
                Interlocked.Add(ref _totalBytesRead, bytesToRead);
                return bytesToRead;
            }
        }

        // 读取指定长度的数据
        public byte[] Read(int count)
        {
            if (count < 0 || count > Count)
                throw new ArgumentOutOfRangeException(nameof(count));

            var result = new byte[count];
            Read(result, 0, count);
            return result;
        }

        // 读取所有可用数据
        public byte[] ReadAllAvailable()
        {
            if (Count == 0) // 如果没有可读数据，提前返回
            {
                return Array.Empty<byte>();
            }

            var result = new byte[Count];
            Read(result, 0, Count);
            return result;
        }

        public void Clear()
        {
            lock (_syncLock)
            {
                _head = 0;
                _tail = 0;
                _count = 0;
                Array.Clear(_buffer, 0, _buffer.Length); // 清空缓冲区内容
            }
        }

        //获取当前缓冲区的状态和数据的debug info
        public virtual string GetDebugInfo()
        {
            var info = "";
            lock (_syncLock)
            {
                info +=
                    $"Capacity: {Capacity}, Count: {Count}, Head: {_head}, Tail: {_tail}, TotalBytesWritten: {TotalBytesWritten}, TotalBytesRead: {TotalBytesRead}\n";
                if (_buffer is byte[] data)
                {
                    info += $"Data: {BitConverter.ToString(data)}\n";
                }
                else
                {
                    foreach (var item in _buffer)
                    {
                        info += item + ",";
                    }
                }
            }

            return info;
        }
    }
}