using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction.Dtos
{
    public class GetReceiptAgainstDto
    {
        public Guid PartyBalanceId { get; set; }
        public int SN { get; set; }
        public string BillDate { get; set; }
        public string DueDate { get; set; }
        public string VoucherType { get; set; }
        public string VoucherNo { get; set; }
        public int VoucherNumbering { get; set; }
        public Guid VoucherTypeId { get; set; }
        public decimal BillAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public decimal Adjust { get; set; }
        public bool IsSettled { get; set; }
    }
}
