using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Dtos
{
    public class CreateOrEditPaymentMasterDto : EntityDto<Guid?>
    {
        public string VoucherNo { get; set; }

        public decimal TotalAmount { get; set; }

        public string Description { get; set; }

        public string DateMiti { get; set; }

        public Guid LedgerId { get; set; }

        public List<CreateOrEditReceiptDetailDto> PaymentDetails { get; set; }
    }
}