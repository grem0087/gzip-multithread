using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VeemExercise.Infrastructure.Interfaces;

namespace VeemExercise.Infrastructure
{
    public class BufferStorage<T> : IBufferStorage<T> where T : IObjectWithId
    {
        private object locker = new object();
        private Dictionary<int, T> _storage = new Dictionary<int, T>();
        private bool _isInputOpen = true;
        private volatile int writeBlockId = 0;
        private volatile int readBlockId = 0;
        private MyCancellationToken _cancellationToken;

        public BufferStorage(MyCancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _cancellationToken.OnCancel += _cancellationToken_OnCancel;
        }

        public void Add(T buffer)
        {
            lock (locker)
            {
                if (!buffer.Id.HasValue)
                {
                    buffer.Id = writeBlockId;
                    _storage.Add(writeBlockId, buffer);
                    Interlocked.Increment(ref writeBlockId);
                }
                else
                {
                    _storage.Add(buffer.Id.Value, buffer);
                    if (writeBlockId < buffer.Id)
                    {
                        writeBlockId = buffer.Id.Value;
                    }
                }

                Monitor.PulseAll(locker);
            }
        }

        public T ReadNext()
        {
            lock (locker)
            {
                T result;

                if (_storage.Count() == 0 && !_isInputOpen)
                {
                    return default(T);
                }

                while (!_storage.ContainsKey(readBlockId) && _isInputOpen)
                {
                    Monitor.Wait(locker);
                }
                result = _storage[readBlockId];
                _storage.Remove(readBlockId);
                Interlocked.Increment(ref readBlockId);
                Monitor.PulseAll(locker);
                return result;
            }
        }

        public void Close()
        {
            lock (locker)
            {
                _isInputOpen = false;
                Monitor.PulseAll(locker);
            }
        }

        private void _cancellationToken_OnCancel()
        {
            Close();
        }
    }
}
