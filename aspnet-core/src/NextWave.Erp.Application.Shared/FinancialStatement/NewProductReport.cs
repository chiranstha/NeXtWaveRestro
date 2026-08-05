using System;

namespace Suktas.Erp.FinancialStatement
{
    public class NewProductReport
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Rate { get; set; }
        public int VoucherNumbering { get; set; }
        public Guid UnitId { get; set; }
        public decimal InWardQty { get; set; }
        public decimal OutWardQty { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal InWardValue { get; set; }
        public decimal OutWardValue { get; set; }
        public string VoucherType { get; set; }

        public Guid? RackId { get; set; }
        public DateTime Date { get; set; }
        public bool IsAllowSerialNo { get; set; }
        public string ProductGroup { get; set; }
    }
}