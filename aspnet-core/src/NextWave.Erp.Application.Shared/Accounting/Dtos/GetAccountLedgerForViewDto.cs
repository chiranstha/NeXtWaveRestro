using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Accounting.Dtos
{
    public class GetAccountLedgerForViewDto : EntityDto<Guid>
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string PaNumber { get; set; }
        public virtual decimal? CreditLimit { get; set; }
        public string Phone { get; set; }
        public bool IsDefault { get; set; }
        public Guid AccountGroupId { get; set; }
        public string AccountGroupName { get; set; }
    }
}
