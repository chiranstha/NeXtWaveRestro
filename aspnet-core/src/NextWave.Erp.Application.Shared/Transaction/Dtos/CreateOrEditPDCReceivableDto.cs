using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class CreateOrEditPDCReceivableDto : EntityDto<Guid?>
    {
        public string VoucherNo { get; set; }

        public decimal? Amount { get; set; }

        public string ChequeNo { get; set; }

        public string ChequeMiti { get; set; }

        public string Description { get; set; }

        public string DateMiti { get; set; }

        public Guid BankId { get; set; }

        public Guid LedgerId { get; set; }
    }
}