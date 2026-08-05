using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Accounting.Dtos
{
    public class GetFinancialYearForViewDto : EntityDto<Guid>
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string FromMiti { get; set; }

        public string ToMiti { get; set; }

        public bool Status { get; set; }

        public bool Active { get; set; }
        public string Name { get; set; }
        public bool IsOldYear { get; set; }
    }
}
