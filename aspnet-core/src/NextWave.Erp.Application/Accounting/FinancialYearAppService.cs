using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.EntityFrameworkCore.Repositories;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NepDate;
using NextWave.Erp.Accounting.Dtos;
using NextWave.Erp.Accounting.Exporting;
using NextWave.Erp.Authorization;
using NextWave.Erp.Authorization.BranchUser;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Configuration;
using NextWave.Erp.Dto;
using NextWave.Erp.Enums;
using NextWave.Erp.FinancialStatement;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.GeneralSetting.Dtos;
using NextWave.Erp.Inventory;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;


namespace NextWave.Erp.Accounting
{

    [Audited]
    [AbpAuthorize(AppPermissions.PagesFinancialYears)]
    public class FinancialYearsAppService(
        IRepository<FinancialYear, Guid> financialYearRepository,
        IFinancialYearsExcelExporter financialYearsExcelExporter,
        IRepository<AccountLedger, Guid> accountLedgerRepository,
        IRepository<LedgerPosting, Guid> ledgerPostingRepository,
        UserManager userManager,
        IRepository<UserBranch, Guid> userBranchRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<StockPosting, Guid> stockPostingRepository,
        IRepository<UnitConversion, Guid> unitConversionRepository,
        IRepository<FinancialYearSelect, Guid> financialYearSelectRepository,
        IRepository<AccountGroup, Guid> accountGroupRepository,
        IRepository<NewPartyBalance, Guid> newPartyBalanceRepository,
        IRepository<VoucherNumbering, Guid> voucherNumberingRepository)
        : ErpAppServiceBase, IFinancialYearsAppService
    {
        private readonly ILogger<FinancialYearsAppService> _logger;
        protected List<UniversalDropdownDto> AccountLedgerList = [];
        protected List<FinancialStatementDto> ClStockReports = [];
        protected List<LedgerPostingForReportDto> PostingList = [];

        [DisableAuditing]
        public async Task<PagedResultDto<GetFinancialYearForViewDto>> GetAll(GetAllUniversalInput input)
        {
            var currentFinancialYearId = FinancialYearId;
            var filteredFinancialYears = financialYearRepository.GetAll();
            var pagedAndFilteredFinancialYears = filteredFinancialYears
                .OrderBy(input.Sorting ?? "id asc")
                .PageBy(input);

            var financialYears = pagedAndFilteredFinancialYears.Select(o => new GetFinancialYearForViewDto
            {
                Name = o.Name,
                FromDate = o.FromDate,
                ToDate = o.ToDate,
                FromMiti = o.FromMiti,
                ToMiti = o.ToMiti,
                Status = o.Status,
                IsOldYear = o.IsOldYear,
                Active = o.Id == currentFinancialYearId,
                Id = o.Id
            }).OrderBy(x => x.FromDate).ToListAsync();
            return new PagedResultDto<GetFinancialYearForViewDto>(
                financialYears.Result.Count,
                await financialYears
            );
        }

        public async Task<GetFinancialYearForViewDto> GetFinancialYearForView(Guid id)
        {
            var financialYear = await financialYearRepository.GetAsync(id);

            var output = new GetFinancialYearForViewDto
            {
                Id = financialYear.Id,
                FromDate = financialYear.FromDate,
                Name = financialYear.Name,
                ToDate = financialYear.ToDate,
                Active = financialYear.Id == FinancialYearId,
                FromMiti = financialYear.FromMiti,
                ToMiti = financialYear.ToMiti,
                Status = financialYear.Status
            };
            return output;
        }


        [AbpAuthorize(AppPermissions.PagesFinancialYearsEdit)]
        public async Task<GetFinancialYearForEditOutput> GetFinancialYearForEdit(EntityDto<Guid> input)
        {
            var financialYear = await financialYearRepository.FirstOrDefaultAsync(input.Id);

            var output = new GetFinancialYearForEditOutput
            {
                Id = financialYear.Id,
                FromMiti = financialYear.FromMiti,
                FromDate = financialYear.FromDate,
                ToDate = financialYear.ToDate,
                ToMiti = financialYear.ToMiti,
                Status = financialYear.Status,
            };
            return output;
        }

        public async Task CreateOrEdit(CreateOrEditFinancialYearDto input)
        {
            if (input.Id == null || input.Id == Guid.Empty)
                await Create(input);
            else
                await Update(input);
        }

        [AbpAuthorize(AppPermissions.PagesFinancialYearsDelete)]
        public async Task Delete(EntityDto<Guid> input)
        {
            var openingVoucherId = await VoucherTypeManager.GetVoucherTypeId("OpeningStock");
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningBalance");

            if (await financialYearRepository.CountAsync() == 1)
                throw new UserFriendlyException("Can't Delete FinancialYear");
            if (await ledgerPostingRepository.CountAsync(x =>
                    x.VoucherTypeId != voucherTypeId && x.FinancialYearId == input.Id) > 0)
                throw new UserFriendlyException("Can't Delete FinancialYear");

            if (await stockPostingRepository.CountAsync(x =>
                    x.VoucherTypeId != openingVoucherId && x.FinancialYearId == input.Id) > 0)
                throw new UserFriendlyException("Can't Delete FinancialYear");


            var oldFinancialYear = await financialYearRepository.GetAll()
                .Where(x => x.Id != input.Id)
                .OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            if (oldFinancialYear != null)
            {
                oldFinancialYear.IsOldYear = false;
                await financialYearRepository.UpdateAsync(oldFinancialYear);
                await ChangeFinancialYear(oldFinancialYear.Id);
            }

            try
            {
                await voucherNumberingRepository.DeleteAsync(e => e.FinancialYearId == input.Id);
                await ledgerPostingRepository.DeleteAsync(e => e.FinancialYearId == input.Id);
                await stockPostingRepository.DeleteAsync(e => e.FinancialYearId == input.Id);
                await newPartyBalanceRepository.DeleteAsync(e => e.FinancialYearId == input.Id);
                await financialYearRepository.DeleteAsync(input.Id);
            }
            catch (Exception ex)
            {
                var message = "FinancialYear Delete " + ex.Message;
                //  await emailSender.SendErrorAsync("Financial Delete", message);
                throw new UserFriendlyException("Please Contact Support", "Data Can't Delete");
            }
        }

        public async Task<FileDto> GetFinancialYearsToExcel(GetAllUniversalInput input)
        {
            var filteredFinancialYears = financialYearRepository.GetAll();
            var query = from o in filteredFinancialYears
                        select new GetFinancialYearForViewDto
                        {
                            FromDate = o.FromDate,
                            ToDate = o.ToDate,
                            FromMiti = o.FromMiti,
                            ToMiti = o.ToMiti,
                            Status = o.Status,
                            Id = o.Id
                        };

            var financialYearListDtos = await query.ToListAsync();

            return financialYearsExcelExporter.ExportToFile(financialYearListDtos);
        }

        [DisableAuditing]
        public async Task<List<FinancialYearSelectDto>> GetAllFinancialYear()
        {
            var currentFinancialYearId = FinancialYearId;
            var dateselect = await financialYearRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.GetTenantId())
                .OrderByDescending(x => x.FromDate)
                .ToListAsync();
            var list = new List<FinancialYearSelectDto>();
            foreach (var item in dateselect)
            {
                var model = new FinancialYearSelectDto
                {
                    FinancialYearId = item.Id,
                    FinancialYear = item.Name,
                    Active = item.Id == currentFinancialYearId
                };
                list.Add(model);
            }

            return list;
        }

        public async Task ChangeFinancialYear(Guid financialYearId)
        {
            var financialYears = await financialYearRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId)
                .ToListAsync();
            if (financialYears.Count(x => x.Id == financialYearId) == 0)
                throw new UserFriendlyException("Selected Financial Year is Not Valid ❎");

            var dateSelect = await financialYearSelectRepository.GetAll()
                .Where(x => x.Date == DateTime.Today && x.UserId == AbpSession.GetUserId())
                .FirstOrDefaultAsync();
            if (dateSelect == null)
            {
                var model = new FinancialYearSelect
                {
                    Date = DateTime.Today,
                    FinancialYearId = financialYearId,
                    UserId = AbpSession.GetUserId(),
                    TenantId = AbpSession.GetTenantId()
                };
                await financialYearSelectRepository.InsertAsync(model);
            }
            else
            {
                dateSelect.FinancialYearId = financialYearId;
                await financialYearSelectRepository.UpdateAsync(dateSelect);
            }
        }


        [AbpAuthorize(AppPermissions.PagesFinancialYearsSplit)]

        public async Task DataSplit(Guid oldFinancialYearId)
        {
            var oldFinancialYear = await financialYearRepository.FirstOrDefaultAsync(x => x.Id == oldFinancialYearId);
            oldFinancialYear.Status = false;
            oldFinancialYear.IsOldYear = true;
            await financialYearRepository.UpdateAsync(oldFinancialYear);

            var tempFromMiti = DateConverter.ConvertToNepali(oldFinancialYear.ToDate.AddDays(30)).Split('/');
            var newFromMiti = $"{tempFromMiti[0]}/04/01";
            var newFromDate = DateConverter.ConvertToEnglish(newFromMiti);

            if (await financialYearRepository.GetAll().AnyAsync(e => e.FromDate >= newFromDate))
                throw new UserFriendlyException("Data is Already Split");

            var newToDate = newFromDate.AddYears(1);
            var tempToMiti = DateConverter.ConvertToNepali(newToDate).Split('/');
            var newToMiti = $"{tempToMiti[0]}/03/32";
            var financialYear = new FinancialYear
            {
                Id = Guid.Empty,
                TenantId = AbpSession.TenantId,
                Name = newFromMiti.Split('/')[0] + " ➔ " + newToMiti.Split('/')[0].Substring(2, 2),
                FromDate = newFromDate,
                ToDate = newToDate,
                FromMiti = newFromMiti,
                ToMiti = newToMiti,
                Status = true,
                IsOldYear = false
            };
            var newFinancialYearId = await financialYearRepository.InsertAndGetIdAsync(financialYear);
            var objAccountLedger = await accountLedgerRepository.GetAll()
                .Select(x => new { x.Id, x.IsBillByBill })
                .ToListAsync();
            var noTransferGroupId = new List<Guid>();
            var objGroups = await accountGroupRepository.GetAll().AsNoTracking()
                .Where(x => x.Name == "Direct Income" || x.Name == "Indirect Income" ||
                            x.Name == "Indirect Expenses" || x.Name == "Direct Expenses" ||
                            x.Name == "Sales Account" || x.Name == "Purchase Account")
                .Select(x => x.Id)
                .ToListAsync();
            foreach (var objGroup in objGroups) noTransferGroupId.AddRange(await FuncRecursive(objGroup));
            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningBalance");

            var objLedgerPosting = ledgerPostingRepository.GetAll().AsNoTracking()
                .Include(x => x.AccountLedgerFk)
                .ThenInclude(x => x.AccountGroupFk)
                .Where(x => x.FinancialYearId == oldFinancialYearId &&
                            !noTransferGroupId.Contains(x.AccountLedgerFk.AccountGroupId)).ToList();

            var legerPostingList = new List<LedgerPosting>();
            var partyBalanceList = new List<NewPartyBalance>();
            foreach (var ledger in objAccountLedger)
            {
                var objSingePosting = objLedgerPosting.Where(x => x.LedgerId == ledger.Id).ToList();
                {
                    var balance = objSingePosting.Sum(x => x.Debit - x.Credit);
                    if (Math.Abs(balance) > 1)
                    {
                        var ledgerPosting = new LedgerPosting
                        {
                            DetailIds = "",
                            VoucherNumbering = 1,
                            TenantId = AbpSession.TenantId,
                            DateMiti = newFromMiti,
                            Date = newFromDate,
                            DetailId = Guid.Empty,
                            Debit = balance > 0 ? balance : 0,
                            Credit = balance < 0 ? Math.Abs(balance) : 0,
                            VoucherNo = ledger.Id.ToString(),
                            InvoiceNo = "",
                            LedgerId = ledger.Id,
                            FinancialYearId = newFinancialYearId,
                            VoucherTypeId = voucherTypeId,
                            PostingNumber = 0,
                            MasterId = ledger.Id,
                            VendorVoucherNo = ""
                        };
                        legerPostingList.Add(ledgerPosting);
                        // await ledgerPostingRepository.InsertAsync(ledgerPosting);
                        if (!ledger.IsBillByBill) continue;

                        var newPartyBalance = new NewPartyBalance
                        {
                            Date = financialYear.FromDate,
                            DueDate = financialYear.FromDate,
                            AgainstVoucherNo = "",
                            AgainstVoucherTypeId = Guid.Empty,
                            LedgerId = ledger.Id,
                            FinancialYearId = newFinancialYearId,
                            VoucherNo = "",
                            VoucherTypeId = voucherTypeId,
                            Debit = balance > 0 ? balance : 0,
                            Credit = balance < 0 ? Math.Abs(balance) : 0,
                            IsMain = true,
                            IsFullySettled = false,
                            IsPartiallySettled = false,
                            MasterId = ledger.Id,
                            DetailId = Guid.Empty,
                            MasterPartyBalanceId = null,
                            VoucherNumbering = 0,
                            AgainstVoucherNumbering = 0,
                            TenantId = AbpSession.TenantId
                        };
                        partyBalanceList.Add(newPartyBalance);
                        // await newPartyBalanceRepository.InsertAsync(newPartyBalance);
                    }
                }
            }

            var stockPostingList = new List<StockPosting>();
            var openingVoucherId = await VoucherTypeManager.GetVoucherTypeId("OpeningStock");
            foreach (var product in await GetProductStock(oldFinancialYearId))
            {
                stockPostingList.AddRange(from productUnit in product.ProductUnit
                                          where productUnit.Qty > 0
                                          select new StockPosting
                                          {
                                              IsDeleted = false,
                                              GrossAmount = productUnit.Qty * productUnit.Rate,
                                              DiscountAmount = 0,
                                              NetAmount = productUnit.Qty * productUnit.Rate,
                                              TaxAmount = 0,
                                              LedgerId = null,
                                              IsValueIncrease = true,
                                              VendorVoucherNo = "",
                                              MasterId = null,
                                              TenantId = AbpSession.TenantId,
                                              DateMiti = newFromMiti,
                                              Date = newFromDate,
                                              VoucherTypeId = openingVoucherId,
                                              VoucherNo = "",
                                              VoucherNumbering = 1,
                                              ProductId = product.ProductId,
                                              UnitId = productUnit.UnitId,
                                              Amount = productUnit.Qty * productUnit.Rate,
                                              AgainstVoucherTypeId = Guid.Empty,
                                              AgainstVoucherNo = "NA",
                                              InWardQty = productUnit.Qty,
                                              OutWardQty = 0,
                                              Rate = productUnit.Rate,
                                              FinancialYearId = newFinancialYearId
                                          });

                var qtyCount = product.ProductUnit.Where(x => x.Qty > 0).Sum(x => x.Qty);

            }




            var accountGroup =
                await accountGroupRepository.FirstOrDefaultAsync(x => x.Name.ToString() == "ReServers & Surplus".ToLower());
            if (accountGroup != null)
            {
                Guid masterId;
                var getProfitLossAc = await accountLedgerRepository.FirstOrDefaultAsync(x =>
                    x.Name.ToString() == "Profit & Loss A/c".ToLower()
                    && x.AccountGroupId == accountGroup.Id);
                if (getProfitLossAc == null)
                {
                    var accountLedger = new AccountLedger
                    {
                        Name = "Profit & Loss A/c",
                        OpeningBalance = 0,
                        IsDefault = true,
                        CrOrDr = DrOrCr.Dr,
                        Narration = "",
                        Address = "",
                        Phone = "",
                        Email = "",
                        OpeningDate = null,
                        CreditPeriod = 0,
                        CreditLimit = 0,
                        IsBillByBill = false,
                        Pan = "",
                        Status = true,
                        IsDelete = false,
                        IsCompany = false,
                        UserId = AbpSession.GetUserId(),
                        CreateUserId = AbpSession.GetUserId(),
                        UpdateUserId = null,
                        ParentId = null,
                        AccountGroupId = accountGroup.Id,
                        TenantId = AbpSession.GetTenantId()
                    };
                    masterId = await accountLedgerRepository.InsertAndGetIdAsync(accountLedger);
                }
                else
                {
                    masterId = getProfitLossAc.Id;
                }

                var profitLossAmount = await ProfitLossAmount(oldFinancialYearId);
                if (profitLossAmount != 0)
                {
                    var ledgerPosting = new LedgerPosting
                    {

                        VendorVoucherNo = "",
                        DetailIds = "",
                        VoucherNumbering = 0,
                        TenantId = AbpSession.TenantId,
                        DateMiti = newFromMiti,
                        Date = newFromDate,
                        DetailId = Guid.Empty,
                        Debit = profitLossAmount > 0 ? profitLossAmount : 0,
                        Credit = profitLossAmount < 0 ? Math.Abs(profitLossAmount) : 0,
                        VoucherNo = "",
                        InvoiceNo = "",
                        LedgerId = masterId,
                        FinancialYearId = newFinancialYearId,
                        VoucherTypeId = voucherTypeId,
                        PostingNumber = 0,
                        MasterId = masterId
                    };
                    legerPostingList.Add(ledgerPosting);
                    //await ledgerPostingRepository.InsertAsync(ledgerPosting);
                }
            }

            await ledgerPostingRepository.InsertRangeAsync(legerPostingList);
            await stockPostingRepository.InsertRangeAsync(stockPostingList);
            await newPartyBalanceRepository.InsertRangeAsync(partyBalanceList);

        }

        public async Task SyncData(Guid financialYearId)
        {
            var getFinancialYear = await financialYearRepository.FirstOrDefaultAsync(x => x.Id == financialYearId);

            // Add null checks
            if (getFinancialYear == null)
            {
                throw new InvalidOperationException($"Financial year with ID {financialYearId} not found");
            }

            if (getFinancialYear.OldFinancialYearId == null)
            {
                throw new InvalidOperationException("OldFinancialYearId cannot be null for data split operation");
            }


            var objAccountLedger = await accountLedgerRepository.GetAll()
                .Select(x => new { x.Id, x.IsBillByBill })
                .ToListAsync();

            var noTransferGroupId = new List<Guid>();
            var objGroups = await accountGroupRepository.GetAll().AsNoTracking()
                .Where(x => x.Name == "Direct Income" || x.Name == "Indirect Income" ||
                            x.Name == "Indirect Expenses" || x.Name == "Direct Expenses" ||
                            x.Name == "Sales Account" || x.Name == "Purchase Account")
                .Select(x => x.Id)
                .ToListAsync();

            foreach (var objGroup in objGroups)
            {
                noTransferGroupId.AddRange(await FuncRecursive(objGroup));
            }

            var voucherTypeId = await VoucherTypeManager.GetVoucherTypeId("OpeningBalance");
            var openingVoucherId = await VoucherTypeManager.GetVoucherTypeId("OpeningStock");

            // Delete existing entries
            await ledgerPostingRepository.DeleteAsync(x => x.FinancialYearId == getFinancialYear.Id && x.VoucherTypeId == voucherTypeId);
            await stockPostingRepository.DeleteAsync(x => x.FinancialYearId == getFinancialYear.Id && x.VoucherTypeId == openingVoucherId);

            // Store the old financial year ID in a local variable
            var oldFinancialYearId = getFinancialYear.OldFinancialYearId.Value;

            // Convert to HashSet for better performance
            var noTransferGroupIdSet = noTransferGroupId.ToHashSet();

            // Get ledger postings - simplified approach
            var allLedgerPostings = await ledgerPostingRepository.GetAll().AsNoTracking()
                .Include(x => x.AccountLedgerFk)
                .ThenInclude(x => x.AccountGroupFk)
                .Where(x => x.FinancialYearId == oldFinancialYearId)
                .ToListAsync();

            // Filter in memory to avoid LINQ translation issues
            var objLedgerPosting = allLedgerPostings
                .Where(x => x.AccountLedgerFk != null &&
                            !noTransferGroupIdSet.Contains(x.AccountLedgerFk.AccountGroupId))
                .ToList();

            var legerPostingList = new List<LedgerPosting>();
            var partyBalanceList = new List<NewPartyBalance>();

            foreach (var ledger in objAccountLedger)
            {
                var objSingePosting = objLedgerPosting.Where(x => x.LedgerId == ledger.Id).ToList();
                var balance = objSingePosting.Sum(x => x.Debit - x.Credit);

                if (Math.Abs(balance) > 1)
                {
                    var ledgerPosting = new LedgerPosting
                    {
                        DetailIds = "",
                        VoucherNumbering = 1,
                        TenantId = AbpSession.TenantId,
                        DateMiti = getFinancialYear.FromMiti,
                        Date = getFinancialYear.FromDate,
                        FinancialYearId = getFinancialYear.Id,
                        DetailId = Guid.Empty,
                        Debit = balance > 0 ? balance : 0,
                        Credit = balance < 0 ? Math.Abs(balance) : 0,
                        VoucherNo = ledger.Id.ToString(),
                        InvoiceNo = "",
                        LedgerId = ledger.Id,
                        VoucherTypeId = voucherTypeId,
                        PostingNumber = 0,
                        MasterId = ledger.Id,
                        VendorVoucherNo = ""
                    };
                    legerPostingList.Add(ledgerPosting);

                    if (ledger.IsBillByBill)
                    {
                        var newPartyBalance = new NewPartyBalance
                        {
                            Date = getFinancialYear.FromDate,
                            FinancialYearId = getFinancialYear.Id,
                            AgainstVoucherNo = "",
                            AgainstVoucherTypeId = Guid.Empty,
                            LedgerId = ledger.Id,
                            VoucherNo = "",
                            VoucherTypeId = voucherTypeId,
                            Debit = balance > 0 ? balance : 0,
                            Credit = balance < 0 ? Math.Abs(balance) : 0,
                            IsMain = true,
                            IsFullySettled = false,
                            IsPartiallySettled = false,
                            MasterId = ledger.Id,
                            DetailId = Guid.Empty,
                            MasterPartyBalanceId = null,
                            VoucherNumbering = 0,
                            AgainstVoucherNumbering = 0,
                            TenantId = AbpSession.TenantId
                        };
                        partyBalanceList.Add(newPartyBalance);
                    }
                }
            }

            var stockPostingList = new List<StockPosting>();


            var productStocks = await GetProductStock(oldFinancialYearId);

            foreach (var product in productStocks)
            {
                var validProductUnits = product.ProductUnit.Where(x => x.Qty > 0).ToList();

                foreach (var productUnit in validProductUnits)
                {
                    var stockPosting = new StockPosting
                    {
                        IsDeleted = false,
                        GrossAmount = productUnit.Qty * productUnit.Rate,
                        DiscountAmount = 0,
                        NetAmount = productUnit.Qty * productUnit.Rate,
                        TaxAmount = 0,
                        LedgerId = null,
                        IsValueIncrease = true,
                        VendorVoucherNo = "",
                        MasterId = null,
                        TenantId = AbpSession.TenantId,
                        DateMiti = getFinancialYear.FromMiti,
                        Date = getFinancialYear.FromDate,
                        FinancialYearId = getFinancialYear.Id,
                        VoucherTypeId = openingVoucherId,
                        VoucherNo = "",
                        VoucherNumbering = 1,
                        ProductId = product.ProductId,
                        UnitId = productUnit.UnitId,
                        Amount = productUnit.Qty * productUnit.Rate,
                        AgainstVoucherTypeId = Guid.Empty,
                        AgainstVoucherNo = "NA",
                        InWardQty = productUnit.Qty,
                        OutWardQty = 0,
                        Rate = productUnit.Rate,
                    };
                    stockPostingList.Add(stockPosting);
                }

                var qtyCount = validProductUnits.Sum(x => x.Qty);
            }

            // Handle Profit & Loss Account
            var accountGroup = await accountGroupRepository.FirstOrDefaultAsync(x =>
                x.Name.ToLower() == "ReServers & Surplus".ToLower());

            if (accountGroup != null)
            {
                Guid masterId;
                var getProfitLossAc = await accountLedgerRepository.FirstOrDefaultAsync(x =>
                    x.Name.ToLower() == "Profit & Loss A/c".ToLower() &&
                    x.AccountGroupId == accountGroup.Id);

                if (getProfitLossAc == null)
                {
                    var accountLedger = new AccountLedger
                    {
                        Name = "Profit & Loss A/c",
                        OpeningBalance = 0,
                        IsDefault = true,
                        CrOrDr = DrOrCr.Dr,
                        Narration = "",
                        Address = "",
                        Phone = "",
                        Email = "",
                        OpeningDate = null,
                        CreditPeriod = 0,
                        CreditLimit = 0,
                        IsBillByBill = false,
                        Pan = "",
                        Status = true,
                        IsDelete = false,
                        IsCompany = false,
                        UserId = AbpSession.GetUserId(),
                        CreateUserId = AbpSession.GetUserId(),
                        UpdateUserId = null,
                        ParentId = null,
                        AccountGroupId = accountGroup.Id,
                        TenantId = AbpSession.GetTenantId()
                    };
                    masterId = await accountLedgerRepository.InsertAndGetIdAsync(accountLedger);
                }
                else
                {
                    masterId = getProfitLossAc.Id;
                }

                var profitLossAmount = await ProfitLossAmount(oldFinancialYearId);
                if (profitLossAmount != 0)
                {
                    var ledgerPosting = new LedgerPosting
                    {
                        VendorVoucherNo = "",
                        DetailIds = "",
                        VoucherNumbering = 0,
                        TenantId = AbpSession.TenantId,
                        DateMiti = getFinancialYear.FromMiti,
                        Date = getFinancialYear.FromDate,
                        FinancialYearId = getFinancialYear.Id,
                        DetailId = Guid.Empty,
                        Debit = profitLossAmount > 0 ? profitLossAmount : 0,
                        Credit = profitLossAmount < 0 ? Math.Abs(profitLossAmount) : 0,
                        VoucherNo = "",
                        InvoiceNo = "",
                        LedgerId = masterId,
                        VoucherTypeId = voucherTypeId,
                        PostingNumber = 0,
                        MasterId = masterId
                    };
                    legerPostingList.Add(ledgerPosting);
                }
            }

            // Bulk insert all data
            if (legerPostingList.Any())
                await ledgerPostingRepository.InsertRangeAsync(legerPostingList);

            if (stockPostingList.Any())
                await stockPostingRepository.InsertRangeAsync(stockPostingList);

            if (partyBalanceList.Any())
                await newPartyBalanceRepository.InsertRangeAsync(partyBalanceList);
        }
        private async Task<decimal> ProfitLossAmount(Guid oldFinancialYearId)
        {
            var returnList = new List<FinancialStatementDto>();

            var stockCalculation = await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.StockCalculation, AbpSession.GetTenantId());

            PostingList = await ledgerPostingRepository.GetAll().AsNoTracking()
                .Where(x => x.FinancialYearId == oldFinancialYearId)
                .Include(x => x.AccountLedgerFk).Include(x => x.VoucherTypeFk)
                .AsSplitQuery()
                .Select(x => new LedgerPostingForReportDto
                {
                    Debit = x.Debit,
                    Credit = x.Credit,
                    LedgerId = x.LedgerId,
                    AccountGroupId = x.AccountLedgerFk.AccountGroupId,
                    VoucherName = x.VoucherTypeFk.Name
                }).OrderBy(x => x.AccountGroupId).ToListAsync();

            AccountLedgerList = await accountLedgerRepository.GetAll()
                .Where(e => e.TenantId == AbpSession.TenantId).AsNoTracking()
                .Select(x => new UniversalDropdownDto
                {
                    Id = x.Id,
                    DisplayName = x.Name
                })
                .ToListAsync();

            //start stock calculation
            var unitConversion = await unitConversionRepository.GetAll().AsNoTracking()
                .Include(x => x.UnitFk).Select(x => new
                {
                    x.ProductId,
                    x.UnitId,
                    x.PrimaryQty,
                    x.Qty,
                    UnitName = x.UnitFk.Name,
                    x.ConversionRate
                }).ToListAsync();
            var stockPostingList = await stockPostingRepository.GetAll()
                .AsNoTracking()
                .Where(x => x.FinancialYearId == oldFinancialYearId &&
                            x.TenantId == AbpSession.TenantId && !x.IsDeleted)
                .Include(x => x.VoucherTypeFk)
                .Include(x => x.ProductFk)
                .Where(e => e.VoucherTypeFk.Name != "StockReceipt" || e.VoucherTypeFk.Name != "StockTransfer")
                .AsSplitQuery()
                .Select(x => new
                {
                    x.ProductId,
                    VoucherType = x.VoucherTypeFk.Name,
                    x.Date,
                    ProductName = x.ProductFk.Name,
                    x.Amount,
                    x.Rate,
                    x.UnitId,
                    x.InWardQty,
                    x.OutWardQty
                }).ToListAsync();

            var posting = new List<NewProductReport>();
            foreach (var x in stockPostingList)
            {
                var product = new NewProductReport();
                var minUnit = unitConversion.Where(a => a.ProductId == x.ProductId).MinBy(a => a.ConversionRate);
                var unitConversionDetail =
                    unitConversion.FirstOrDefault(a => a.ProductId == x.ProductId && a.UnitId == x.UnitId);
                if (minUnit == null)
                {
                    var units = new UnitConversion
                    {
                        Id = Guid.Empty,
                        IsDeleted = false,
                        ConversionRate = 1,
                        Qty = 1,
                        PrimaryQty = 1,
                        ProductId = x.ProductId,
                        UnitId = x.UnitId,
                        TenantId = AbpSession.TenantId
                    };
                    await unitConversionRepository.InsertAsync(units);
                }
                else
                {
                    product.ProductId = x.ProductId;
                    product.ProductName = x.ProductName;
                    product.UnitId = minUnit.UnitId;
                    product.VoucherType = x.VoucherType;
                    product.Date = x.Date;
                    if (unitConversionDetail != null)
                    {
                        product.Rate = x.Rate * minUnit.PrimaryQty * unitConversionDetail.Qty /
                                       (minUnit.Qty * unitConversionDetail.PrimaryQty);
                        product.InWardQty = x.InWardQty * unitConversionDetail.PrimaryQty * minUnit.Qty /
                                            (unitConversionDetail.Qty * minUnit.PrimaryQty);
                        product.OutWardQty = x.OutWardQty * unitConversionDetail.PrimaryQty * minUnit.Qty /
                                             (unitConversionDetail.Qty * minUnit.PrimaryQty);
                        product.InWardValue = x.InWardQty * x.Rate;
                        product.OutWardValue = x.OutWardQty * x.Rate;
                    }

                    product.CurrentStock = product.InWardQty - product.OutWardQty;
                    posting.Add(product);
                }
            }

            var openingStockList = stockPostingList.Where(x => x.InWardQty > 0 && x.VoucherType == "OpeningStock")
                .Select(x => new FinancialStatementDto
                {
                    Id = x.ProductId,
                    Data = new FinancialStatementDetail
                    {
                        Id = x.ProductId,
                        Name = x.ProductName,
                        Debit = x.InWardQty * x.Rate,
                        Credit = 0,
                        GroupType = TrailBalanceGroupEnum.Product
                    },
                    Children = null
                }).ToList();

            foreach (var product in stockPostingList.DistinctBy(a => a.ProductId).ToList())
            {
                var productUniqueStock = posting.Where(p => p.ProductId == product.ProductId).ToList();
                if (!productUniqueStock.Any()) continue;
                decimal stockValue = 0;
                decimal stockQty = 0;
                if (stockCalculation.ToLower() == "fifo")
                {
                    var datedProductUniqueStock = productUniqueStock.OrderBy(x => x.Date.Date).ToList();
                    var soldQty = datedProductUniqueStock.Where(x => x.InWardQty == 0).Sum(x => x.OutWardQty);
                    foreach (var datedProduct in datedProductUniqueStock)
                    {
                        if (soldQty > datedProduct.InWardQty)
                        {
                            soldQty -= datedProduct.InWardQty;
                            continue;
                        }

                        if (soldQty <= datedProduct.InWardQty)
                        {
                            var qty = datedProduct.InWardQty - soldQty;
                            if (qty > 0)
                            {
                                stockValue += qty * datedProduct.Rate;
                                soldQty = 0;
                                continue;
                            }
                        }

                        if (soldQty == 0) stockValue += datedProduct.InWardValue;
                    }
                }

                if (stockCalculation.ToLower() == "lifo")
                {
                    var datedProductUniqueStock = productUniqueStock.OrderBy(x => x.Date.Date).ToList();
                    var newList = new List<NewProductReport>();
                    foreach (var pro in datedProductUniqueStock)
                    {
                        if (pro.InWardQty == 0)
                        {
                            var tempQty = pro.OutWardQty;
                            while (tempQty > 0)
                            {
                                var last = newList.LastOrDefault();
                                if (last != null && last.InWardQty < tempQty)
                                {
                                    newList.Remove(last);
                                    tempQty -= last.InWardQty;
                                    continue;
                                }

                                if (last == null || last.InWardQty < tempQty) continue;
                                newList.Remove(last);
                                last.InWardQty -= tempQty;
                                newList.Add(last);
                                tempQty = 0;
                            }
                        }

                        if (pro.OutWardQty == 0) newList.Add(pro);
                    }

                    stockValue = newList.Sum(x => x.InWardQty * x.Rate);
                }
                else
                {
                    decimal productRate = 0;
                    var inward = productUniqueStock
                        .Where(x => x.VoucherType is "OpeningStock" or "StockJournal" or "StockReceipt" or "PurchaseInvoice"
                            or "PurchaseReturn")
                        .Where(x => x.InWardQty > 0).ToList();
                    if (inward.Count > 0)
                    {
                        var inWardAmt = inward.Sum(x => x.InWardValue);
                        var inWardQty = inward.Sum(x => x.InWardQty);
                        productRate = inWardAmt / inWardQty;
                    }

                    stockQty = productUniqueStock.Sum(b => b.InWardQty) -
                               productUniqueStock.Sum(b => b.OutWardQty);

                    stockValue = productRate * stockQty;
                }

                if (stockValue == 0) continue;
                var productData = new FinancialStatementDto
                {
                    Id = product.ProductId,
                    Data = new FinancialStatementDetail
                    {
                        Id = product.ProductId,
                        Name = product.ProductName + "  Qty-" + Math.Round(stockQty, 2),
                        Debit = 0,
                        Credit = Math.Round(stockValue, 2),
                        GroupType = TrailBalanceGroupEnum.Product
                    }
                };
                ClStockReports.Add(productData);
            }

            var opStock = new FinancialStatementDto
            {
                Id = Guid.Empty,
                Data = new FinancialStatementDetail
                {
                    Id = Guid.Empty,
                    Name = "Opening Stock",
                    Debit = openingStockList.Sum(x => x.Data.Debit),
                    Credit = 0,
                    GroupType = TrailBalanceGroupEnum.OpeningStock
                },
                Children = openingStockList.Where(x => x.Data.Debit > 0).ToList()
            };
            returnList.Add(opStock);
            var primaryAccountGroup = await accountGroupRepository.FirstOrDefaultAsync(x => x.Name == "Primary");

            var result = await FuncRecursiveGroupId(primaryAccountGroup.Id);
            returnList.AddRange(result);
            returnList.Add(new FinancialStatementDto
            {
                Id = Guid.Empty,
                Children = ClStockReports,
                Data = new FinancialStatementDetail
                {
                    Id = Guid.Empty,
                    Name = "" +
                           "Closing" +
                           "" +
                           "" +
                           " Stock",
                    Credit = ClStockReports.Sum(x => x.Data.Credit),
                    Debit = ClStockReports.Sum(x => x.Data.Debit),
                    GroupType = TrailBalanceGroupEnum.ClosingStock
                }
            });
            var profitLossAmount = returnList.Sum(x => x.Data.Debit - x.Data.Credit);
            return profitLossAmount;
        }


        private async Task<List<FinancialStatementDto>> FuncRecursiveGroupId(Guid accountGroupId)
        {
            var result = new List<FinancialStatementDto>();
            var accountGroups = accountGroupRepository.GetAll().AsNoTracking()
                .Where(x => x.GroupUnder == accountGroupId && x.Name != "Primary" && x.AffectGrossProfit);

            foreach (var accountGroup in await accountGroups.ToListAsync())
            {
                var ledgerPostings =
                    PostingList.Where(x => x.AccountGroupId == accountGroup.Id).ToList();
                var calcAmount = ledgerPostings.Sum(x => x.Debit) - ledgerPostings.Sum(x => x.Credit);
                var data = new FinancialStatementDetail
                {
                    Id = accountGroup.Id,
                    Name = accountGroup.Name,
                    Debit = calcAmount > 0 ? calcAmount : 0,
                    Credit = calcAmount < 0 ? Math.Abs(calcAmount) : 0,
                    GroupType = TrailBalanceGroupEnum.AccountGroup
                };
                var a = new FinancialStatementDto
                {
                    Data = data,
                    Id = accountGroup.Id
                };
                a.Children = a.Id != Guid.Empty ? await FuncRecursiveGroupId(accountGroup.Id) : null;

                var distinctLedgerIds = ledgerPostings.Select(x => x.LedgerId).Distinct().ToList();
                var ledgersList = new List<FinancialStatementDto>();
                foreach (var distinctLedgerId in distinctLedgerIds)
                {
                    var m = new FinancialStatementDto();
                    var calcLedgerAmount = ledgerPostings.Where(x => x.LedgerId == distinctLedgerId).Sum(x => x.Debit)
                                           - ledgerPostings.Where(x => x.LedgerId == distinctLedgerId).Sum(x => x.Credit);
                    var ledger = new FinancialStatementDetail
                    {
                        Id = distinctLedgerId,
                        Name = AccountLedgerList.FirstOrDefault(x => x.Id == distinctLedgerId)?.DisplayName,
                        Debit = calcLedgerAmount > 0 ? calcLedgerAmount : 0,
                        Credit = calcLedgerAmount < 0 ? Math.Abs(calcLedgerAmount) : 0,
                        GroupType = TrailBalanceGroupEnum.AccountLedger
                    };
                    m.Id = Guid.Empty;
                    m.Data = ledger;
                    ledgersList.Add(m);
                }

                if (a.Children != null)
                {
                    if (a.Children.Count > 0)
                    {
                        data.Debit += a.Data.Debit = a.Children.Sum(x => x.Data.Debit); /*+ a.DebitAmount*/
                        data.Credit += a.Data.Credit = a.Children.Sum(x => x.Data.Credit); /*+ a.CreditAmount*/
                    }

                    var fAmt = data.Debit - data.Credit;
                    data.Debit = fAmt > 0 ? fAmt : 0;
                    data.Credit = fAmt < 0 ? Math.Abs(fAmt) : 0;
                    a.Children.AddRange(ledgersList);
                }

                result.Add(a);
            }

            return result;
        }


        private async Task<List<StockForFinancialYearDto>> GetProductStock(Guid financialYearId)
        {
            var unitConversion = await unitConversionRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId).AsNoTracking()
                .Include(x => x.UnitFk).Select(x => new
                {
                    x.ProductId,
                    x.UnitId,
                    x.PrimaryQty,
                    x.Qty,
                    x.ConversionRate
                }).ToListAsync();

            var stockQuery = stockPostingRepository.GetAll().AsNoTracking()
                .Where(x => x.TenantId == AbpSession.TenantId && x.FinancialYearId == financialYearId && !x.IsDeleted)
                .Include(x => x.VoucherTypeFk)
                .Include(x => x.ProductFk)
                .ThenInclude(x => x.UnitFk)
                .AsSplitQuery()
                .Select(x => new
                {
                    x.ProductId,
                    x.Date,
                    UnitName = x.UnitFk.Name,
                    VoucherType = x.VoucherTypeFk.Name,
                    x.ProductFk.ProductGroupId,
                    ProductName = x.ProductFk.Name,
                    x.Rate,
                    x.ProductFk.PurchaseRate,
                    x.UnitId,
                    x.InWardQty,
                    x.OutWardQty
                });


            var stockpostinglist = await stockQuery
                .AsSplitQuery()
                .Select(x => new
                {
                    x.ProductId,
                    x.Date,
                    x.VoucherType,
                    x.ProductGroupId,
                    x.ProductName,
                    x.UnitName,
                    x.Rate,
                    x.PurchaseRate,
                    x.UnitId,
                    x.InWardQty,
                    x.OutWardQty
                }).ToListAsync();

            var posting = new List<NewProductReport>();
            foreach (var x in stockpostinglist)
            {
                var product = new NewProductReport();


                product.ProductId = x.ProductId;
                product.ProductName = x.ProductName;
                product.UnitId = x.UnitId;
                product.VoucherType = x.VoucherType;
                product.Date = x.Date;
                product.Rate = x.Rate;

                product.InWardQty = x.InWardQty;
                product.OutWardQty = x.OutWardQty;
                product.InWardValue = x.InWardQty * x.Rate;
                product.OutWardValue = x.OutWardQty * x.Rate;
                product.CurrentStock = product.InWardQty - product.OutWardQty;
                posting.Add(product);

            }

            var list = new List<StockForFinancialYearDto>();

            foreach (var product in stockpostinglist.Select(a =>
                             new { a.ProductId, a.UnitId })
                         .DistinctBy(b => b.ProductId))
            {
                var singleProduct = unitConversion.Where(a => a.ProductId == product.ProductId).ToList();
                var minUnit = singleProduct.MinBy(a => a.ConversionRate);
                var orderedUnits = singleProduct.OrderByDescending(x => x.ConversionRate).ToList();
                var stockPosting = posting.Where(x => x.ProductId == product.ProductId).ToList();
                if (stockPosting.Count <= 0) continue;
                {
                    var stockCalculation = new StockForFinancialYearDto();

                    var stockRate = stockPosting.Where(x =>
                            x.VoucherType is "OpeningStock" or "StockJournal" or "StockReceipt" or "PurchaseInvoice"
                                or "PurchaseReturn")
                        .Where(x => x.InWardQty > 0);

                    decimal purchaseRate = 0;
                    var inward = stockRate.Where(x => x.InWardQty > 0).ToList();
                    if (inward.Count > 0) purchaseRate = inward.Sum(x => x.InWardValue) / inward.Sum(x => x.InWardQty);
                    stockCalculation.ProductId = product.ProductId;
                    var closingQty = stockPosting.Sum(x => x.InWardQty - x.OutWardQty);


                    var closingTemporaryUnits = new List<TemporaryUnitsForFinanceDto>();

                    closingTemporaryUnits.Add(new TemporaryUnitsForFinanceDto
                    {
                        Qty = closingQty,
                        UnitId = product.UnitId,
                        Rate = purchaseRate
                    });


                    stockCalculation.ProductUnit = closingTemporaryUnits;
                    list.Add(stockCalculation);
                }
            }

            return list;
        }

        private async Task<List<Guid>> FuncRecursive(Guid id)
        {
            var result = await accountGroupRepository.GetAll()
                .Where(x => x.TenantId == AbpSession.TenantId && x.GroupUnder == id)
                .Select(x => x.Id).ToListAsync();
            var groupList = new List<Guid>();
            foreach (var i in result)
            {
                var groupListnew = await FuncRecursive(i);
                groupList.AddRange(groupListnew);
            }

            groupList.Add(id);
            groupList.Sort();
            return groupList;
        }

        [AbpAuthorize(AppPermissions.PagesFinancialYearsCreate)]
        protected virtual async Task Create(CreateOrEditFinancialYearDto input)
        {
            var financialYear = new FinancialYear
            {
                Id = Guid.Empty,
                TenantId = null,
                Name = input.FromMiti.Split('/')[0] + " ➔ " + input.ToMiti.Split('/')[0].Substring(2, 2),
                FromDate = DateConverter.ConvertToEnglish(input.FromMiti),
                ToDate = new NepaliDate(input.ToMiti).EnglishDate,
                FromMiti = input.FromMiti,
                ToMiti = input.ToMiti,
                Status = false,
                IsOldYear = false
            };

            if (AbpSession.TenantId != null) financialYear.TenantId = AbpSession.TenantId;

            await financialYearRepository.InsertAsync(financialYear);
        }

        [AbpAuthorize(AppPermissions.PagesFinancialYearsEdit)]
        protected virtual async Task Update(CreateOrEditFinancialYearDto input)
        {
            var financialYear = await financialYearRepository.FirstOrDefaultAsync(x => x.Id == input.Id);
            if (financialYear != null)
            {
                financialYear.Name = input.FromMiti.Split('/')[0] + " ➔ " + input.ToMiti.Split('/')[0].Substring(2, 2);
                financialYear.FromDate = DateConverter.ConvertToEnglish(input.FromMiti);
                financialYear.ToDate = DateConverter.ConvertToEnglish(input.ToMiti);
                financialYear.FromMiti = input.FromMiti;
                financialYear.ToMiti = input.ToMiti;
                await financialYearRepository.UpdateAsync(financialYear);
            }
        }
    }
}
