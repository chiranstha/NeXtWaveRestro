using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Dtos
{
    public class ProductUnitConversionDto : EntityDto<Guid>
    {
        public decimal InitializedQty { get; set; }
        public Guid InitializeUnitId { get; set; }
        public decimal FinalQty { get; set; }
        public Guid UnitId { get; set; }
        public bool IsEdit { get; set; }
    }
}
