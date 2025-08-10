using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ApBox.Core.PacketTracing
{
    public class CircularBuffer<T> : IEnumerable<T>
    {
        private readonly T[] _buffer;
        private readonly ReaderWriterLockSlim _lock = new();
        private int _head;
        private int _tail;
        private int _count;
        
        public CircularBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("Capacity must be positive", nameof(capacity));
            
            _buffer = new T[capacity];
        }
        
        public int Capacity => _buffer.Length;
        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try { return _count; }
                finally { _lock.ExitReadLock(); }
            }
        }
        
        public void Add(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                _buffer[_tail] = item;
                _tail = (_tail + 1) % _buffer.Length;
                
                if (_count < _buffer.Length)
                {
                    _count++;
                }
                else
                {
                    _head = (_head + 1) % _buffer.Length;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _head = 0;
                _tail = 0;
                _count = 0;
                Array.Clear(_buffer, 0, _buffer.Length);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            _lock.EnterReadLock();
            try
            {
                var items = new List<T>(_count);
                var index = _head;
                for (int i = 0; i < _count; i++)
                {
                    items.Add(_buffer[index]);
                    index = (index + 1) % _buffer.Length;
                }
                return items.GetEnumerator();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}