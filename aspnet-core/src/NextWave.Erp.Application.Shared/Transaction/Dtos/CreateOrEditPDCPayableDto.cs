using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class CreateOrEditPDCPayableDto : EntityDto<Guid?>
    {
        public string VoucherNo { get; set; }

        public DateTime? Date { get; set; }

        public decimal? Amount { get; set; }

        public string ChequeNo { get; set; }

        public DateTime? ChequeDate { get; set; }

        public string Description { get; set; }

        public Guid VoucherTypeId { get; set; }

        public string VoucherName { get; set; }

        public Guid LedgerId { get; set; }

        public Guid BankId { get; set; }

        public Guid FinancialYearId { get; set; }
        public string DateMiti { get; set; }
        public string ChequeMiti { get; set; }
    }
}