using System;

namespace NextWave.Erp.Accounting.Dtos
{
    public class AccountLedgerAccountGroupTableDto
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; }
        public string Nature { get; set; }

        public virtual bool IsBank { get; set; }

        public int Order { get; set; }
    }
}
