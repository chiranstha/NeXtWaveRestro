using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.GeneralSetting.Dtos
{
    public class GetVoucherTypeForEditOutput : EntityDto<Guid>
    {
        public virtual Guid VoucherTypeId { get; set; }
        public virtual int StartIndex { get; set; }
        public virtual string Prefix { get; set; }
        public virtual string Postfix { get; set; }
        public virtual Guid FinancialYearId { get; set; }
        public string FinancialYearName { get; set; }
        public VoucherGenerateType VoucherGenerateType { get; set; }
    }
}
