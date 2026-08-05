using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.Accounting.Dtos
{
    public class AccountLedgerZeroOpeningBalanceDto
    {
        public Guid AccountLedgerId { get; set; }
        public string LedgerName { get; set; }
        public decimal OpeningBalance { get; set; }
        public DrOrCr DrOrCr { get; set; }
    }
}
