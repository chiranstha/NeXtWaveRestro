using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Dto;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Purchase;
using NextWave.Erp.Sales;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory
{
    [AbpAuthorize(AppPermissions.PagesProductMerge)]
    public class ProductMergeAppService(
    IRepository<Product, Guid> productRepository,
    IRepository<SalesReturnDetail, Guid> salesReturnDetailsRepository,
    IRepository<StockFifoTable, Guid> stockFifoTablesRepository,
    IRepository<StockMaintain, Guid> stockMaintainsRepository,
    IRepository<StockPosting, Guid> stockPostingRepository,
    IRepository<Bom, Guid> bomRepository,
    IRepository<PurchaseDetail, Guid> purchaseDetailsRepository,
    IRepository<PurchaseOrderDetails, Guid> purchaseOrderDetailsRepository,
    IRepository<PurchaseProductCancelDetail, Guid> purchaseProductCancelDetailRepository,
    IRepository<PurchaseReturnDetail, Guid> purchaseReturnDetailsRepository,
    IRepository<SalesDetail, Guid> salesDetailsRepository,
    IRepository<UnitConversion, Guid> unitConversionRepository,
    IRepository<SalesProductCancelDetail, Guid> salesProductCancelDetailRepository,
    StockManagementAppService stockManagementAppService) : ErpAppServiceBase
    {
        public async Task CreateProductMerge(CreateProductMergeDto input)
        {
            // UnitOf Work
            var unitOfWork = UnitOfWorkManager.Begin();

            var oldProduct = await productRepository.FirstOrDefaultAsync(input.OldProductId);
            var newProduct = await productRepository.FirstOrDefaultAsync(input.NewProductId);

            if (oldProduct == newProduct) throw new UserFriendlyException("Same Product Can't Merge");

            if (oldProduct == null || newProduct == null) throw new UserFriendlyException("Product Not Found");


            foreach (var item in await salesReturnDetailsRepository.GetAll()
                         .Where(e => e.TenantId == AbpSession.TenantId && e.ProductId == input.OldProductId).ToListAsync())
            {
                item.ProductId = input.NewProductId;
                await salesReturnDetailsRepository.UpdateAsync(item);
            }

           
            await stockManagementAppService.MergeProduct(input.OldProductId, input.NewProductId);
                      

            foreach (var item in await stockPostingRepository.GetAll()
                         .Where(e => e.TenantId == AbpSession.TenantId && e.ProductId == input.OldProductId).ToListAsync())
            {
                item.ProductId = input.NewProductId;
                await stockPostingRepository.UpdateAsync(item);
            }

            foreach (var item in await bomRepository.GetAll()
                         .Where(e => e.TenantId == AbpSession.TenantId && e.ProductId == input.OldProductId)
                         .ToListAsync()) await bomRepository.DeleteAsync(item);
                       

            foreach (var item in await unitConversionRepository.GetAll()
                         .Where(e => e.TenantId == AbpSession.TenantId && e.ProductId == input.OldProductId).ToListAsync())
            {
                item.ProductId = input.NewProductId;
                await unitConversionRepository.UpdateAsync(item);
            }           

            foreach (var item in await purchaseDetailsRepository.GetAll()
                         .Where(e => e.TenantId == AbpSession.TenantId && e.ProductId == input.OldProductId).ToListAsync())
            {
                item.ProductId = input.NewProductId;
                await purchaseDetailsRepository.UpdateAsync(item);
            }                       

            foreach (var item in await purchaseOrderDetailsRepository.GetAll()
                         .Where(e => e.TenantId == AbpSession.TenantId && e.ProductId == input.OldProductId).ToListAsync())
            {
                item.ProductId = input.NewProductId;
                await purchaseOrderDetailsRepository.UpdateAsync(item);
            }

            foreach (var item in await purchaseProductCancelDetailRepository.GetAll()
                         .Where(e => e.TenantId == AbpSession.TenantId && e.ProductId == input.OldProductId).ToListAsync())
            {
                item.ProductId = input.NewProductId;
                await purchaseProductCancelDetailRepository.UpdateAsync(item);
            }

            foreach (var item in await purchaseReturnDetailsRepository.GetAll()
                         .Where(e => e.TenantId == AbpSession.TenantId && e.ProductId == input.OldProductId).ToListAsync())
            {
                item.ProductId = input.NewProductId;
                await purchaseReturnDetailsRepository.UpdateAsync(item);
            }
                       

            foreach (var item in await salesDetailsRepository.GetAll()
                         .Where(e => e.TenantId == AbpSession.TenantId && e.ProductId == input.OldProductId).ToListAsync())
            {
                item.ProductId = input.NewProductId;
                await salesDetailsRepository.UpdateAsync(item);
            }

           

            foreach (var item in await salesProductCancelDetailRepository.GetAll()
                         .Where(e => e.TenantId == AbpSession.TenantId && e.ProductId == input.OldProductId).ToListAsync())
            {
                item.ProductId = input.NewProductId;
                await salesProductCancelDetailRepository.UpdateAsync(item);
            }           

            await productRepository.DeleteAsync(input.OldProductId);


            await unitOfWork.CompleteAsync();
        }


        public async Task<List<UniversalDropdownDto>> GetAllProductDropdown()
        {
            return await productRepository.GetAll()
                .Where(e => e.TenantId == AbpSession.TenantId)
                .Select(x => new UniversalDropdownDto
                {
                    Id = x.Id,
                    DisplayName = x.Name
                }).ToListAsync();
        }

        public async Task<List<UniversalDropdownDto>> GetAllProductDropdownByProductId(Guid productId)
        {
            return await productRepository.GetAll()
                .Where(e => e.TenantId == AbpSession.TenantId && e.Id != productId)
                .Select(x => new UniversalDropdownDto
                {
                    Id = x.Id,
                    DisplayName = x.Name
                }).ToListAsync();
        }


        public async Task<List<UniversalDropdownDto>> GetAllProductUnitDropdown(Guid productId)
        {
            var units = await unitConversionRepository.GetAll()
                .Where(e => e.TenantId == AbpSession.TenantId)
                .Include(e => e.UnitFk)
                .Where(e => e.ProductId == productId)
                .Select(x => new UniversalDropdownDto
                {
                    Id = x.UnitId,
                    DisplayName = x.UnitFk.Name
                }).ToListAsync();
            return units.DistinctBy(e => e.Id).ToList();
        }
    }
}
