using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.Accounting.Dtos
{
    public class GetAccountLedgerForEditOutput : EntityDto<Guid>
    {
        public string Name { get; set; }

        public decimal? OpeningBalance { get; set; }

        public bool IsDefault { get; set; }

        public DrOrCr CrOrDr { get; set; }

        public string Narration { get; set; }
        public string Address { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public int? CreditPeriod { get; set; }

        public bool IsBank { get; set; }
        public decimal? CreditLimit { get; set; }

        public bool IsBillByBill { get; set; }

        public string Pan { get; set; }

        public bool Status { get; set; }

        public Guid AccountGroupId { get; set; }
        public string AccountGroupName { get; set; }

        public bool IsCompany { get; set; }
    }
}
