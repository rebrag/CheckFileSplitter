using System;
using System.IO;

namespace CheckFileSplitter
{
    /// <summary>
    /// Copies everything written to the console into Splitter_Log_YYYYMMDD.log.
    /// One log file per day; each run is appended, so earlier runs that day are kept.
    /// Dispose it to stop logging and put the console back the way it was.
    /// </summary>
    public class SplitterLog : IDisposable
    {
        private readonly TextWriter _originalOut;
        private readonly TextWriter _originalError;
        private readonly StreamWriter _logFile;

        public string LogPath { get; private set; }

        public static string FileNameFor(DateTime date)
        {
            return $"Splitter_Log_{date:yyyyMMdd}.log";
        }

        public SplitterLog(string folder, DateTime now)
        {
            Directory.CreateDirectory(folder);
            LogPath = Path.Combine(folder, FileNameFor(now));

            // append: true keeps earlier runs from the same day; AutoFlush so nothing is lost if the program crashes
            _logFile = new StreamWriter(LogPath, append: true) { AutoFlush = true };

            _originalOut = Console.Out;
            _originalError = Console.Error;
            Console.SetOut(new TeeTextWriter(_originalOut, _logFile));
            Console.SetError(new TeeTextWriter(_originalError, _logFile));
        }

        public void Dispose()
        {
            Console.SetOut(_originalOut);
            Console.SetError(_originalError);
            _logFile.Dispose();
        }
    }
}
