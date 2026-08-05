using System;

namespace NextWave.Erp.Dto
{
    public class ImportUniversalFromExcelJobArgs
    {
        public int? TenantId { get; set; }

        public Guid BinaryObjectId { get; set; }
    }
}
