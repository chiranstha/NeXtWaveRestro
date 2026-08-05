using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Dto
{
    public class PartyBalanceNewEntryDto : EntityDto<Guid>
    {
        public DateTime Date { get; set; }
        public DateTime DueDate { get; set; }
        public Guid LedgerId { get; set; }
        public string VoucherNo { get; set; }
        public int VoucherNumbering { get; set; }
        public decimal Amount { get; set; }
        public Guid MasterId { get; set; }
        public Guid AgainstId { get; set; }
    }
}
