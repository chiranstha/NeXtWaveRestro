using System;
using NextWave.Erp.Enums;


namespace Suktas.Erp.FinancialStatement
{
    public class FinancialLedgerPostingDto
    {

        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public Guid LedgerId { get; set; }
        public string LedgerName { get; set; }
        public AccountGroupNature Nature { get; set; }
        public int AccountGroupId { get; set; }
    }
}