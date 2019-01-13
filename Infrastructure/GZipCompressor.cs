using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using VeemExercise.Infrastructure.Common;
using VeemExercise.Infrastructure.DataContainers;
using VeemExercise.Infrastructure.Interfaces;

namespace VeemExercise.Infrastructure
{
    class GZipCompressor : IGZipPackager
    {
        private IBufferStorage<DecompressedData> _inputStorage;
        private IBufferStorage<CompressedData> _outputStorage;
        private static int compressorThreadsCount;
        private const int BlockSize = 1024 * 1024;

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
                var block = _inputStorage.ReadNext();

                if (block == null)
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
                        gz.Write(block.Buffer, 0, block.Buffer.Length);
                    }

                    var compressedData = memoryStream.ToArray();
                    _outputStorage.Add(new CompressedData { Id = block.Id.Value, Buffer = compressedData });
                }
            }
        }

        public void Read(string fileName, MyCancellationToken cancellationToken)
        {
            using (var readFile = new FileStream(fileName, FileMode.Open))
            {
                int bytesRead;

                while (readFile.Position < readFile.Length && !cancellationToken.IsCancelled)
                {
                    if (readFile.Length - readFile.Position <= BlockSize)
                    {
                        bytesRead = (int)(readFile.Length - readFile.Position);
                    }
                    else
                    {
                        bytesRead = BlockSize;
                    }

                    var readBuffer = new byte[bytesRead];
                    readFile.Read(readBuffer, 0, bytesRead);
                    ConsoleIndicator.ShowProgress(readFile.Position, readFile.Length);
                    _inputStorage.Add(new DecompressedData { Buffer = readBuffer });
                }
                _inputStorage.Close();
            }
        }

        public void Write(string fileName, MyCancellationToken cancellationToken)
        {
            using (var fileStream = new FileStream(fileName, FileMode.Create))
            {
                while (!cancellationToken.IsCancelled)
                {
                    var block = _outputStorage.ReadNext();
                    if (block == null)
                    {
                        return;
                    }

                    BitConverter.GetBytes(block.Buffer.Length).CopyTo(block.Buffer, 4);
                    fileStream.Write(block.Buffer, 0, block.Buffer.Length);
                }
            }
        }
    }
}
