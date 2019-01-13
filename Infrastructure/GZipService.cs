﻿using System;
using System.Collections.Generic;
using System.Threading;
using VeemExercise.Infrastructure.Interfaces;

namespace VeemExercise.Infrastructure
{
    class GZipService
    {
        private readonly string _fileInput;
        private readonly string _fileOutput;
        private IGZipPackager _packager;
        private static int _maxThreads = Environment.ProcessorCount;
        private List<Thread> _threads = new List<Thread>();
        private MyCancellationToken _cancellationToken;

        public GZipService(string fileInput, string fileOutput, IGZipPackager packager, MyCancellationToken cancellationToken)
        {
            _fileInput = fileInput;
            _fileOutput = fileOutput;
            _packager = packager;
            _cancellationToken = cancellationToken;
        }

        public void Start()
        {
            var readerThread = new Thread(x => ThreadCaller(() => _packager.Read(_fileInput, _cancellationToken)));
            readerThread.Start();
            _threads.Add(readerThread);

            for (int i = 0; i < _maxThreads; i++)
            {
                var cs = new Thread(x => ThreadCaller(() => _packager.Process(_cancellationToken)));
                cs.Start();
                _threads.Add(cs);
            }

            var writeThread = new Thread(x => ThreadCaller(() => _packager.Write(_fileOutput, _cancellationToken)));
            writeThread.Start();
            _threads.Add(writeThread);

            foreach (var thread in _threads)
            {
                thread.Join();
            }
        }

        private void ThreadCaller(Action method)
        {
            try
            {
                method?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                _cancellationToken.Cancel();
            }
        }
    }
}
