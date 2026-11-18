namespace CheckFileSplitter
{
    /// <summary>
    /// One row of the trust disbursement file (24 pipe-delimited columns).
    /// </summary>
    public class TrustDisbursement
    {
        public const int ColumnCount = 24;
        public const int CkNumColumn = 18;

        public string CustomAttribute { get; set; }
        public string Ecase_ID_CCC { get; set; }
        public string TranDate { get; set; }
        public string TrustDisbursementType { get; set; }
        public string Matter { get; set; }
        public string BankAcctTrust { get; set; }
        public string TrustIntendedUse { get; set; }
        public string Amount { get; set; }
        public string isReversed { get; set; }
        public string ReverseDate { get; set; }
        public string ReverseReason { get; set; }
        public string Comment { get; set; }
        public string Payee { get; set; }
        public string Flag1099 { get; set; }
        public string IsPaid { get; set; }
        public string isApproved { get; set; }
        public string DocumentNumber { get; set; }
        public string ReconStatusList { get; set; }
        public string CkNum { get; set; }
        public string isVoided { get; set; }
        public string VoidDate { get; set; }
        public string NameOnCheck { get; set; }
        public string OnBehalf { get; set; }
        public string ExecutionGUID { get; set; }

        /// <summary>
        /// True when the row has no check number. These rows come first in the file
        /// and all go into the same output file.
        /// </summary>
        public bool HasNoCheckNumber
        {
            get { return string.IsNullOrWhiteSpace(CkNum); }
        }

        /// <summary>
        /// The value used to keep rows for the same check together.
        /// All rows without a check number share the key "".
        /// </summary>
        public string CheckKey
        {
            get { return HasNoCheckNumber ? "" : CkNum.Trim(); }
        }

        /// <summary>
        /// Rebuilds the original pipe-delimited row, in the same column order it was read in.
        /// </summary>
        public string ToPipeLine()
        {
            return string.Join("|", new[]
            {
                CustomAttribute, Ecase_ID_CCC, TranDate, TrustDisbursementType, Matter,
                BankAcctTrust, TrustIntendedUse, Amount, isReversed, ReverseDate,
                ReverseReason, Comment, Payee, Flag1099, IsPaid, isApproved,
                DocumentNumber, ReconStatusList, CkNum, isVoided, VoidDate,
                NameOnCheck, OnBehalf, ExecutionGUID
            });
        }
    }
}
