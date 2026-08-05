using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Dtos
{
    public class GetProductForEditOutput : EntityDto<Guid?>
    {
        public string ProductCode { get; set; }
        public ProductTypeEnum ProductType { get; set; }

        public string Name { get; set; }
        public string HsCode { get; set; }

        public decimal? Mrp { get; set; }

        public decimal? SalesRate { get; set; }

        public decimal? PurchaseRate { get; set; }

        public decimal? MinimumStock { get; set; }

        public decimal? MaximumStock { get; set; }
        public decimal Margin { get; set; }


        public bool IsOpeningStock { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public Guid TaxId { get; set; }


        public Guid ProductGroupId { get; set; }

        public Guid UnitId { get; set; }
        public bool IsUnitEdit { get; set; }


        public List<StockPostingCreateDto> OpeningStock { get; set; }
    }
}
