using System;
using System.Collections.Generic;
using System.IO;

namespace CheckFileSplitter
{
    /// <summary>
    /// Splits records into chunks of about <c>recordsPerChunk</c>, never splitting a check across chunks.
    /// </summary>
    public static class DisbursementSplitter
    {
        /// <summary>
        /// Rules:
        /// 1. A chunk is closed only once it holds at least <paramref name="recordsPerChunk"/> records
        ///    AND the next record starts a different check. So if record 500 is the first row of a check,
        ///    the rest of that check stays in the same chunk, even though the chunk goes over 500.
        /// 2. Rows with no CkNum (they come first in the file) are treated as one group, so they all
        ///    land in the same chunk however many there are.
        /// The input must already be sorted (grouped) by CkNum; otherwise an exception is thrown,
        /// because the rules above could not be guaranteed.
        /// </summary>
        public static List<List<TrustDisbursement>> Split(IList<TrustDisbursement> records, int recordsPerChunk)
        {
            if (recordsPerChunk < 1)
                throw new ArgumentOutOfRangeException(nameof(recordsPerChunk), "Must be at least 1.");

            EnsureGroupedByCheck(records);

            var chunks = new List<List<TrustDisbursement>>();
            var current = new List<TrustDisbursement>();
            string previousKey = null;

            foreach (var record in records)
            {
                bool startsNewCheck = record.CheckKey != previousKey;

                if (current.Count >= recordsPerChunk && startsNewCheck)
                {
                    chunks.Add(current);
                    current = new List<TrustDisbursement>();
                }

                current.Add(record);
                previousKey = record.CheckKey;
            }

            if (current.Count > 0)
                chunks.Add(current);

            return chunks;
        }

        /// <summary>
        /// Throws if any check number (including "no check number") appears in two separate runs,
        /// e.g. 500101, 500102, 500101.
        /// </summary>
        public static void EnsureGroupedByCheck(IList<TrustDisbursement> records)
        {
            var finishedChecks = new HashSet<string>();
            string previousKey = null;

            for (int i = 0; i < records.Count; i++)
            {
                string key = records[i].CheckKey;

                if (key == previousKey)
                    continue;

                if (previousKey != null)
                    finishedChecks.Add(previousKey);

                if (finishedChecks.Contains(key))
                {
                    string what = key == "" ? "rows with no CkNum" : $"CkNum {key}";
                    throw new InvalidDataException(
                        $"Data row {i + 1}: {what} appear again after other checks. " +
                        "The file must be sorted by CkNum, with rows that have no CkNum first.");
                }

                previousKey = key;
            }
        }
    }
}
