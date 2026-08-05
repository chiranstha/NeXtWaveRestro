using Abp.Application.Services.Dto;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace NextWave.Erp.Accounting.Dtos
{
    public class CreateOrEditAccountLedgerDto : EntityDto<Guid?>
    {
        [Required] public string Name { get; set; }
        public string SurName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public decimal OpeningBalance { get; set; }
        public DrOrCr CrOrDr { get; set; }
        public bool IsBillByBill { get; set; }
        public bool IsRequiredUser { get; set; }
        public Guid AccountGroupId { get; set; }
        public string Narration { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Pan { get; set; }
        public short? CreditPeriod { get; set; }
        public decimal? CreditLimit { get; set; }
        public bool IsCompany { get; set; }
    }
}
