using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction.Dtos
{
    public class CreateReceiptAgainstMasterDto
    {
        public Guid MasterId { get; set; }
        public DateTime Date { get; set; }
        public Guid DetailId { get; set; }
        public string MasterVoucherNo { get; set; }
        public int MasterVoucherNumbering { get; set; }
        public Guid MasterVoucherTypeId { get; set; }
        public Guid LedgerId { get; set; }
        public GetReceiptAgainstMasterDto Data { get; set; }
    }
}
