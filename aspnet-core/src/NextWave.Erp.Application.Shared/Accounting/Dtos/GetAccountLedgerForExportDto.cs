using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.Accounting.Dtos
{
    public class GetAccountLedgerForExportDto : EntityDto<Guid>
    {
        public string Name { get; set; }

        public string Phone { get; set; }
        public decimal? OpeningBalance { get; set; }
        public decimal? CreditLimit { get; set; }
        public DrOrCr CrOrDr { get; set; }
        public int AccountGroupId { get; set; }

        public string AccountGroupName { get; set; }
        public string Address { get; set; }
        public string Pan { get; set; }
    }
}
