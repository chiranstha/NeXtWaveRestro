using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.UI;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using NextWave.Erp.Sales.Dtos;
using NextWave.Erp.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Importing
{

    internal class ImportSalesMasterToExcelJob(
        IUnitOfWorkManager unitOfWorkManager,
        IBinaryObjectManager binaryObjectManager,
        ISalesMasterListExcelDataReader salesMasterListExcelDataReader,
        IRepository<SalesMaster, Guid> salesMasterRepository,
        IRepository<SalesDetail, Guid> salesDetailRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Branch, Guid> branchRepository,
        IRepository<AccountLedger, Guid> ledgerRepository,
        IRepository<VoucherType, Guid> voucherTypeRepository,
        IRepository<FinancialYear, Guid> financialYearRepository)
        : BackgroundJob<ImportUniversalFromExcelJobArgs>, ITransientDependency
    {
        private readonly IRepository<Product, Guid> _productRepository = productRepository;
        private readonly IRepository<SalesDetail, Guid> _salesDetailRepository = salesDetailRepository;

        public override void Execute(ImportUniversalFromExcelJobArgs args)
        {
            var purchase = GetSalesMasterListFromExcelOrNull(args);

            CreateSalesMaster(args, purchase);
        }

        private List<GetSalesMasterForImport> GetSalesMasterListFromExcelOrNull(ImportUniversalFromExcelJobArgs args)
        {
            using (var uow = unitOfWorkManager.Begin())
            {
                using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                {
                    try
                    {
                        var file = AsyncHelper.RunSync(() => binaryObjectManager.GetOrNullAsync(args.BinaryObjectId));
                        return salesMasterListExcelDataReader.GetSalesMasterFromExcel(file.Bytes);
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
        }

        private void CreateSalesMaster(ImportUniversalFromExcelJobArgs args,
            List<GetSalesMasterForImport> salesMasters)
        {
            var salesList = new List<GetSalesMasterForImport>();
            foreach (var sales in salesMasters)
                using (var uow = unitOfWorkManager.Begin())
                {
                    using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                    {
                        try
                        {
                            AsyncHelper.RunSync(() => CreateSalesMasterAsync(sales));
                        }
                        catch (UserFriendlyException exception)
                        {
                            sales.Exception = exception.Message;
                            salesList.Add(sales);
                        }
                        catch (Exception exception)
                        {
                            sales.Exception = exception.ToString();
                            salesList.Add(sales);
                        }
                    }

                    uow.Complete();
                }
        }

        private async Task CreateSalesMasterAsync(GetSalesMasterForImport input)
        {
            var voucherNo = await salesMasterRepository.FirstOrDefaultAsync(x => x.VoucherNo == input.VoucherNo);

            if (voucherNo == null)
            {
                var tenantId = CurrentUnitOfWork.GetTenantId();
                var branchId = (await branchRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId)).Id;
                var ledgerId = (await ledgerRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId)).Id;
                var voucherTypeId = (await voucherTypeRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId)).Id;
                var financialYearId = (await financialYearRepository.FirstOrDefaultAsync(x => x.TenantId == tenantId)).Id;

                var sales = new SalesMaster
                {
                    VoucherNumbering = 0,
                    TenantId = tenantId,
                    VoucherNo = input.VoucherNo,
                    SalesAccountId = Guid.Empty,
                    Date = default,
                    DateMiti = input.DateMiti,
                    CreditPeriod = 0,
                    CreditDate = default,
                    Description = null,
                    TaxAmount = (decimal)input.TaxAmount,
                    BillDiscount = (decimal)input.BillDiscount,
                    GrandTotal = (decimal)input.GrandTotal,
                    GrossAmount = (decimal)input.TotalAmount,
                    TaxableAmount = (decimal)input.TaxAmount,
                    IsPrint = false,
                    NoOfPrint = 0,
                    NetAmount = 0,
                    SyncwithIrd = false,
                    PrintedTime = null,
                    IsRealTime = false,
                    PaymentMethod = PaymentMethod.Cash,
                    PaymentMethodLedgerId = null,
                    IsDelete = false,
                    VatRefundAmount = null,
                    LrNo = null,
                    VehicleNo = null,
                    AgainstId = null,
                    SalesModeType = SalesModeType.Na,
                    AgainstVoucherNo = null,
                    InvoiceType = InvoiceTypeEnum.LocalInvoice,
                    PrintUserId = 0,
                    VoucherTypeId = voucherTypeId,
                    LedgerName = null,
                    VatNo = null,
                    LedgerId = ledgerId,
                    ServiceDeliveryId = null,
                    FinancialYearId = financialYearId,
                    CreateUserId = null,
                    UpdateUserId = null,
                    PostingNumbering = null
                };
                await salesMasterRepository.InsertAsync(sales);
            }
        }
    }
}
