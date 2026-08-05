using Abp.Application.Services.Dto;
using NextWave.Erp.Transaction.Enums;
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class CreateOrEditPDCClearanceDto : EntityDto<Guid?>
    {
        public string VoucherNo { get; set; }
        public PDCClearanceAgainstMode AgainstMode { get; set; }
        public string Description { get; set; }
        public PDCClearanceType Status { get; set; }
        public string DateMiti { get; set; }
        public decimal Amount { get; set; }
        public string ChequeNo { get; set; }
        public string ChequeMiti { get; set; }
        public Guid BankId { get; set; }
        public string VoucherName { get; set; }
        public Guid LedgerId { get; set; }
        public Guid AgainstLedgerId { get; set; }
        public Guid AgainstId { get; set; }
    }
}