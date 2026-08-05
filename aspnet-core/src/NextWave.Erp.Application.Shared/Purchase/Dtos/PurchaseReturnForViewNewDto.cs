using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Dtos
{
    public class PurchaseReturnForViewNewDto
    {
        public Guid Id { get; set; }
        public string VoucherNo { get; set; }

        public string DateMiti { get; set; }
        public decimal TotalDiscount { get; set; }

        public string Description { get; set; }
        public string ReturnType { get; set; }

        public decimal TotalTax { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal GrandTotal { get; set; }

        public Guid? PurchaseMasterId { get; set; }

        // public virtual bool DebitOrCreditNote { get; set; }

        public Guid LedgerId { get; set; }


        public string PurchaseMasterVoucherNo { get; set; }

        public string LedgerName { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TotalTaxableAmount { get; set; }
        public List<PurchaseReturnDetailForViewDto> Details { get; set; }
    }
}
