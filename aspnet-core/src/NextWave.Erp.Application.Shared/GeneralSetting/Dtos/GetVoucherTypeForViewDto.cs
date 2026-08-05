using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.GeneralSetting.Dtos
{
    public class GetVoucherTypeForViewDto : EntityDto<Guid>
    {
        public VoucherGenerateType VoucherGenerateType { get; set; }
        public string VoucherName { get; set; }

        public int StartIndex { get; set; }
        public string Prefix { get; set; }
        public string Postfix { get; set; }
    }
}
