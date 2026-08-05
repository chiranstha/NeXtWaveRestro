using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Dto
{
    public class CreateOrEditDocumentDto : EntityDto<Guid?>
    {
        public Guid VoucherTypeId { get; set; }
        public string VoucherNo { get; set; }
    }
}
