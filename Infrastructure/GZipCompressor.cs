using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using VeemExercise.Infrastructure.Interfaces;

namespace VeemExercise.Infrastructure
{
    class GZipCompressor : IGZipPackager
    {
        private IBufferStorage<DecompressedData> _inputStorage;
        private IBufferStorage<CompressedData> _outputStorage;
        private static int compressorThreadsCount;
        private const int _blockSize = 1024 * 1024;

        public GZipCompressor(IBufferStorage<DecompressedData> inputStorage, IBufferStorage<CompressedData> outputStorage)
        {
            _inputStorage = inputStorage;
            _outputStorage = outputStorage;
        }

        public void Process(MyCancellationToken cancellationToken)
        {
            Interlocked.Increment(ref compressorThreadsCount);

            while (!cancellationToken.IsCancelled)
            {
                var _block = _inputStorage.ReadNext();

                if (_block == null)
                {
                    Interlocked.Decrement(ref compressorThreadsCount);
                    if (compressorThreadsCount <= 0)
                    {
                        _outputStorage.Close();
                    }
                    return;
                }

                using (var memoryStream = new MemoryStream())
                {
                    using (var gz = new GZipStream(memoryStream, CompressionMode.Compress))
                    {
                        gz.Write(_block.Buffer, 0, _block.Buffer.Length);
                    }

                    var compressedData = memoryStream.ToArray();
                    _outputStorage.Add(new CompressedData { Id = _block.Id.Value, Buffer = compressedData });
                }
            }
        }

        public void Read(string fileName, MyCancellationToken cancellationToken)
        {
            using (var _readFile = new FileStream(fileName, FileMode.Open))
            {
                int bytesRead;
                byte[] lastBuffer;

                while (_readFile.Position < _readFile.Length && !cancellationToken.IsCancelled)
                {
                    if (_readFile.Length - _readFile.Position <= _blockSize)
                    {
                        bytesRead = (int)(_readFile.Length - _readFile.Position);
                    }
                    else
                    {
                        bytesRead = _blockSize;
                    }

                    lastBuffer = new byte[bytesRead];
                    _readFile.Read(lastBuffer, 0, bytesRead);
                    _inputStorage.Add(new DecompressedData { Buffer = lastBuffer });
                }
                _inputStorage.Close();
            }
        }

        public void Write(string fileName, MyCancellationToken cancellationToken)
        {
            using (var fileStream = new FileStream($"{fileName}", FileMode.Create))
            {
                while (!cancellationToken.IsCancelled)
                {
                    var _block = _outputStorage.ReadNext();
                    if (_block == null)
                    {
                        return;
                    }

                    Console.WriteLine(_block.Buffer.Length);
                    BitConverter.GetBytes(_block.Buffer.Length).CopyTo(_block.Buffer, 4);
                    fileStream.Write(_block.Buffer, 0, _block.Buffer.Length);
                }
            }
        }
    }
}
