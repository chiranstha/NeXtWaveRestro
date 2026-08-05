using Abp.Application.Services.Dto;
using NextWave.Erp.Transaction.Enums;
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetPDCClearanceForViewDto : EntityDto<Guid>
    {
        public string VoucherNo { get; set; }
        public DateTime? Date { get; set; }
        public PDCClearanceAgainstMode AgainstMode { get; set; }
        public string AgainstModeName { get; set; }
        public string Description { get; set; }
        public PDCClearanceType Status { get; set; }
        public string StatusName { get; set; }
        public string DateMiti { get; set; }
        public string ChequeNo { get; set; }
        public string ChequeMiti { get; set; }
        public Guid BankId { get; set; }
        public string BankName { get; set; }
        public decimal Amount { get; set; }
        public Guid LedgerId { get; set; }
        public Guid AgainstLedgerId { get; set; }
        public string LedgerName { get; set; }
        public string ReceivedInOrPaidFrom { get; set; }
        public string UpdateUser { get; set; }
        public string CreateUser { get; set; }
    }
}