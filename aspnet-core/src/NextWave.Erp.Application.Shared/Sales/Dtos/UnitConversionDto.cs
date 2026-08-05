using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Dtos
{
    public class UnitConversionDto
    {
        public Guid ProductId { get; set; }
        public Guid UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal PrimaryQty { get; set; }
        public decimal Qty { get; set; }
        public decimal ConversionRate { get; set; }
    }

    public class StockPostingDto
    {
        public Guid ProductId { get; set; }
        public ProductTypeEnum ProductType { get; set; }

        public DateTime Date { get; set; }

        public string VoucherType { get; set; }

        public string ProductName { get; set; }
        public decimal Rate { get; set; }

        public Guid UnitId { get; set; }

        public decimal InWardQty { get; set; }
        public decimal OutWardQty { get; set; }
        public string ProductGroup { get; set; }


    }

    //public class ProductBriefDto
    //{
    //    public Guid ProductId { get; set; }
    //    public string ProductName { get; set; }
    //    public bool IsMultipleUnit { get; set; }
    //    public string UnitName { get; set; }
    //}

    public class StockCalculationDto
    {
        public int SlNo { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Rate { get; set; }
        public string OpeningStockQtyString { get; set; }
        public decimal OpeningStockValue { get; set; }
        public string InWardQtyString { get; set; }
        public decimal InWardValue { get; set; }
        public string OutWardQtyString { get; set; }
        public decimal OutWardValue { get; set; }
        public string ClosingQtyString { get; set; }
        public decimal ClosingValue { get; set; }
        public decimal ClosingQty { get; set; }
        public string ProductGroup { get; set; }
    }
}
