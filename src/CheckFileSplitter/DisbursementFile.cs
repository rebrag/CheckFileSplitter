using System;
using System.Collections.Generic;
using System.IO;

namespace CheckFileSplitter
{
    /// <summary>
    /// A parsed input file: the header row plus every data row.
    /// </summary>
    public class DisbursementFile
    {
        public string HeaderLine { get; set; }
        public List<TrustDisbursement> Records { get; set; }

        public static DisbursementFile Read(string filePath)
        {
            var records = new List<TrustDisbursement>();
            string headerLine = null;
            int lineNumber = 0;

            using (var reader = new StreamReader(filePath))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var c = line.Split('|');

                    if (c.Length != TrustDisbursement.ColumnCount)
                        throw new FormatException(
                            $"Line {lineNumber}: expected {TrustDisbursement.ColumnCount} columns, found {c.Length}.");

                    // The first non-blank line is the header record. It is copied to the top of every output file.
                    if (headerLine == null)
                    {
                        if (c[TrustDisbursement.CkNumColumn] != "CkNum")
                            throw new FormatException(
                                $"Line {lineNumber}: expected a header row with 'CkNum' in column {TrustDisbursement.CkNumColumn}, " +
                                $"found '{c[TrustDisbursement.CkNumColumn]}'.");

                        headerLine = line;
                        continue;
                    }

                    records.Add(new TrustDisbursement
                    {
                        CustomAttribute = c[0],
                        Ecase_ID_CCC = c[1],
                        TranDate = c[2],
                        TrustDisbursementType = c[3],
                        Matter = c[4],
                        BankAcctTrust = c[5],
                        TrustIntendedUse = c[6],
                        Amount = c[7],
                        isReversed = c[8],
                        ReverseDate = c[9],
                        ReverseReason = c[10],
                        Comment = c[11],
                        Payee = c[12],
                        Flag1099 = c[13],
                        IsPaid = c[14],
                        isApproved = c[15],
                        DocumentNumber = c[16],
                        ReconStatusList = c[17],
                        CkNum = c[18],
                        isVoided = c[19],
                        VoidDate = c[20],
                        NameOnCheck = c[21],
                        OnBehalf = c[22],
                        ExecutionGUID = c[23]
                    });
                }
            }

            if (headerLine == null)
                throw new FormatException("The input file is empty.");

            return new DisbursementFile
            {
                HeaderLine = headerLine,
                Records = records
            };
        }
    }
}
