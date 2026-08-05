using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.UI;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Dto;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase.Importing
{

    internal class ImportPurchaseMasterToExcelJob(
        IUnitOfWorkManager unitOfWorkManager,
        IBinaryObjectManager binaryObjectManager,
        IPurchaseMasterListExcelDataReader purchaseMasterListExcelDataReader,
        IRepository<PurchaseMaster, Guid> purchaseMasterRepository,
        IRepository<Branch, Guid> branchRepository,
        IRepository<PurchaseDetail, Guid> purchaseDetailRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<AccountLedger, Guid> ledgerRepository,
        IRepository<VoucherType, Guid> voucherTypeRepository,
        IRepository<FinancialYear, Guid> financialYearRepository)
        : BackgroundJob<ImportUniversalFromExcelJobArgs>, ITransientDependency
    {
        private readonly IRepository<Product, Guid> _productRepository = productRepository;
        private readonly IRepository<PurchaseDetail, Guid> _purchaseDetailRepository = purchaseDetailRepository;

        public override void Execute(ImportUniversalFromExcelJobArgs args)
        {
            var purchase = GetPurchaseMasterListFromExcelOrNull(args);

            CreatePurchaseMaster(args, purchase);
        }

        private List<GetPurchaseMasterForImportDto> GetPurchaseMasterListFromExcelOrNull(
            ImportUniversalFromExcelJobArgs args)
        {
            using var uow = unitOfWorkManager.Begin();
            using (CurrentUnitOfWork.SetTenantId(args.TenantId))
            {
                try
                {
                    var file = AsyncHelper.RunSync(() => binaryObjectManager.GetOrNullAsync(args.BinaryObjectId));
                    return purchaseMasterListExcelDataReader.GetpurchaseMasterFromExcel(file.Bytes);
                }
                catch (Exception)
                {
                    return null;
                }
                finally
                {
                    uow.Complete();
                }
            }
        }

        private void CreatePurchaseMaster(ImportUniversalFromExcelJobArgs args,
            List<GetPurchaseMasterForImportDto> purchaseMasters)
        {
            var purchaseList = new List<GetPurchaseMasterForImportDto>();
            foreach (var purchase in purchaseMasters)
            {
                using (var uow = unitOfWorkManager.Begin())
                {
                    using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                    {
                        try
                        {
                            AsyncHelper.RunSync(() => CreatePurchaseMasterAsync(purchase));
                        }
                        catch (UserFriendlyException exception)
                        {
                            purchase.Exception = exception.Message;
                            purchaseList.Add(purchase);
                        }
                        catch (Exception exception)
                        {
                            purchase.Exception = exception.ToString();
                            purchaseList.Add(purchase);
                        }
                    }

                    uow.Complete();
                }

                using (var uow = unitOfWorkManager.Begin())
                {
                    using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                    {
                    }

                    uow.Complete();
                }
            }
        }

        private async Task CreatePurchaseMasterAsync(GetPurchaseMasterForImportDto input)
        {
            var voucherNo = await purchaseMasterRepository.FirstOrDefaultAsync(x => x.VoucherNo == input.VoucherNo);

            if (voucherNo == null)
            {
                var tenantId = CurrentUnitOfWork.GetTenantId();
                var branchId = (await branchRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId)).Id;
                var ledgerId = (await ledgerRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId)).Id;
                var voucherTypeId = (await voucherTypeRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId)).Id;
                var financialYearId = (await financialYearRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId)).Id;
                //var accountGroup = await _accountGroupRepository.FirstOrDefaultAsync(x => x.Name == input.AccountGroupName);


                var purchase = new PurchaseMaster
                {
                    VoucherNo = input.VoucherNo,
                    VoucherNumbering = 0,
                    Date = DateTime.Now,
                    DateMiti = input.DateMiti,
                    CreditDate = DateTime.Now,
                    VendorInvoiceNo = input.VendorInvoiceNo,
                    //VendorInvoiceDate = DateTime.Now,
                    CreditPeriod = 0,
                    //InvoiceTypeEnum = 0,
                    Narration = "",
                    TotalTax = (decimal)input.TaxAmount,
                    TotalTaxableAmount = (decimal)input.TaxableAmount,
                    //PpdNo = "",

                    TotalAmount = (decimal)input.TotalAmount,
                    BillDiscount = (decimal)input.BillDiscount,
                    GrandTotal = (decimal)input.GrandTotal,
                    AgainstId = 0,
                    LrNo = "",
                    //TransportationCompany = "",
                    VoucherTypeId = voucherTypeId,
                    PurchaseAccountId = Guid.Empty,
                    FinancialYearId = financialYearId,
                    LedgerId = ledgerId,
                    PurchaseOrderMasterId = null,
                    CreateUserId = null,
                    UpdateUserId = null,
                    TenantId = tenantId,
                    PostingNumbering = null
                };
                await purchaseMasterRepository.InsertAsync(purchase);
            }
        }
    }
}
