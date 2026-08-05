using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction.Dtos
{
    public class RemainingCreditDto
    {
        public string Date { get; set; }
        public string DueDate { get; set; }
        public string VoucherType { get; set; }
        public string VoucherNo { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public bool IsDisable { get; set; }
    }
}
