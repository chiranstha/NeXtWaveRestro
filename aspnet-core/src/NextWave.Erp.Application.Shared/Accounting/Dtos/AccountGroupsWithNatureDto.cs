using NextWave.Erp.Enums;
using System;

namespace NextWave.Erp.Accounting.Dtos
{
    public class AccountGroupsWithNatureDto
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public AccountGroupNature Nature { get; set; }
        public bool AffectGrossProfit { get; set; }
    }
}
