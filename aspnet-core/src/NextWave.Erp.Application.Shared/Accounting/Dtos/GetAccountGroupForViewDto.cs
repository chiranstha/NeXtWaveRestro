using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.Accounting.Dtos
{
    public class GetAccountGroupForViewDto : EntityDto<Guid>
    {
        public string Name { get; set; }
        public string Narration { get; set; }
        public bool AffectGrossProfit { get; set; }

        public AccountGroupNature Nature { get; set; }
        public string NatureName { get; set; }
        public Guid? AccountGroupId { get; set; }
        public string AccountGroupName { get; set; }
        public bool IsDefault { get; set; }
    }
}
