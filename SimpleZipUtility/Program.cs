/*
 Author: Kilovatiy Maksim
 
 Algorithm Description:
 * Упаковка файла.
 * 1. В первом потоке исходный файл начитывается сегментами по 3МБ
 * 2. Каждый сегмент попадает в очередь
 * 3. Во втором потоке из очереди читаются сегменты упаковываются и заливаются в архив.
      У GzipStream ограничение в 4ГБ для .NET 3.
 * Распаковка файла.
 * 1. В первом потоке архив читается порциями по 3МБ
 * 2. Алгоритм пытается найти начало и конец архивированного сегмента,
 *    как только это происходит сегмент с архивом попадает в очередь.
 * 3. Второй поток обращается к очереди, распаковывает сегменты и записывает в файл.

*/

using System;
using System.IO;
using System.Threading;
using SimpleZipUtility.Interfaces;

namespace SimpleZipUtility
{
    internal class Program
    {
        private const string COMPRESS = "compress";
        private const string DECOMPRESS = "decompress";

        private const int STREAM_BUFFER_SIZE = 2 << 18; //512Kb
        private const int ARCHIVE_SIZE = 3 * 1024 * 1024; //3Mb

        private static AutoResetEvent _mainEvent = new AutoResetEvent(false);

        private static volatile bool _isCompleted = false;
        private static volatile bool _isCancelled = false;
        private static volatile bool _isError = false;

        private static Thread _backgroundThread;
        private static Thread _ss;
        
        static void Main(string[] args)
        {
            int capaticy = 50;

            AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine();
                _isCancelled = true;
                if (_backgroundThread != null)
                    _backgroundThread.Abort();
            };

            if (ValidateArguments(args))
            {
                bool compress = string.Equals(args[0], COMPRESS);
                string initPath = args[1];
                string archPath = args[2];

                IArchivator compressor = new CompressEngine(new GZipCompress(), capaticy);
                IArchivator decompressor = new DecompressEngine(new GZipDecompress(), capaticy);

                _ss = new Thread(() => ScreenSaver()) { IsBackground = true };

                Console.WriteLine("Operation Started");
                _ss.Start();

                if (string.Equals(args[0], COMPRESS))
                {
                    _backgroundThread = new Thread(() => Compress(initPath, archPath, compressor, ARCHIVE_SIZE)) { IsBackground = true };
                    _backgroundThread.Start();
                }
                else if (string.Equals(args[0], DECOMPRESS))
                {
                    _backgroundThread = new Thread(() => DeCompress(initPath, archPath, decompressor, ARCHIVE_SIZE)) { IsBackground = true };
                    _backgroundThread.Start();
                }

                _mainEvent.WaitOne();

                Console.WriteLine();

                if (_isCompleted)
                    Console.WriteLine("0");
                else if (_isCancelled)
                    Console.WriteLine("Cancelled");
                else if (_isError)
                    Console.WriteLine("1");

                Console.WriteLine();
                Console.WriteLine("Press any key");
                Console.ReadLine();
            }
        }

        private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Unexpected exception \r\n {0}", e.ExceptionObject == null ? "" : e.ExceptionObject.ToString());
        }

        static void ScreenSaver()
        {
            while (!_isCompleted)
            {
                if (_isCancelled || _isError)
                    break;
                Console.Write(".");
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        static void Compress(string initPath, string archPath, IArchivator e, int sizeMb)
        {
            try
            {
                using (var fs = new FileStream(initPath, FileMode.Open, FileAccess.Read,
                    FileShare.None, STREAM_BUFFER_SIZE))
                {
                    using (var cs = new FileStream(archPath, FileMode.Create, FileAccess.Write,
                    FileShare.None, STREAM_BUFFER_SIZE, FileOptions.Asynchronous))
                    {
                        _isCompleted = e.DoAction(fs, cs, sizeMb);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                _isError = true;
            }
            catch (Exception)
            {
                _isError = true;
            }
            finally
            {
                _mainEvent.Set();
            }
        }

        static void DeCompress(string archPath, string initPath, IArchivator e, int sizeMb)
        {
            try
            {
                using (var cs = new FileStream(archPath, FileMode.Open, FileAccess.Read,
                    FileShare.None, STREAM_BUFFER_SIZE, FileOptions.Asynchronous))
                {
                    using (var fs = new FileStream(initPath, FileMode.Create, FileAccess.Write,
                    FileShare.None, STREAM_BUFFER_SIZE, FileOptions.Asynchronous))
                    {
                        _isCompleted = e.DoAction(cs, fs, sizeMb);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                _isError = true;
            }
            catch (Exception)
            {
                _isError = true;
            }
            finally
            {
                _mainEvent.Set();
            }
        }

        static bool ValidateArguments(string[] args)
        {
            if (args == null)
            {
                Console.WriteLine("No arguments provided");
                return false;
            }
            if (args.Length < 3 || args.Length > 3)
            {
                Console.WriteLine("Incorrect number of arguments");
                return false;
            }
            if (!(string.Equals(args[0], COMPRESS) || string.Equals(args[0], DECOMPRESS)))
            {
                Console.WriteLine("Incorrect action");
                return false;
            }

            return true;
        }
    }
}

