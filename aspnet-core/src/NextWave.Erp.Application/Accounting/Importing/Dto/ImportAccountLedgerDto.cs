using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;

namespace NextWave.Erp.Accounting.Importing.Dto
{
    public class ImportAccountLedgerDto : EntityDto
    {
        public string Name { get; set; }

        public double? OpeningBalance { get; set; }

        public double? CreditLimit { get; set; }

        public int AccountGroupId { get; set; }
        public DrOrCr CrOrDr { get; set; }
        public string Phone { get; set; }
        public string AccountGroupName { get; set; }
        public string Pan { get; set; }
        public string Address { get; set; }
        public string Exception { get; set; }

        public bool CanBeImported()
        {
            return string.IsNullOrEmpty(Exception);
        }
    }
}
