using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Dto
{
    public class GetAllUniversalMastersInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }
        public string FromMiti { get; set; }
        public string ToMiti { get; set; }
    }
}
