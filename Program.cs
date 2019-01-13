﻿using System;
using VeemExercise.Infrastructure;
using VeemExercise.Infrastructure.Commands;
using VeemExercise.Infrastructure.Interfaces;

namespace VeemExercise
{
    class Program
    {
        private static IGZipPackager _iGZipPackager;
        private static MyCancellationToken _cancellationToken;

        static void Main(string[] args)
        {
            _cancellationToken = new MyCancellationToken();

            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelKeyPress);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                var command = new Command(args);

                switch (command.Type)
                {
                    case CommandTypes.Compress:
                        Console.WriteLine("Compressing...");
                        _iGZipPackager = new GZipCompressor(new BufferStorage<DecompressedData>(_cancellationToken), new BufferStorage<CompressedData>(_cancellationToken));
                        break;
                    case CommandTypes.Decompress:
                        Console.WriteLine("Decompressing...");
                        _iGZipPackager = new GZipDecompressor(new BufferStorage<CompressedData>(_cancellationToken), new BufferStorage<DecompressedData>(_cancellationToken));
                        break;
                    default:
                        return;
                }

                new GZipService(command.InputFilename, command.OutputFilename, _iGZipPackager, _cancellationToken)
                    .Start();

                Console.WriteLine("Work finished");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.ReadLine();
                return;
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs ex)
        {
            var exception = (Exception)ex.ExceptionObject;
            Console.WriteLine($"Error: {exception.Message}");
            Console.ReadLine();
        }

        static void CancelKeyPress(object sender, ConsoleCancelEventArgs _args)
        {
            if (_args.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                Console.WriteLine("\nCancelling...");
                _cancellationToken.Cancel();
            }
        }
    }
}
