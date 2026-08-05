using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.GeneralSetting.Dtos
{
    public class GetTaxForViewDto : EntityDto<Guid>
    {
        public string Name { get; set; }
        public decimal Rate { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public Guid LedgerId { get; set; }
    }
}
