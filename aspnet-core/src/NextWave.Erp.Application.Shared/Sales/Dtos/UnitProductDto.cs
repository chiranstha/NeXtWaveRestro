using System;

namespace NextWave.Erp.Sales.Dtos
{
    public class UnitProductDto
    {
        public Guid Id { get; set; }
        public decimal SalesRate { get; set; }
        public decimal Margin { get; set; }
    }

    public class StockSummaryDto
    {
        public Guid UnitId { get; set; }
        public decimal NetQuantity { get; set; }
    }

    public class PriceDetailDto
    {
        public decimal FinalRate { get; set; }
        public Guid UnitId { get; set; }
    }
}
