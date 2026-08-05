using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.ControlPanel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Authorization.BranchUser
{
    public class UserBranchManager(
       IRepository<UserBranch, Guid> userBranchRepository,
       IRepository<Branch, Guid> branchRepository)
       : ErpDomainServiceBase
    {
        public async Task UserBranchAdd(User user, List<Guid> inputBranch)
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();
            if (inputBranch != null)
            {
                if (inputBranch.Count > 0)
                {
                    foreach (var branchId in inputBranch)
                    {
                        var branchUser = new UserBranch
                        {
                            Id = Guid.Empty,
                            TenantId = tenantId,
                            UserId = user.Id,
                            BranchId = branchId,
                        };
                        await userBranchRepository.InsertAsync(branchUser);
                    }
                }
            }
        }


        public async Task UserBranchUpdate(User user, List<Guid> inputBranch)
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();

            if (inputBranch.Count > 0)
            {
                foreach (var deleteId in await userBranchRepository.GetAllListAsync(x =>
                             x.UserId == user.Id && x.TenantId == tenantId))
                {
                    await userBranchRepository.DeleteAsync(deleteId);
                }

                foreach (var branchId in inputBranch)
                {
                    var branchUser = new UserBranch
                    {
                        Id = Guid.Empty,
                        TenantId = tenantId,
                        UserId = user.Id,
                        BranchId = branchId,
                    };
                    await userBranchRepository.InsertAsync(branchUser);
                }
            }
        }

        public async Task<List<BranchResultDto>> GetAllBranchByUser(long userId)
        {
            List<BranchResultDto> result = new List<BranchResultDto>();
            var activeBranch = await userBranchRepository.GetAll().Include(x => x.BranchFk)
                .Where(a => a.UserId == userId && !a.IsDelete)
                .Select(x => new BranchResultDto
                {
                    Id = x.BranchId,
                    Name = x.BranchFk.Name,
                    IsActive = true
                }).ToListAsync();

            result.AddRange(activeBranch);

            var branchUser = await branchRepository.GetAll().Where(x => !activeBranch.Select(a => a.Id).Contains(x.Id))
                .Select(x => new BranchResultDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsActive = false
                }).ToListAsync();

            result.AddRange(branchUser);
            return result;
        }


        public async Task<List<BranchResultDto>> GetAllBranch()
        {
            return await branchRepository.GetAll().Where(x => x.Status)
                .Select(x => new BranchResultDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsActive = false
                }).ToListAsync();
        }
    }
}
