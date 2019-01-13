using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using VeemExercise.Infrastructure.Common;
using VeemExercise.Infrastructure.DataContainers;
using VeemExercise.Infrastructure.Interfaces;

namespace VeemExercise.Infrastructure
{
    class GZipDecompressor : IGZipPackager
    {
        private static int decompressorThreadsCount;
        private IBufferStorage<CompressedData> _inputStorage;
        private IBufferStorage<DecompressedData> _outputStorage;

        public GZipDecompressor(IBufferStorage<CompressedData> inputStorage, IBufferStorage<DecompressedData> outputStorage)
        {
            _inputStorage = inputStorage;
            _outputStorage = outputStorage;
        }

        public void Read(string fileName, MyCancellationToken cancellationToken)
        {
            using (var compressedFile = new FileStream(fileName, FileMode.Open))
            {
                while (compressedFile.Position < compressedFile.Length && !cancellationToken.IsCancelled)
                {
                    var lengthBuffer = new byte[8];
                    compressedFile.Read(lengthBuffer, 0, lengthBuffer.Length);
                    var blockLength = BitConverter.ToInt32(lengthBuffer, 4);
                    var compressedData = new byte[blockLength];
                    lengthBuffer.CopyTo(compressedData, 0);

                    compressedFile.Read(compressedData, 8, blockLength - 8);

                    int dataSize = BitConverter.ToInt32(compressedData, blockLength - 4);
                    byte[] lastBuffer = new byte[dataSize];
                    var _block = new CompressedData { Buffer = compressedData, UncompressedSize = dataSize };
                    ConsoleIndicator.ShowProgress(compressedFile.Position, compressedFile.Length);
                    _inputStorage.Add(_block);
                }

                _inputStorage.Close();
            }
        }

        public void Process(MyCancellationToken cancellationToken)
        {
            Interlocked.Increment(ref decompressorThreadsCount);

            while (!cancellationToken.IsCancelled)
            {
                var block = _inputStorage.ReadNext();

                if (block == null)
                {
                    Interlocked.Decrement(ref decompressorThreadsCount);
                    if (decompressorThreadsCount <= 0)
                    {
                        _outputStorage.Close();
                    }
                    return;
                }

                using (var source = new MemoryStream(block.Buffer))
                {
                    using (var decompressionStream = new GZipStream(source,
                        CompressionMode.Decompress))
                    {
                        byte[] newBuffer = new byte[block.UncompressedSize];

                        decompressionStream.Read(newBuffer, 0, newBuffer.Length);
                        var decompressedData = block.Buffer.ToArray();
                        _outputStorage.Add(new DecompressedData { Id = block.Id, Buffer = newBuffer });
                    }
                }
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

                    fileStream.Write(block.Buffer, 0, block.Buffer.Length);
                }
            }
        }
    }
}
