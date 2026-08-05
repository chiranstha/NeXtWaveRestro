using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.Accounting.Importing.Dto
{
    public class ImportAccountGroupDto
    {
        public virtual string Name { get; set; }

        public virtual string Narration { get; set; }

        public virtual bool IsDefault { get; set; }

        public virtual bool AffectGrossProfit { get; set; }

        public virtual AccountGroupNature Nature { get; set; }

        public virtual Guid? GroupUnder { get; set; }

        public int? TenantId { get; set; }
    }
}
