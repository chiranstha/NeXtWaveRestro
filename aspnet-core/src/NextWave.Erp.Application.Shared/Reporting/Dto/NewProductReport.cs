using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.Reporting.Dto
{
    public class NewProductReport
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public ProductTypeEnum ProductType { get; set; }
        public decimal Rate { get; set; }
        public int VoucherNumbering { get; set; }
        public Guid UnitId { get; set; }
        public decimal InWardQty { get; set; }
        public decimal OutWardQty { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal InWardValue { get; set; }
        public decimal OutWardValue { get; set; }
        public string VoucherType { get; set; }
        public DateTime Date { get; set; }
        public string ProductGroup { get; set; }
    }
}
