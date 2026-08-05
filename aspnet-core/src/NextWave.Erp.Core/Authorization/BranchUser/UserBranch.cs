using Abp.Domain.Entities;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.ControlPanel;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Authorization.BranchUser
{
    [Table("AbpUserBranch")]
    public class UserBranch : Entity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }

        public long UserId { get; set; }

        [ForeignKey("UserId")] public virtual User UserFk { get; set; }

        public virtual Guid BranchId { get; set; }

        [ForeignKey("BranchId")] public virtual Branch BranchFk { get; set; }
        public bool IsDelete { get; set; } = false;

        public static UserBranch CreateBranchAdminUser(int tenantId, long userId, Guid branchId)
        {
            return new UserBranch
            {
                Id = Guid.NewGuid(), // Generate a new GUID instead of using default
                TenantId = tenantId,
                UserId = userId,
                BranchId = branchId,
                IsDelete = false
            };
        }
    }
}
