using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Dtos
{
    public class PriceLevelProductListStockJournalDto
    {
        public Guid Id { get; set; }
        public ProductTypeEnum ProductType { get; set; }
        public decimal? Rate { get; set; }
        public decimal Mrp { get; set; }
        public Guid TaxId { get; set; }
        public Guid UnitId { get; set; }
        public decimal Quantity { get; set; }
        public decimal TaxRate { get; set; }
        public string ProductName { get; set; }
    }
}
