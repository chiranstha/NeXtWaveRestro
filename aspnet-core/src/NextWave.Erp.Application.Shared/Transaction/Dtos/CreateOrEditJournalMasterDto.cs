using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction.Dtos
{
    public class CreateOrEditJournalMasterDto : EntityDto<Guid?>
    {
        public string VoucherNo { get; set; }

        public decimal DebitTotal { get; set; }
        public decimal CreditTotal { get; set; }
        public string Description { get; set; }

        public string DateMiti { get; set; }

        public string VoucherName { get; set; }
        public decimal TotalAmount { get; set; }
        public List<CreateOrEditJournalDetailDto> JournalDetail { get; set; }
        public string ReferenceNo { get; set; }
    }
}
