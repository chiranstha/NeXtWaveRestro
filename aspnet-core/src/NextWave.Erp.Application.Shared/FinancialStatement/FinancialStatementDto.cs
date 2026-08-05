using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.FinancialStatement
{
    public class FinancialStatementDto
    {
        public Guid Id { get; set; }
        public FinancialStatementDetail Data { get; set; }
        public List<FinancialStatementDto> Children { get; set; }
    }

    public class FinancialStatementDetail
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal OpeningDr { get; set; }
        public decimal OpeningCr { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal ClosingDr { get; set; }
        public decimal ClosingCr { get; set; }
        public TrailBalanceGroupEnum GroupType { get; set; }
    }
}
