using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace CheckFileSplitter.Tests
{
    public class DisbursementSplitterTests
    {
        static TrustDisbursement Row(string ckNum)
        {
            return new TrustDisbursement { CkNum = ckNum };
        }

        // Adds `count` rows for one check number
        static void AddCheck(List<TrustDisbursement> rows, string ckNum, int count)
        {
            for (int i = 0; i < count; i++)
                rows.Add(Row(ckNum));
        }

        [Fact]
        public void CheckThatStartsAtRecord500StaysInTheSameFile()
        {
            var rows = new List<TrustDisbursement>();
            for (int i = 1; i <= 499; i++) AddCheck(rows, (100000 + i).ToString(), 1);
            AddCheck(rows, "200000", 2);   // records 500 and 501
            AddCheck(rows, "200001", 2);

            var chunks = DisbursementSplitter.Split(rows, 500);

            Assert.Equal(2, chunks.Count);
            Assert.Equal(501, chunks[0].Count);
            Assert.Equal("200001", chunks[1][0].CkNum);
        }

        [Fact]
        public void NewFileStartsAtExactly500WhenACheckEndsThere()
        {
            var rows = new List<TrustDisbursement>();
            for (int i = 1; i <= 250; i++) AddCheck(rows, (100000 + i).ToString(), 2);   // 500 rows
            AddCheck(rows, "200000", 2);

            var chunks = DisbursementSplitter.Split(rows, 500);

            Assert.Equal(2, chunks.Count);
            Assert.Equal(500, chunks[0].Count);
            Assert.Equal(2, chunks[1].Count);
        }

        [Fact]
        public void RowsWithNoCheckNumberAllGoInTheSameFileRegardlessOfCount()
        {
            var rows = new List<TrustDisbursement>();
            AddCheck(rows, "", 700);       // more than 500 rows with no CkNum
            AddCheck(rows, "  ", 3);       // whitespace counts as no CkNum too
            AddCheck(rows, "100001", 2);
            AddCheck(rows, "100002", 2);

            var chunks = DisbursementSplitter.Split(rows, 500);

            Assert.Equal(2, chunks.Count);
            Assert.Equal(703, chunks[0].Count);
            Assert.True(chunks[0].All(r => r.HasNoCheckNumber));
            Assert.True(chunks[1].All(r => !r.HasNoCheckNumber));
        }

        [Fact]
        public void NoCheckIsEverSplitAndEveryRowIsWrittenOnceInOrder()
        {
            var rows = new List<TrustDisbursement>();
            AddCheck(rows, "", 353);
            int ck = 600000;
            for (int i = 0; i < 3000; i++) AddCheck(rows, (ck++).ToString(), 1 + (i % 7 == 0 ? 20 : i % 3));

            var chunks = DisbursementSplitter.Split(rows, 500);

            Assert.Equal(rows, chunks.SelectMany(c => c).ToList());
            var fileOfCheck = new Dictionary<string, int>();
            for (int f = 0; f < chunks.Count; f++)
            {
                foreach (var r in chunks[f])
                {
                    int seenIn;
                    if (fileOfCheck.TryGetValue(r.CheckKey, out seenIn))
                        Assert.Equal(seenIn, f);
                    else
                        fileOfCheck[r.CheckKey] = f;
                }
            }
            for (int f = 0; f < chunks.Count - 1; f++)
                Assert.True(chunks[f].Count >= 500);
        }

        [Fact]
        public void ThrowsWhenFileIsNotSortedByCheckNumber()
        {
            var rows = new List<TrustDisbursement> { Row("100001"), Row("100002"), Row("100001") };

            var ex = Assert.Throws<InvalidDataException>(() => DisbursementSplitter.Split(rows, 500));
            Assert.Contains("CkNum 100001", ex.Message);
        }

        [Fact]
        public void ThrowsWhenRowsWithNoCheckNumberAreNotAllAtTheTop()
        {
            var rows = new List<TrustDisbursement> { Row(""), Row("100001"), Row("") };

            Assert.Throws<InvalidDataException>(() => DisbursementSplitter.Split(rows, 500));
        }
    }
}
