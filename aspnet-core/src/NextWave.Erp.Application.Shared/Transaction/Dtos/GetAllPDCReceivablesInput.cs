using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetAllPDCReceivablesInput : PagedAndSortedResultRequestDto
    {
        public string Filter { get; set; }

        public string LedgerNameFilter { get; set; }

        public string FinancialYearFromMitiFilter { get; set; }
    }
}
