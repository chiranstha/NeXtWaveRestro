using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction
{

    public class AdjustmentEntryDto : EntityDto<Guid>
    {
        public Guid MasterId { get; set; }
        public string MasterVoucherNo { get; set; }
        public int MasterVoucherNumbering { get; set; }
        public Guid LedgerId { get; set; }
        public DateTime Date { get; set; }
        public decimal OnAccountPaid { get; set; }
        public decimal Amount { get; set; }
        public string VoucherNo { get; set; }
        public Guid VoucherTypeId { get; set; }
        public int VoucherNumbering { get; set; }
        public Guid DetailId { get; set; }
    }
}
