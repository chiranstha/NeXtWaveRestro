using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetPaymentMasterForViewDto : EntityDto<Guid>
    {
        public string VoucherNo { get; set; }

        public DateTime Date { get; set; }

        public decimal TotalAmount { get; set; }

        public string DateMiti { get; set; }

        public Guid VoucherTypeId { get; set; }
        public string VoucherType { get; set; }
        public string LedgerName { get; set; }
        public string UpdateUser { get; set; }
        public string CreateUser { get; set; }
    }
}