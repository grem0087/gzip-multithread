using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
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
            try
            {
                using (var _compressedFile = new FileStream(fileName, FileMode.Open))
                {
                    while (_compressedFile.Position < _compressedFile.Length && !cancellationToken.IsCancelled)
                    {
                        var lengthBuffer = new byte[8];
                        _compressedFile.Read(lengthBuffer, 0, lengthBuffer.Length);
                        var blockLength = BitConverter.ToInt32(lengthBuffer, 4);
                        var compressedData = new byte[blockLength];
                        lengthBuffer.CopyTo(compressedData, 0);

                        _compressedFile.Read(compressedData, 8, blockLength - 8);

                        int dataSize = BitConverter.ToInt32(compressedData, blockLength - 4);
                        byte[] lastBuffer = new byte[dataSize];
                        var _block = new CompressedData { Buffer = compressedData, UncompressedSize = dataSize };
                        _inputStorage.Add(_block);
                    }

                    _inputStorage.Close();
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                cancellationToken.Cancel();
            }
        }

        public void Process(MyCancellationToken cancellationToken)
        {
            try
            {
                Interlocked.Increment(ref decompressorThreadsCount);

                while (!cancellationToken.IsCancelled)
                {
                    var _block = _inputStorage.ReadNext();

                    if (_block == null)
                    {
                        Interlocked.Decrement(ref decompressorThreadsCount);
                        if (decompressorThreadsCount <= 0)
                        {
                            _outputStorage.Close();
                        }
                        return;
                    }

                    using (var source = new MemoryStream(_block.Buffer))
                    {
                        using (var decompressionStream = new GZipStream(source,
                            CompressionMode.Decompress))
                        {
                            byte[] newBuffer = new byte[_block.UncompressedSize];

                            decompressionStream.Read(newBuffer, 0, newBuffer.Length);
                            var decompressedData = _block.Buffer.ToArray();
                            _outputStorage.Add(new DecompressedData { Id = _block.Id, Buffer = newBuffer });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GZipDecompressor Error: {Environment.NewLine} {ex.Message}");
                cancellationToken.Cancel();
            }
        }

        public void Write(string fileName, MyCancellationToken cancellationToken)
        {
            try
            {
                using (var fileStream = new FileStream(fileName, FileMode.Create))
                {
                    while (!cancellationToken.IsCancelled)
                    {
                        var _block = _outputStorage.ReadNext();
                        if (_block == null)
                        {
                            return;
                        }

                        Console.WriteLine(_block.Id);
                        fileStream.Write(_block.Buffer, 0, _block.Buffer.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                cancellationToken.Cancel();
            }
        }
    }
}
