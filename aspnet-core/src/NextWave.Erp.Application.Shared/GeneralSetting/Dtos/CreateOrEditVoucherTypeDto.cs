using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.GeneralSetting.Dtos
{
    public class CreateOrEditVoucherTypeDto : EntityDto<Guid?>
    {
        public int StartIndex { get; set; }
        public Guid VoucherTypeId { get; set; }
        public string Prefix { get; set; }
        public string Postfix { get; set; }
        public VoucherGenerateType VoucherGenerateType { get; set; }
    }
}
