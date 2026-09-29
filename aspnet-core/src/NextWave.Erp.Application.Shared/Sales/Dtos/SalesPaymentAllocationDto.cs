using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.Sales.Dtos
{
    public class SalesPaymentAllocationDto
    {
        public PaymentMethod PaymentMethod { get; set; }
        public Guid? PaymentLedgerId { get; set; }
        public decimal Amount { get; set; }
        public string Reference { get; set; }
    }
}
