using System;
using Abp.Domain.Entities;
using NextWave.Erp.Enums;

namespace Suktas.Erp.FinancialStatement
{
    public class AccountGroupDto : Entity<Guid>
    {
        public virtual string Name { get; set; }

        public virtual string Narration { get; set; }

        public virtual bool IsDefault { get; set; }

        public virtual bool AffectGrossProfit { get; set; }

        public virtual AccountGroupNature Nature { get; set; }

        public virtual Guid? GroupUnder { get; set; }
    }
}