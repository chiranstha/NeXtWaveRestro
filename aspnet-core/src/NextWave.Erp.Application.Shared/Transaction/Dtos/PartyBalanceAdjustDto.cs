using System;

namespace NextWave.Erp.Transaction.Dtos
{
    public class PartyBalanceAdjustDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string PayDate { get; set; }
        public string DueDate { get; set; }
        public string BalanceString { get; set; }
        public decimal Balance { get; set; }
        public decimal BillAmt { get; set; }
        public decimal Adjust { get; set; }
        public bool IsDisable { get; set; }
        public string VoucherTypeName { get; set; }
        public decimal OnAccountPaid { get; set; }
        public decimal Paid { get; set; }
        public string VoucherNo { get; set; }
        public Guid VoucherTypeId { get; set; }
        public int VoucherNumbering { get; set; }
    }
}
