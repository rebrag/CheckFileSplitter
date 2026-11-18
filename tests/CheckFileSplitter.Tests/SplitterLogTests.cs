using System;
using System.IO;
using Xunit;

namespace CheckFileSplitter.Tests
{
    public class SplitterLogTests
    {
        [Fact]
        public void LogFileIsNamedForTheDate()
        {
            Assert.Equal("Splitter_Log_20261118.log", SplitterLog.FileNameFor(new DateTime(2026, 11, 18, 15, 2, 0)));
            Assert.Equal("Splitter_Log_20270105.log", SplitterLog.FileNameFor(new DateTime(2027, 1, 5)));
        }

        [Fact]
        public void TeeWriterSendsEverythingToBothWriters()
        {
            var a = new StringWriter();
            var b = new StringWriter();
            var tee = new TeeTextWriter(a, b);

            tee.WriteLine("Read 7697 records.");
            tee.Write('x');
            tee.WriteLine($"{3} files");

            Assert.Equal(a.ToString(), b.ToString());
            Assert.Equal("Read 7697 records." + Environment.NewLine + "x3 files" + Environment.NewLine, b.ToString());
        }

        [Fact]
        public void ConsoleOutputAndErrorsGoToTheLogAndRunsAreAppended()
        {
            string folder = Path.Combine(Path.GetTempPath(), "SplitterLogTest_" + Guid.NewGuid().ToString("N"));
            var day = new DateTime(2026, 11, 18);
            TextWriter originalOut = Console.Out;

            using (var log = new SplitterLog(folder, day))
            {
                Console.WriteLine("first run");
                Console.Error.WriteLine("Error: something bad");
            }
            using (new SplitterLog(folder, day))
            {
                Console.WriteLine("second run");
            }

            string text = File.ReadAllText(Path.Combine(folder, "Splitter_Log_20261118.log"));
            Assert.Contains("first run", text);
            Assert.Contains("Error: something bad", text);
            Assert.Contains("second run", text);
            Assert.True(text.IndexOf("first run") < text.IndexOf("second run"));
            Assert.Same(originalOut, Console.Out);   // console is put back afterwards

            Directory.Delete(folder, true);
        }
    }
}
