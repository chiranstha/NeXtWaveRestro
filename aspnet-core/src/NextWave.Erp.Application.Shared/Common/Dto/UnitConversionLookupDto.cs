using NextWave.Erp.Sales.Dtos;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Common.Dto
{
    public class UnitConversionLookupDto
    {
        public UnitConversionDto MinUnit { get; set; }
        public Dictionary<Guid, UnitConversionDto> AllUnits { get; set; }
    }
}
