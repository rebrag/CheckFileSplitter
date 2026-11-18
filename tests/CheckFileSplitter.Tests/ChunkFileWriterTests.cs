using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace CheckFileSplitter.Tests
{
    public class ChunkFileWriterTests
    {
        static string WriteChunk(string header, params string[] ckNums)
        {
            string path = Path.GetTempFileName();
            var records = ckNums.Select(ck => new TrustDisbursement { CustomAttribute = "R", CkNum = ck }).ToList();
            ChunkFileWriter.Write(path, header, records);
            return File.ReadAllText(path, Encoding.UTF8);
        }

        [Fact]
        public void EveryLineIncludingTheLastEndsWithCrlf()
        {
            string text = WriteChunk("H1|H2", "500101", "500101", "500102");

            Assert.True(text.EndsWith("\r\n"));
            int lineFeeds = text.Count(ch => ch == '\n');
            int crlfs = (text.Length - text.Replace("\r\n", "").Length) / 2;
            Assert.Equal(4, lineFeeds);      // header + 3 records
            Assert.Equal(lineFeeds, crlfs);  // no bare \n anywhere
        }

        [Fact]
        public void HeaderIsTheFirstLineExactly()
        {
            string header = "CustomAttribute|Ecase_ID_CCC|CkNum";
            string text = WriteChunk(header, "500101");

            Assert.True(text.StartsWith(header + "\r\n"));
        }
    }
}
