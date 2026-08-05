using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction.Dtos
{
    public class CreateOrEditReceiptMasterDto : EntityDto<Guid?>
    {
        public string VoucherNo { get; set; }

        public decimal TotalAmount { get; set; }

        public string Description { get; set; }

        public string DateMiti { get; set; }
        public Guid LedgerId { get; set; }


        public List<CreateOrEditReceiptDetailDto> ReceiptDetails { get; set; }
    }
}
