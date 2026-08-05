using Abp.Application.Services.Dto;
using NextWave.Erp.Transaction.Enums;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Dtos
{
    public class CreateOrEditContraMasterDto : EntityDto<Guid?>
    {
        public ContraType Type { get; set; }

        public string VoucherNo { get; set; }

        public decimal TotalAmount { get; set; }

        public string Narration { get; set; }

        public string DateMiti { get; set; }

        public Guid LedgerId { get; set; }

        public List<CreateOrEditContraDetailDto> ContraDetails { get; set; }
    }
}
