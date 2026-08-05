using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.Dto
{
    public class GetBranchForViewDto : EntityDto<Guid>
    {
        public string Name { get; set; }

        public string Description { get; set; }
        public BranchType BranchType { get; set; }
        public string BranchTypeName { get; set; }
       
        public byte[] Logo1 { get; set; }
        public byte[] Logo2 { get; set; }
        public string CompanyName { get; set; }
        public string CompanyContact { get; set; }
        public string Address { get; set; }

        public bool IsMain { get; set; }
    }
}
