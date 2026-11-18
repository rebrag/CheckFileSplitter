using System.IO;
using System.Text;

namespace CheckFileSplitter
{
    /// <summary>
    /// A TextWriter that sends everything to two writers, for example the console and a log file.
    /// Used with Console.SetOut / Console.SetError so every Console.WriteLine also lands in the log.
    /// </summary>
    public class TeeTextWriter : TextWriter
    {
        private readonly TextWriter _first;
        private readonly TextWriter _second;

        public TeeTextWriter(TextWriter first, TextWriter second)
        {
            _first = first;
            _second = second;
        }

        public override Encoding Encoding
        {
            get { return _first.Encoding; }
        }

        public override void Write(char value)
        {
            _first.Write(value);
            _second.Write(value);
        }

        public override void Write(string value)
        {
            _first.Write(value);
            _second.Write(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            _first.Write(buffer, index, count);
            _second.Write(buffer, index, count);
        }

        public override void Flush()
        {
            _first.Flush();
            _second.Flush();
        }
    }
}
