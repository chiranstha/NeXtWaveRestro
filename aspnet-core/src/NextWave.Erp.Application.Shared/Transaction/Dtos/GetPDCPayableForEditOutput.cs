using Abp.Application.Services.Dto;
using NextWave.Erp.Transaction.Enums;
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetPDCPayableForEditOutput : EntityDto<Guid>
    {
        public string VoucherNo { get; set; }

        public DateTime? Date { get; set; }

        public decimal? Amount { get; set; }

        public string ChequeNo { get; set; }

        public string ChequeMiti { get; set; }

        public string Description { get; set; }

        public string DateMiti { get; set; }

        public Guid LedgerId { get; set; }

        public string LedgerName { get; set; }
        public Guid BankId { get; set; }
        public string BankName { get; set; }
        public bool Editable { get; set; }
        public PDCClearanceType Status { get; set; }
        public string StatusString { get; set; }
        public string UpdateUser { get; set; }
        public string CreateUser { get; set; }
    }
}