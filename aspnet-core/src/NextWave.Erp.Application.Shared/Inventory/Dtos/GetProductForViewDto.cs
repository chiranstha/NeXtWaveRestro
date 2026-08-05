using Abp.Application.Services.Dto;
using NextWave.Erp.Dto;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Inventory.Dtos
{
    public class GetProductForViewDto : EntityDto<Guid>
    {
        public Guid? ProductId { get; set; }

        public string Name { get; set; }


        public string ProductGroupName { get; set; }

        public decimal? Mrp { get; set; }

        public decimal? SalesRate { get; set; }

        public decimal? PurchaseRate { get; set; }
        public decimal? MinimumStock { get; set; }



        public bool IsOpeningStock { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; }

        public Guid TaxId { get; set; }
        public Guid ProductGroupId { get; set; }
        public UniversalDropdownDto Unit { get; set; }
        public decimal TaxRate { get; set; }
        public Guid UnitId { get; set; }
        public decimal Rate { get; set; }
        public string UnitName { get; set; }
    }
}
