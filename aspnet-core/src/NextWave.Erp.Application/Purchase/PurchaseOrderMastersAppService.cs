using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Linq.Extensions;
using Abp.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Common;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.ControlPanel.Documents;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Purchase.Exporting;
using NextWave.Erp.Purchase.Pdf;
using QuestPDF.Fluent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase
{
    [Audited]
    [AbpAuthorize(AppPermissions.PagesPurchaseOrderMasters)]
    public class PurchaseOrderMastersAppService(
        IRepository<PurchaseOrderMaster, Guid> purchaseOrderMasterRepository,
        IRepository<User, long> userRepository,
        IPurchaseOrderMastersExcelExporter purchaseOrderMastersExcelExporter,
        ProductAppService productAppService,
        IRepository<VoucherPhotos, Guid> voucherPhotosRepository,
        IRepository<VoucherType, Guid> voucherTypeRepository,
        IRepository<Branch, Guid> branchRepository,
     //   IRepository<UnitConversion, Guid> unitConversionRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<PurchaseOrderDetails, Guid> purchaseOrderDetailRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<AccountLedger, Guid> accountLedgerRepository,
        IRepository<PurchaseMaster, Guid> purchaseMasterRepository,
        IDocumentsAppService documentsAppService
        //IAppNotifier appNotifier,
        //UserManager userManager,
        //IRepository<UserBranch, Guid> userBranchRepository
        )
        : ErpAppServiceBase, IPurchaseOrderMastersAppService
    {
        [DisableAuditing]
        public async Task<PagedResultDto<GetPurchaseOrderMasterForViewDto>> GetAll(
            GetAllUniversalMastersInput input)
        {
            if (!string.IsNullOrEmpty(input.Filter))
                input.Filter = input.Filter.Trim();
            var filteredPurchaseOrderMasters = purchaseOrderMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking().Include(e => e.AccountLedgerFk)
                .Where(x => x.FinancialYearId == FinancialYearId && x.TenantId == AbpSession.TenantId)
                .WhereIf(!string.IsNullOrEmpty(input.Filter),
                    x => x.AccountLedgerFk.Name.Contains(input.Filter) || x.VoucherNo.Contains(input.Filter))
                .Select(x => new
                {
                    x.Id,
                    x.VoucherNo,
                    x.Date,
                    x.DateMiti,
                    x.DueDate,
                    x.Cancelled,
                    x.Description,
                    x.TotalAmount,
                    x.VoucherTypeId,
                    x.CreateUserId,
                    x.UpdateUserId,
                    x.LedgerId,
                    x.VoucherNumbering,
                    LedgerName = x.AccountLedgerFk.Name
                });
            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredPurchaseOrderMasters = filteredPurchaseOrderMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredPurchaseOrderMasters = filteredPurchaseOrderMasters.Where(x => x.Date <= date);
            }

            var pagedAndFilteredPurchaseOrderMasters = filteredPurchaseOrderMasters
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.VoucherNumbering)
                .PageBy(input);
            var purchaseMasters =
                (await purchaseMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x => x.PurchaseOrderMasterId != null).Select(x => x.PurchaseOrderMasterId).ToListAsync())
                .Distinct().ToList();
            var purchaseOrderMasters = from o in pagedAndFilteredPurchaseOrderMasters
                                       join o6 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                           o.CreateUserId equals o6.Id into j6
                                       from s6 in j6.DefaultIfEmpty()
                                       join o5 in userRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking() on
                                           o.UpdateUserId equals o5.Id into j5
                                       from s5 in j5.DefaultIfEmpty()
                                       select new GetPurchaseOrderMasterForViewDto
                                       {
                                           VoucherNo = o.VoucherNo,
                                           DateMiti = o.DateMiti,
                                           Cancelled = o.Cancelled,
                                           Description = o.Description,
                                           TotalAmount = o.TotalAmount,
                                           PurchaseStatus = purchaseMasters.Contains(o.Id) ? PurchaseStatus.Complete : PurchaseStatus.Pending,
                                           RemainingDays = (o.DueDate - DateTime.Today).Value.Days,
                                           Id = o.Id,
                                           CreateUser = s6 == null || s6.Name == null ? "" : s6.Name,
                                           UpdateUser = s5 == null || s5.Name == null ? "" : s5.Name,
                                           LedgerId = o.LedgerId,
                                           LedgerName = o.LedgerName
                                       };

            var totalCount = await filteredPurchaseOrderMasters.CountAsync();

            return new PagedResultDto<GetPurchaseOrderMasterForViewDto>(
                totalCount,
                await purchaseOrderMasters.ToListAsync()
            );
        }

        public async Task<GetPurchaseOrderMasterForViewDto> GetPurchaseOrderMasterForView(Guid id)
        {
            var purchaseOrderMaster = (await purchaseOrderMasterRepository.GetAll()
                    .Where(x => x.Id == id).Include(x => x.AccountLedgerFk).ToListAsync())
                .FirstOrDefault();

            var output = new GetPurchaseOrderMasterForViewDto
            {
                Id = purchaseOrderMaster.Id,
                VoucherNo = purchaseOrderMaster.VoucherNo,
                Cancelled = purchaseOrderMaster.Cancelled,
                Description = purchaseOrderMaster.Description,
                TotalAmount = purchaseOrderMaster.TotalAmount,
                LedgerId = purchaseOrderMaster.LedgerId,
                DateMiti = purchaseOrderMaster.DateMiti,
                LedgerName = purchaseOrderMaster.AccountLedgerFk.Name,
                DueDateMiti = purchaseOrderMaster.DueDateMiti
            };

            if (output.LedgerId != null)
            {
                var accountLedger =
                    await accountLedgerRepository.FirstOrDefaultAsync(output.LedgerId);
                output.LedgerName = accountLedger?.Name;
            }

            output.PurchaseOrderDetails =
                (await purchaseOrderDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x => x.PurchaseOrderMasterId == id).Include(x => x.ProductFk).Include(x => x.UnitFk)
                    .ToListAsync())
                .Select(x => new PurchaseOrderDetailsForViewDto
                {
                    Id = x.Id,
                    Qty = x.Qty,
                    Rate = x.Rate,
                    ProductCode = x.ProductCode,
                    Amount = x.Amount,
                    ProductId = x.ProductId,
                    ProductName = x.ProductFk.Name,
                    UnitId = x.UnitId,
                    UnitName = x.UnitFk.Name
                }).ToList();
            return output;
        }

        [AbpAuthorize(AppPermissions.PagesPurchaseOrderMastersEdit)]
        public async Task<GetPurchaseOrderMasterForEditOutput> GetPurchaseOrderMasterForEdit(EntityDto<Guid> input)
        {
            var purchaseOrderMaster = (await purchaseOrderMasterRepository.GetAll().Where(x => x.Id == input.Id).Include(x => x.AccountLedgerFk).ToListAsync()).FirstOrDefault();

            if (purchaseOrderMaster == null)
                throw new UserFriendlyException("Purchase Order not found");

            var output = new GetPurchaseOrderMasterForEditOutput
            {
                Id = purchaseOrderMaster.Id,
                VoucherNo = purchaseOrderMaster.VoucherNo,
                DateMiti = purchaseOrderMaster.DateMiti,
                DueDateMiti = purchaseOrderMaster.DueDateMiti,
                Description = purchaseOrderMaster.Description,
                TotalAmount = purchaseOrderMaster.TotalAmount,
                LedgerName = purchaseOrderMaster.AccountLedgerFk.Name,
                LedgerId = purchaseOrderMaster.LedgerId,
                PurchaseOrderDetails = (await purchaseOrderDetailRepository.GetAll()
                    .Where(x => x.TenantId == AbpSession.TenantId)
                    .Where(x => x.PurchaseOrderMasterId == input.Id).Include(x => x.ProductFk)
                    .ToListAsync()).Select(x => new PurchaseOrderDetailsDto
                    {
                        Id = x.Id,
                        Qty = x.Qty,
                        Rate = x.Rate,
                        ProductCode = x.ProductCode,
                        Amount = x.Amount,
                        ProductName = x.ProductFk.Name,
                        ProductId = x.ProductId,
                        UnitId = x.UnitId
                    }).ToList()
            };

            return output;
        }

        public async Task<Guid> CreateOrEdit(CreateOrEditPurchaseOrderMasterDto input)
        {
            var date = DateConverter.ConvertToEnglish(input.DateMiti);
            if (FinancialYear.FromDate > date)
                throw new UserFriendlyException(
                    $"Select Financial Year {DateConverter.ConvertToNepali(FinancialYear.FromDate)}");
            if (FinancialYear.ToDate < date)
                throw new UserFriendlyException(
                    $"Select Financial Year {DateConverter.ConvertToNepali(FinancialYear.ToDate)}");
            if (input.Id == null || input.Id == Guid.Empty)
                return await Create(input);
            return await Update(input);
        }

        [AbpAuthorize(AppPermissions.PagesPurchaseOrderMastersDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var purchaseOrder = await purchaseOrderMasterRepository.FirstOrDefaultAsync(x => x.Id == input.Id);
            if (await purchaseMasterRepository.CountAsync(x => x.PurchaseOrderMasterId == input.Id) > 0)
                throw new UserFriendlyException("Purchase Master Reference Exists");


            await purchaseOrderDetailRepository.DeleteAsync(x => x.PurchaseOrderMasterId == input.Id);
            await purchaseOrderMasterRepository.DeleteAsync(input.Id);
        }

        public async Task<FileDto> GetPurchaseOrderMastersToExcel(GetAllUniversalMastersInput input)
        {
            var filteredPurchaseOrderMasters = purchaseOrderMasterRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Include(e => e.AccountLedgerFk)
                .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), e => false || e.Description.Contains(input.Filter));
            if (input.FromMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.FromMiti);
                filteredPurchaseOrderMasters = filteredPurchaseOrderMasters.Where(x => x.Date >= date);
            }

            if (input.ToMiti != null)
            {
                var date = DateConverter.ConvertToEnglish(input.ToMiti);
                filteredPurchaseOrderMasters = filteredPurchaseOrderMasters.Where(x => x.Date <= date);
            }

            var query = from o in filteredPurchaseOrderMasters

                        join o2 in accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId) on o.LedgerId
                            equals o2.Id into j2
                        from s2 in j2.DefaultIfEmpty()
                        select new GetPurchaseOrderMasterForViewDto
                        {
                            VoucherNo = o.VoucherNo,
                            Cancelled = o.Cancelled,
                            Description = o.Description,
                            TotalAmount = o.TotalAmount,
                            Id = o.Id,
                            LedgerName = s2 == null || s2.Name == null ? "" : s2.Name
                        };

            var purchaseOrderMasterListDtos = await query.ToListAsync();
            return purchaseOrderMastersExcelExporter.ExportToFile(purchaseOrderMasterListDtos);
        }


        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseOrderMasters)]
        public async Task<List<PurchaseOrderMasterAccountLedgerTableDto>> GetAllAccountLedgerForTableDropdown()
        {
            return await accountLedgerRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId && !x.IsDelete)
                .Include(x => x.AccountGroupFk)
                .Where(x => x.AccountGroupFk.Name == "Sundry Creditors" || x.AccountGroupFk.Name == "Sundry Debtors" ||
                            x.AccountGroupFk.Name == "Cash-in Hand")
                .Select(accountLedger => new PurchaseOrderMasterAccountLedgerTableDto
                {
                    Id = accountLedger.Id,
                    DisplayName = accountLedger.Name,
                    PanNo = accountLedger.Pan,
                    MobileNo = accountLedger.Phone,
                    Address = accountLedger.Address
                }).ToListAsync();
        }


        public async Task<PdfForPurchaseOrderModelNew> GetPurchaseOrderForPdf(Guid id)
        {
            var purchaseOrder = await purchaseOrderMasterRepository.FirstOrDefaultAsync(x => x.Id == id);
            if (purchaseOrder != null)
            {
                var branchData = await branchRepository.GetAll().Include(x => x.BranchFk).FirstOrDefaultAsync();
                var result = new PdfForPurchaseOrderModelNew
                {
                    BranchName = branchData.BranchFk == null ? branchData.Name : branchData.BranchFk.Name,
                    BranchPhone = branchData.BranchFk == null ? branchData.PhoneNo1 : branchData.BranchFk.PhoneNo1,
                    Date = purchaseOrder.Date,
                    DateMiti = purchaseOrder.DateMiti,
                    Logo1 = branchData.Image1,
                    Tin = 0,
                    VoucherTypeName =
                        (await voucherTypeRepository.FirstOrDefaultAsync(x => x.Id == purchaseOrder.VoucherTypeId)).Name,
                    OrderNo = purchaseOrder.VoucherNo,
                    LedgerName =
                        (await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == purchaseOrder.LedgerId)).Name,
                    CustomerAddress =
                        (await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == purchaseOrder.LedgerId)).Address,
                    CustomerPan = (await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == purchaseOrder.LedgerId))
                        .Pan,
                    CustomerPhone = (await accountLedgerRepository.FirstOrDefaultAsync(x => x.Id == purchaseOrder.LedgerId))
                        .Phone,
                    PurchaseOrderDetail = null,
                    BranchContact = branchData.PhoneNo1 + " / " + branchData.PhoneNo2,
                    BranchAddress = branchData.Address,
                    Pan = branchData.PANumber,
                    TotalAmountInWord = CurrencyToAmount.NumberToText((int)purchaseOrder.TotalAmount),
                    TotalAmount = purchaseOrder.TotalAmount,
                    Description = purchaseOrder.Description,
                    InvoiceName = "Purchase Order",
                    ApprovedBy = null,
                    ReceivedBy = null
                };
                var serial = 1;
                result.PurchaseOrderDetail =
                    (await purchaseOrderDetailRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                        .Where(x => x.PurchaseOrderMasterId == id)
                        .Include(x => x.ProductFk).Include(x => x.UnitFk).ToListAsync()).Select(x =>
                        new PdfForPurchaseOrderDetailModel
                        {
                            SlNo = serial++,
                            ProductName = x.ProductFk.Name,
                            HsCode = x.ProductFk.HsCode,
                            Quantity = x.Qty,
                            Unit = x.UnitFk.Name,
                            Rate = x.Rate,
                            Amount = x.Amount
                        }).ToList();

                return result;
            }

            throw new UserFriendlyException("Data not found");
        }

        //[AbpAuthorize(AppPermissions.PagesPurchaseOrderMastersPrint)]
        //public async Task<byte[]> GetPdfdownload(Guid id)
        //{
        //    var model = await GetPurchaseOrderForPdf(id);
        //    var list = new List<PdfForPurchaseOrderModelNew> { model };
        //    var document = new PurchaseOrderNewPdf(list);
        //    return document.GeneratePdf();
        //}

        public async Task<bool> GetCheckVoucherNo(string voucherNo)
        {
            return await purchaseOrderMasterRepository.CountAsync(x => x.FinancialYearId == FinancialYearId && x.VoucherNo == voucherNo) > 0;
        }

        public async Task CreateOrUpdateFile(string voucherNo, IFormFile file)
        {
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseOrder");
            var purchaseOrderMaster = await purchaseOrderMasterRepository.FirstOrDefaultAsync(x =>
                x.VoucherNo == voucherNo && x.FinancialYearId == FinancialYearId && x.VoucherTypeId == voucherTypeId);
            var input = new CreateOrEditDocumentDto
            {
                VoucherTypeId = purchaseOrderMaster.VoucherTypeId,
                VoucherNo = purchaseOrderMaster.VoucherNo,
            };
            await documentsAppService.Create(input);
        }

        [DisableAuditing]
        public async Task<List<UnitConversionServiceDto>> GetAllUnits()
        {
            return await unitRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(unit => new UnitConversionServiceDto
                {
                    UnitId = unit.Id,
                    UnitName = unit.Name == null ? "" : unit.Name.ToString(),
                    Rate = 0
                }).ToListAsync();
        }

        public async Task<List<PurchaseOrderMasterDto>> GetPurchaseOrderByFilter(DateTime? fromdate, DateTime? toDate, Guid? partyId, Guid? orderNo)
        {
            var query = purchaseOrderMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId);
            if (fromdate != null) query = query.Where(x => x.Date >= fromdate);

            if (toDate != null) query = query.Where(x => x.Date <= toDate);


            if (partyId != null) query = query.Where(x => x.LedgerId == partyId);

            if (orderNo != null) query = query.Where(x => x.Id == orderNo);

            return (await query.ToListAsync()).Select(x => new PurchaseOrderMasterDto
            {
                Id = x.Id,
                VoucherNo = x.VoucherNo,
                Date = x.Date,
                DueDate = x.DueDate,
                Cancelled = x.Cancelled,
                Description = x.Description,
                TotalAmount = x.TotalAmount,
                VoucherTypeId = x.VoucherTypeId,
                LedgerId = x.LedgerId
            }).ToList();
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseOrderMasters)]
        public async Task<List<UniversalDropdownDto>> GetAllProductForTableDropdown()
        {
            return await productRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Select(product => new UniversalDropdownDto
                {
                    Id = product.Id,
                    DisplayName = product.Name == null ? "" : product.Name.ToString()
                }).ToListAsync();
        }

        [DisableAuditing]
        public async Task<List<PurchaseOrderMasterProductListDto>> GetAllProduct()
        {
            var products = await productRepository.GetAll().Include(x => x.UnitFk).Where(x => x.TenantId == AbpSession.TenantId).ToListAsync();

            return products.Select(product => new PurchaseOrderMasterProductListDto
            {
                Id = product.Id,
                ProductName = product.Name,
                UnitId = product.UnitId,
                Rate = product.SalesRate,
                UnitsList = new List<PurchaseReturnUnitsQtyDto>()
                {
                    new PurchaseReturnUnitsQtyDto
                    {
                        Qty = 0,
                        ProductId = product.Id,
                        UnitId = product.UnitId,
                        UnitName = product.UnitFk.Name,
                        Rate = product.PurchaseRate
                    }
                }
            }).ToList();
                
            //    unitConversionRepository.GetAll()
            //            .Include(x => x.UnitFk)
            //            .Where(x => x.TenantId == AbpSession.TenantId)
            //            .Where(x => x.ProductId == product.Id)
            //            .ToList()
            //            .Select(x => new PurchaseReturnUnitsQtyDto
            //            {
            //                Qty = 0,
            //                ProductId = x.ProductId,

            //                UnitId = x.UnitId,
            //                UnitName = x.UnitFk.Name,
            //                Rate = product.PurchaseRate * x.PrimaryQty / x.Qty
            //            })
            //            .ToList()
            //})
            //    .ToList();
        }

        public async Task<GetProductForViewDto> GetProductForView(Guid productId)
        {
            return await productAppService.GetProductForView(productId);
        }

        [DisableAuditing]
        [AbpAuthorize(AppPermissions.PagesPurchaseOrderMasters)]
        public async Task<List<UnitConversionServiceDto>> GetAllUnitsByProductId(Guid productId)
        {
            var product = (await productRepository.GetAll()
                .Include(x => x.UnitFk).Where(x => x.Id == productId).ToListAsync()).FirstOrDefault();

            var result = new List<UnitConversionServiceDto>
            {
                new UnitConversionServiceDto
                {
                    UnitId = product.UnitId,
                    UnitName = product.UnitFk.Name,
                    Rate = product.PurchaseRate
                }
            };
            return result;

            //if (product == null)
            //    throw new UserFriendlyException("Product of id : " + productId + " not found");
            //var result = UnitConversionManager.GetAllUnitConversions(productId, product.PurchaseRate);
            //return result;
        }

        protected async Task<int> GetInlineVoucherNo()
        {
            var data = await purchaseOrderMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            if (data.Count == 0)
            {
                var voucherNumbering =
                    await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PurchaseOrder");
                return voucherNumbering.StartIndex;
            }

            return data.Max() + 1;
        }

        public async Task<string> GetVoucherGenerateType()
        {
            return await VoucherTypeManager.GetVoucherGenerateType(FinancialYearId, "PurchaseOrder");
        }

        [AbpAuthorize(AppPermissions.PagesPurchaseOrderMastersCreate)]
        protected virtual async Task<Guid> Create(CreateOrEditPurchaseOrderMasterDto input)
        {
            using var unitOfWork = unitOfWorkManager.Begin();
            if (await purchaseOrderMasterRepository.CountAsync(x =>
                    x.VoucherNo == input.VoucherNo && x.FinancialYearId == FinancialYearId) > 0)
                throw new UserFriendlyException("Voucher number already exist");
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseOrder");
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;
            var postingNumbering = PostingNumbering;

            var voucherNumbering = 0;
            var voucherNo = string.Empty;
            if (await GetVoucherGenerateType() == "Automatic")
            {
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = await GetPurchaseOrderVoucherNo();
            }

            if (await GetVoucherGenerateType() == "Manually")
            {
                if (await purchaseOrderMasterRepository.CountAsync(x =>
                        x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                    0) throw new UserFriendlyException("PurchaseOrder VoucherNo is Duplicate");
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = input.VoucherNo;
            }

            if (await GetVoucherGenerateType() == "Duplicate")
            {
                voucherNumbering = await GetInlineVoucherNo();
                voucherNo = input.VoucherNo;
            }

            var purchaseOrderMaster = new PurchaseOrderMaster
            {
                TenantId = tenantId,
                VoucherNumbering = voucherNumbering,
                VoucherNo = voucherNo,
                Date = DateConverter.ConvertToEnglish(input.DateMiti),
                DateMiti = input.DateMiti,
                DueDate = DateConverter.ConvertToEnglish(input.DueDateMiti),
                DueDateMiti = input.DueDateMiti,
                Cancelled = false,
                IsCompleted = false,
                Description = input.Description,
                TotalAmount = input.TotalAmount,
                VoucherTypeId = voucherTypeId,
                FinancialYearId = FinancialYearId,
                LedgerId = input.LedgerId,
                CreateUserId = AbpSession.UserId,
                UpdateUserId = null,
                PostingNumbering = postingNumbering
            };

            var masterId = await purchaseOrderMasterRepository.InsertAndGetIdAsync(purchaseOrderMaster);

            foreach (var purchaseOrderDetail in input.PurchaseOrderDetails)
            {
                var purchaseOrder = new PurchaseOrderDetails
                {
                    TenantId = tenantId,
                    Qty = purchaseOrderDetail.Qty ?? 0,
                    ProductCode = purchaseOrderDetail.ProductCode,
                    Rate = purchaseOrderDetail.Rate ?? 0,
                    Amount = purchaseOrderDetail.Amount ?? 0,
                    PurchaseOrderMasterId = masterId,
                    ProductId = purchaseOrderDetail.ProductId,
                    UnitId = purchaseOrderDetail.UnitId
                };
                await purchaseOrderDetailRepository.InsertAsync(purchaseOrder);
            }

            await unitOfWork.CompleteAsync();
            return masterId;
        }

        [AbpAuthorize(AppPermissions.PagesPurchaseOrderMastersEdit)]
        protected virtual async Task<Guid> Update(CreateOrEditPurchaseOrderMasterDto input)
        {
            var tenantId = AbpSession.TenantId;
            if (AbpSession.TenantId != null) tenantId = AbpSession.TenantId;
            var purchaseOrderMaster = await purchaseOrderMasterRepository.FirstOrDefaultAsync(e => e.Id == input.Id);
            if (purchaseOrderMaster == null) return (Guid)input.Id;
            if (await GetVoucherGenerateType() == "Manually")
            {
                if (await purchaseOrderMasterRepository.CountAsync(x =>
                        x.Id != input.Id && x.FinancialYearId == FinancialYearId && x.VoucherNo == input.VoucherNo) >
                    0) throw new UserFriendlyException("PurchaseOrder VoucherNo is Duplicate");
                purchaseOrderMaster.VoucherNo = input.VoucherNo;
            }

            if (await GetVoucherGenerateType() == "Duplicate")
                purchaseOrderMaster.VoucherNo = input.VoucherNo;
            purchaseOrderMaster.Description = input.Description;
            purchaseOrderMaster.LedgerId = input.LedgerId;
            purchaseOrderMaster.TotalAmount = input.TotalAmount;
            purchaseOrderMaster.DueDate = DateConverter.ConvertToEnglish(input.DueDateMiti);
            purchaseOrderMaster.DueDateMiti = input.DueDateMiti;
            purchaseOrderMaster.Cancelled = input.Cancelled;
            purchaseOrderMaster.Date = DateConverter.ConvertToEnglish(input.DateMiti);
            purchaseOrderMaster.DateMiti = input.DateMiti;
            purchaseOrderMaster.UpdateUserId = AbpSession.UserId;
            await purchaseOrderMasterRepository.UpdateAsync(purchaseOrderMaster);

            var detailsIds = input.PurchaseOrderDetails.Select(x => x.Id).ToList();
            var detailsDataBaseIds = await purchaseOrderDetailRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.PurchaseOrderMasterId == input.Id).Select(x => x.Id).ToListAsync();

            foreach (var detailsDataBaseId in detailsDataBaseIds.Where(detailsDataBaseId =>
                         !detailsIds.Contains(detailsDataBaseId)))
                await purchaseOrderDetailRepository.DeleteAsync(detailsDataBaseId);

            foreach (var detail in input.PurchaseOrderDetails)
                if (detail.Id == Guid.Empty)
                {
                    var purchaseOrder = new PurchaseOrderDetails
                    {
                        TenantId = tenantId,
                        Qty = detail.Qty ?? 0,
                        Rate = detail.Rate ?? 0,
                        ProductCode = detail.ProductCode,
                        Amount = detail.Amount ?? 0,
                        PurchaseOrderMasterId = purchaseOrderMaster.Id,
                        ProductId = detail.ProductId,
                        UnitId = detail.UnitId
                    };
                    await purchaseOrderDetailRepository.InsertAsync(purchaseOrder);
                }
                else
                {
                    var data = await purchaseOrderDetailRepository.FirstOrDefaultAsync(x =>
                        x.TenantId == tenantId && x.Id == detail.Id);
                    if (data == null) continue;
                    {
                        data.Qty = detail.Qty ?? 0;
                        data.ProductCode = detail.ProductCode;
                        data.Rate = detail.Rate ?? 0;
                        data.Amount = detail.Amount ?? 0;
                        data.ProductId = detail.ProductId;
                        data.UnitId = detail.UnitId;
                        await purchaseOrderDetailRepository.UpdateAsync(data);
                    }
                }

            return (Guid)input.Id;
        }

        public async Task<string> GetPurchaseOrderVoucherNo()
        {
            var data = await purchaseOrderMasterRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.FinancialYearId == FinancialYearId)
                .Select(x => x.VoucherNumbering).ToListAsync();
            var voucherNumbering = await VoucherTypeManager.GetVoucherNumbering(FinancialYearId, "PurchaseOrder");
            if (data.Count == 0) return voucherNumbering.Prefix + voucherNumbering.StartIndex + voucherNumbering.Postfix;

            return voucherNumbering.Prefix + (data.Max() + 1) + voucherNumbering.Postfix;
        }

        public async Task UploadImageNew(IFormFile file, Guid purchaseOrderId)
        {
            var purchaseOrder = await purchaseOrderMasterRepository.FirstOrDefaultAsync(x => x.Id == purchaseOrderId);
            var tenantId = AbpSession.TenantId;


            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("PurchaseOrder");

            var fileType = Path.GetExtension(file.FileName);
            if (fileType != ".jpg" && fileType != ".jpeg" && fileType != ".png" && fileType != ".pdf")
                throw new UserFriendlyException($"This filetype '{fileType}' is not accepted.");

            var photo = new VoucherPhotos
            {
                VoucherNo = purchaseOrder.VoucherNo,
                VoucherNumbering = purchaseOrder.VoucherNumbering,
                FileName = file.FileName,
                FileType = fileType,
                FinancialYearId = FinancialYearId,
                VoucherTypeId = voucherTypeId,
                TenantId = tenantId
            };
            var id = await voucherPhotosRepository.InsertAndGetIdAsync(photo);

            var fileDetails = await voucherPhotosRepository.FirstOrDefaultAsync(x => x.Id == id);
            if (file == null) throw new UserFriendlyException("Please select upload file");

            var changedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            const long kb = 1000;
            switch (file.Length)
            {
                case > 500 * kb:
                    throw new UserFriendlyException("File size is large.");
                case > 0:
                    {
                        using var ms = new MemoryStream();
                        file.CopyTo(ms);
                        var fileBytes = ms.ToArray();
                        fileDetails.Image = fileBytes;
                        break;
                    }
            }

            fileDetails.ChangedFileName = changedFileName;
            await voucherPhotosRepository.UpdateAsync(fileDetails);
        }

        [DisableAuditing]
        public async Task<List<DocumentDetailsDto>> GetAllDocuments(Guid purchaseOrderId)
        {
            var purchaseOrder = await purchaseOrderMasterRepository.FirstOrDefaultAsync(purchaseOrderId);
            if (purchaseOrder == null)
                throw new UserFriendlyException("Data not found");
            var result = (await voucherPhotosRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.VoucherNo == purchaseOrder.VoucherNo &&
                            x.VoucherNumbering == purchaseOrder.VoucherNumbering &&
                            x.VoucherTypeId == purchaseOrder.VoucherTypeId &&
                            x.FinancialYearId == purchaseOrder.FinancialYearId).Include(x => x.VoucherTypeFk).ToListAsync()).Select(x =>
                new DocumentDetailsDto
                {
                    Id = x.Id,
                    FileName = x.FileName,
                    FileType = x.FileType,
                    VoucherNo = x.VoucherNo,
                    VoucherType = x.VoucherTypeFk.Name,
                    Image = null /*x.Image*/
                }).ToList();
            return result;
        }

        public async Task<DocumentDetailsDto> GetImage(Guid id)
        {
            var data = (await voucherPhotosRepository.GetAll().Where(x => x.TenantId == AbpSession.TenantId)
                .Where(x => x.Id == id).Include(x => x.VoucherTypeFk).ToListAsync()).FirstOrDefault();
            var result = new DocumentDetailsDto
            {
                Id = data.Id,
                FileName = data.FileName,
                FileType = data.FileType,
                VoucherNo = data.VoucherNo,
                Image = data.Image
            };
            return result;
        }

        public async Task DeleteFile(Guid id)
        {
            await voucherPhotosRepository.DeleteAsync(id);
        }

        [AbpAuthorize(AppPermissions.PagesPurchaseOrderMastersPrint)]
        public async Task<byte[]> GetPdfdownload(Guid id)
        {
            var model = await GetPurchaseOrderForPdf(id);
            var list = new List<PdfForPurchaseOrderModelNew> { model };
            var document = new PurchaseOrderNewPdf(list);
            return document.GeneratePdf();
        }
    }
}
