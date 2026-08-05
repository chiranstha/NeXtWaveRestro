using System;

namespace NextWave.Erp.GeneralSetting.Dtos
{
    public class LedgerPostingForReportDto
    {
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public Guid LedgerId { get; set; }
        public Guid AccountGroupId { get; set; }
        public string VoucherName { get; set; }
        public DateTime? Date { get; set; }
        public Guid FinancialYearId { get; set; }
    }
}
