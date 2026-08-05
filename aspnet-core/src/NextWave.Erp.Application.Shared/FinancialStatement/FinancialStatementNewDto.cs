using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;

namespace Suktas.Erp.FinancialStatement
{
    public class FinancialStatementNewDto
    {
        public Guid Id { get; set; }
        public FinancialStatementDetailNew Data { get; set; }
        public List<FinancialStatementNewDto> Children { get; set; }
    }

    public class FinancialStatementDetailNew
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