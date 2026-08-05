using Abp.Application.Services.Dto;
using System;

namespace NextWave.Erp.Purchase.Dtos
{
    public class PurchaseDetailDto : EntityDto<Guid>
    {
        public decimal? Qty { get; set; }

        public decimal? Discount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? TaxAmount { get; set; }

        public decimal? TaxValue { get; set; }

        public decimal? GrossAmount { get; set; }

        public decimal? NetAmount { get; set; }

        public decimal? Amount { get; set; }

        public Guid? OrderDetailId { get; set; }

        public Guid ProductId { get; set; }
        //public string ProductCode { get; set; }

        public Guid UnitId { get; set; }

        public Guid? TaxId { get; set; }

        public decimal? Rate { get; set; }
        //public decimal ImportRate { get; set; }
        //public List<CustomLedgerList> CustomLedgerList { get; set; }
        //public decimal? CustomAmount { get; set; }
        //public List<PurchaseReturnUnitsQtyDto> UnitsList { get; set; }
    }

    //public class CustomLedgerList
    //{
    //    public Guid Id { get; set; }
    //    public Guid LedgerId { get; set; }
    //    public Guid EffectLedgerId { get; set; }
    //    public decimal Amount { get; set; }
    //    public decimal? Percent { get; set; }
    //}
}
