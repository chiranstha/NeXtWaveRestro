using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Accounting.Dtos
{
    public class CreateOrEditFinancialYearDto : EntityDto<Guid?>
    {
        public string FromMiti { get; set; }

        public string ToMiti { get; set; }
    }
}
