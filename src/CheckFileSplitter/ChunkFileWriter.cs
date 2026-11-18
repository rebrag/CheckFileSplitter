using System.Collections.Generic;
using System.IO;

namespace CheckFileSplitter
{
    /// <summary>
    /// Writes one output file: the header line, then each record.
    /// </summary>
    public static class ChunkFileWriter
    {
        /// <summary>
        /// Every line, including the very last one, ends with CRLF.
        /// StreamWriter.WriteLine uses Environment.NewLine by default, which is "\r\n" only on Windows,
        /// so it is set explicitly to give the same output on any machine.
        /// </summary>
        public const string LineEnding = "\r\n";

        public static void Write(string path, string headerLine, IEnumerable<TrustDisbursement> records)
        {
            using (var writer = new StreamWriter(path))
            {
                writer.NewLine = LineEnding;
                writer.WriteLine(headerLine);

                foreach (TrustDisbursement record in records)
                    writer.WriteLine(record.ToPipeLine());
            }
        }
    }
}
