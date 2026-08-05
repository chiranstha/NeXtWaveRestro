using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.Accounting.Dtos
{
    public class CreateOrEditAccountGroupDto : EntityDto<Guid?>
    {
        public string Name { get; set; }

        public string Narration { get; set; }


        public bool AffectGrossProfit { get; set; }

        public AccountGroupNature Nature { get; set; }

        public Guid GroupUnder { get; set; }
    }
}
