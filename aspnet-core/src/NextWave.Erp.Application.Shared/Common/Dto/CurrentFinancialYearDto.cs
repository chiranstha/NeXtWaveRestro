using System;

namespace NextWave.Erp.Common.Dto
{
    public class CurrentFinancialYearDto
    {
        public virtual DateTime FromDate { get; set; }

        public virtual DateTime ToDate { get; set; }

        public virtual string FromMiti { get; set; }

        public virtual string ToMiti { get; set; }
    }
}
