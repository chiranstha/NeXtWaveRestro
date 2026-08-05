using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.ControlPanel
{
    [Table("tbl_Posting")]
    public class Posting : Entity<Guid>, IMayHaveTenant
    {
        [Required] public virtual decimal Numbering { get; set; }
        public int? TenantId { get; set; }
    }
}
