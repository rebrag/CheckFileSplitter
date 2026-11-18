using System;
using System.IO;
using Xunit;

namespace CheckFileSplitter.Tests
{
    public class DisbursementFileTests
    {
        const string Header =
            "CustomAttribute|Ecase_ID_CCC|TranDate|TrustDisbursementType|Matter|BankAcctTrust|TrustIntendedUse|Amount|" +
            "isReversed|ReverseDate|ReverseReason|Comment|Payee|Flag1099|IsPaid|isApproved|DocumentNumber|ReconStatusList|" +
            "CkNum|isVoided|VoidDate|NameOnCheck|OnBehalf|ExecutionGUID";

        const string RowWithCheck =
            "RECORD_000001||2026-11-15|100|MATTER_000001|BANK_ACCOUNT|100|40.08|0||||PAYEE_000001||1|1|||500123|0||PERSON_000001|SOURCE_000001|GUID";

        const string RowWithoutCheck =
            "RECORD_000002||2026-11-15|100|MATTER_000002|BANK_ACCOUNT|100|0|0||||PAYEE_000002||1|1||||0||PERSON_000002|SOURCE_000001|GUID";

        static string WriteTemp(string content)
        {
            string path = Path.GetTempFileName();
            File.WriteAllText(path, content);
            return path;
        }

        [Fact]
        public void ReadsHeaderSeparatelyFromRecords()
        {
            string path = WriteTemp(Header + "\n" + RowWithoutCheck + "\n" + RowWithCheck + "\n");

            var file = DisbursementFile.Read(path);

            Assert.Equal(Header, file.HeaderLine);
            Assert.Equal(2, file.Records.Count);
            Assert.True(file.Records[0].HasNoCheckNumber);
            Assert.Equal("500123", file.Records[1].CkNum);
            Assert.Equal("\n", file.NewLine);
        }

        [Fact]
        public void RowsRoundTripExactly()
        {
            string path = WriteTemp(Header + "\r\n" + RowWithCheck + "\r\n");

            var file = DisbursementFile.Read(path);

            Assert.Equal(RowWithCheck, file.Records[0].ToPipeLine());
            Assert.Equal("\r\n", file.NewLine);
        }

        [Fact]
        public void SkipsBlankLines()
        {
            string path = WriteTemp(Header + "\n\n" + RowWithCheck + "\n   \n");

            Assert.Single(DisbursementFile.Read(path).Records);
        }

        [Fact]
        public void ThrowsWhenHeaderIsMissing()
        {
            string path = WriteTemp(RowWithCheck + "\n");

            var ex = Assert.Throws<FormatException>(() => DisbursementFile.Read(path));
            Assert.Contains("header", ex.Message);
        }

        [Fact]
        public void ThrowsWithLineNumberWhenAColumnIsMissing()
        {
            string badRow = RowWithCheck.Substring(0, RowWithCheck.LastIndexOf('|'));   // 23 columns
            string path = WriteTemp(Header + "\n" + RowWithCheck + "\n" + badRow + "\n");

            var ex = Assert.Throws<FormatException>(() => DisbursementFile.Read(path));
            Assert.Contains("Line 3", ex.Message);
        }
    }
}
