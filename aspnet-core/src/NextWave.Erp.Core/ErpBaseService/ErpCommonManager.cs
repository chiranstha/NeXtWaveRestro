using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.GeneralSetting;
using NextWave.Erp.Inventory;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.SharedDtos;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.ErpBaseService
{
    public class ErpCommonManager(
        IRepository<FinancialYearSelect, Guid> dateSelectRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<UnitConversion, Guid> unitConversionRepository,
        IRepository<Branch, Guid> branchRepository,
        IRepository<Tax, Guid> taxRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<Posting, Guid> postingRepository,
        IRepository<FinancialYear, Guid> financialYearRepository
        ) : ErpDomainServiceBase
    {
        [UnitOfWork]
        public Guid GetCurrentFinancialYearId(long userId)
        {
            using var unitOfWork = unitOfWorkManager.Begin(new UnitOfWorkOptions
            {
                IsTransactional = true,
                IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted
            });
            try
            {
                var tenantId = CurrentUnitOfWork.GetTenantId();
                if (!tenantId.HasValue) return Guid.Empty;

                var financialYear = financialYearRepository.FirstOrDefault(x => x.Status && x.TenantId == tenantId);
                var dateSelect = dateSelectRepository.FirstOrDefault(x =>
                    x.Date == DateTime.Today && x.UserId == userId && x.TenantId == tenantId);

                unitOfWork.Complete();

      //      Fix: Add null check for financialYear
                if (dateSelect != null)
                        return dateSelect.FinancialYearId;
                if (financialYear != null)
                {
                    return financialYear.Id;
                }

                var getAllFinancialYear = financialYearRepository.GetAll().Where(x => x.TenantId == tenantId)
                    .OrderByDescending(e => e.FromDate).FirstOrDefault();
                return getAllFinancialYear?.Id ?? Guid.Empty;
            }
            catch (Exception ex)
            {
          //      Ensure the unit of work is disposed in case of exceptions
                unitOfWork.Dispose();
                    throw new UserFriendlyException(L("Error"), ex.Message);
                }
            }
        public FinancialYear GetFinancialYear(Guid id)
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();
            return financialYearRepository.FirstOrDefault(x => x.TenantId == tenantId && x.Id == id);
        }

        public decimal GetPostingNumbering()
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();
            var posting = postingRepository.FirstOrDefault(x => x.TenantId == tenantId);
            if (posting == null)
            {
                var model = new Posting
                {
                    Numbering = 1,
                    TenantId = tenantId,
                };
                postingRepository.Insert(model);
                return model.Numbering;
            }
            posting.Numbering += 1;
            postingRepository.Update(posting);
            return posting.Numbering;
        }
        public Guid GetTaxDefault()
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();
            return taxRepository.FirstOrDefault(x => x.TenantId == tenantId && x.Rate == 0).Id;
        }

        public async Task<UnitConversionResultDto> UnitConversionMinus(UnitConversionParamDto input)
        {
            var result = new UnitConversionResultDto();
            var unitConversions = await unitConversionRepository.GetAll().Where(x => x.ProductId == input.ProductId)
                .Include(x => x.UnitFk).AsNoTracking().ToListAsync();
            var minUnit = unitConversions.MinBy(x => x.ConversionRate);
            decimal qty = 0;
            decimal minusQty = 0;
            decimal rate = 0;
            if (input.Details.Count == 0)
            {
                result.ProductId = input.ProductId;
                result.Qty = input.Qty;
                result.Rate = input.Rate;
                result.UnitName = (await unitRepository.FirstOrDefaultAsync(x => x.Id == input.UnitId)).Name;
                result.UnitId = input.UnitId;
                return result;
            }

            foreach (var detail in input.Details)
            {
                var thisUnitConversion = unitConversions.FirstOrDefault(x => x.UnitId == detail.UnitId);
                if (thisUnitConversion != null && minUnit != null && minUnit.Id != thisUnitConversion.Id)
                {
                    minusQty += detail.Qty * minUnit.Qty * thisUnitConversion.PrimaryQty / minUnit.PrimaryQty /
                                thisUnitConversion.Qty;
                }
                else
                {
                    minusQty += detail.Qty;
                }
            }

            var thisConversion = unitConversions.FirstOrDefault(x => x.UnitId == input.UnitId);
            if (minUnit != null)
                if (thisConversion != null)
                {
                    qty = (input.Qty * minUnit.Qty * thisConversion.PrimaryQty / minUnit.PrimaryQty /
                           thisConversion.Qty) - minusQty;
                    rate = input.Rate / thisConversion.PrimaryQty * thisConversion.Qty;
                }

            var dscUnitList = unitConversions.OrderByDescending(x => x.ConversionRate).ToList();
            foreach (var dscUnit in dscUnitList)
            {
                if (minUnit != null && qty % (minUnit.Qty * dscUnit.PrimaryQty / minUnit.PrimaryQty / dscUnit.Qty) == 0)
                {
                    result.Qty = qty / (minUnit.Qty * dscUnit.PrimaryQty / minUnit.PrimaryQty / dscUnit.Qty);
                    result.UnitId = dscUnit.UnitId;
                    result.Rate = rate * dscUnit.PrimaryQty / dscUnit.Qty;
                    result.UnitName = dscUnit.UnitFk.Name;
                    result.ProductId = input.ProductId;
                    return result;
                }
            }

            // added start
            var ascUnit = unitConversions.MinBy(x => x.ConversionRate);
            if (minUnit != null)
                result.Qty = qty / (minUnit.Qty * ascUnit.PrimaryQty / minUnit.PrimaryQty / ascUnit.Qty);
            result.UnitId = ascUnit.UnitId;
            result.Rate = rate * ascUnit.PrimaryQty / ascUnit.Qty;
            result.UnitName = ascUnit.UnitFk.Name;
            result.ProductId = input.ProductId;
            return result;
            // added end.
        }

        public async Task<UnitConversionAddParamDetails> UnitConversionAdd(UnitConversionAddParamsDto input)
        {
            var unitConversions = await unitConversionRepository.GetAll().Where(x => x.ProductId == input.ProductId)
                .Include(x => x.UnitFk).AsNoTracking().ToListAsync();
            var minUnit = unitConversions.MinBy(x => x.ConversionRate);
            decimal sumQty = 0;
            foreach (var detail in input.Details)
            {
                var thisUnitConversion = unitConversions.FirstOrDefault(x => x.UnitId == detail.UnitId);
                if (thisUnitConversion != null && minUnit.Id != thisUnitConversion.Id)
                {
                    sumQty += detail.Qty * minUnit.Qty * thisUnitConversion.PrimaryQty / minUnit.PrimaryQty /
                              thisUnitConversion.Qty;
                }
                else
                {
                    sumQty += detail.Qty;
                }
            }

            var result = new UnitConversionAddParamDetails()
            {
                UnitId = minUnit.UnitId,
                Qty = sumQty,
                Rate = sumQty == 0 ? 0 : input.Details.Sum(x => x.NetAmount) / sumQty,
                GrossAmount = input.Details.Sum(x => x.GrossAmount),
                Discount = input.Details.Sum(x => x.Discount),
                NetAmount = input.Details.Sum(x => x.NetAmount),
                TaxAmount = input.Details.Sum(x => x.TaxAmount),
                TotalAmount = input.Details.Sum(x => x.TotalAmount),
            };
            var dscUnitList = unitConversions.OrderByDescending(x => x.ConversionRate).ToList();
            var rate = sumQty == 0 ? 0 : input.Details.Sum(x => x.NetAmount) / sumQty;
            foreach (var dscUnit in dscUnitList)
            {
                if (minUnit.UnitId != dscUnit.UnitId)
                {
                    if (sumQty % (minUnit.Qty * dscUnit.PrimaryQty / minUnit.PrimaryQty / dscUnit.Qty) == 0)
                    {
                        result.Qty = (sumQty / (minUnit.Qty * dscUnit.PrimaryQty / minUnit.PrimaryQty / dscUnit.Qty));
                        result.UnitId = dscUnit.UnitId;
                        result.Rate = (rate / (minUnit.Qty * dscUnit.PrimaryQty / minUnit.PrimaryQty / dscUnit.Qty));
                        break;
                    }
                }
                else
                {
                    result.Qty = (sumQty / (minUnit.Qty * dscUnit.PrimaryQty / minUnit.PrimaryQty / dscUnit.Qty));
                    result.UnitId = dscUnit.UnitId;
                    result.Rate = (rate / (minUnit.Qty * dscUnit.PrimaryQty / minUnit.PrimaryQty / dscUnit.Qty));
                }
            }

            return result;
        }

        public async Task<UnitConversionResultDoubleDto> UnitConversionMinusDouble(UnitConversionParamDto input)
        {
            var result = new UnitConversionResultDoubleDto();
            var unitConversions = await unitConversionRepository.GetAll().Where(x => x.ProductId == input.ProductId)
                .Include(x => x.UnitFk).AsNoTracking().ToListAsync();
            var minUnit = unitConversions.MinBy(x => x.ConversionRate);
            double qty = 0;
            double minusQty = 0;
            decimal rate = 0;
            if (input.Details.Count == 0)
            {
                result.ProductId = input.ProductId;
                result.Qty = (double)input.Qty;
                result.Rate = (double)input.Rate;
                result.UnitName = (await unitRepository.FirstOrDefaultAsync(x => x.Id == input.UnitId)).Name;
                result.UnitId = input.UnitId;
                return result;
            }

            foreach (var detail in input.Details)
            {
                var thisUnitConversion = unitConversions.FirstOrDefault(x => x.UnitId == detail.UnitId);
                if (thisUnitConversion != null && minUnit.Id != thisUnitConversion.Id)
                {
                    minusQty += (double)detail.Qty * (double)minUnit.Qty * (double)thisUnitConversion.PrimaryQty /
                                (double)minUnit.PrimaryQty /
                                (double)thisUnitConversion.Qty;
                }
                else
                {
                    minusQty += (double)detail.Qty;
                }
            }

            var thisConversion = unitConversions.FirstOrDefault(x => x.UnitId == input.UnitId);
            if (minUnit != null)
                if (thisConversion != null)
                {
                    qty = ((double)input.Qty * (double)minUnit.Qty * (double)thisConversion.PrimaryQty /
                           (double)minUnit.PrimaryQty /
                           (double)thisConversion.Qty) - minusQty;
                    rate = input.Rate / thisConversion.PrimaryQty * thisConversion.Qty;
                }

            var dscUnitList = unitConversions.OrderByDescending(x => x.ConversionRate).ToList();
            foreach (var dscUnit in dscUnitList)
            {
                if (minUnit != null && qty % ((double)minUnit.Qty * (double)dscUnit.PrimaryQty /
                                              (double)minUnit.PrimaryQty / (double)dscUnit.Qty) == 0)
                {
                    result.Qty = (qty / ((double)minUnit.Qty * (double)dscUnit.PrimaryQty / (double)minUnit.PrimaryQty /
                                         (double)dscUnit.Qty));
                    result.UnitId = dscUnit.UnitId;
                    result.Rate = (double)(rate * dscUnit.PrimaryQty / dscUnit.Qty);
                    result.UnitName = dscUnit.UnitFk.Name;
                    result.ProductId = input.ProductId;
                    return result;
                }
            }

            // added start
            var ascUnit = unitConversions.MinBy(x => x.ConversionRate);
            if (minUnit != null)
                result.Qty = qty / ((double)minUnit.Qty * (double)ascUnit.PrimaryQty / (double)minUnit.PrimaryQty /
                                    (double)ascUnit.Qty);
            result.UnitId = ascUnit.UnitId;
            result.Rate = (double)rate * (double)ascUnit.PrimaryQty / (double)ascUnit.Qty;
            result.UnitName = ascUnit.UnitFk.Name;
            result.ProductId = input.ProductId;
            return result;
            // added end.
        }

        public async Task<UnitConversionResultDto> UnitConversionPlus(UnitConversionDoubleDto input)
        {
            var result = new UnitConversionResultDto();
            var unitConversions = await unitConversionRepository.GetAll().Where(x => x.ProductId == input.ProductId)
                .Include(x => x.UnitFk).AsNoTracking().ToListAsync();
            var minUnit = unitConversions.MinBy(x => x.ConversionRate);
            double qty = 0;
            double sumQty = 0;
            double rate = 0;
            foreach (var detail in input.Details)
            {
                var thisUnitConversion = unitConversions.FirstOrDefault(x => x.UnitId == detail.UnitId);
                if (minUnit != null)
                    if (thisUnitConversion != null)
                        sumQty += detail.Qty * (double)minUnit.Qty * (double)thisUnitConversion.PrimaryQty /
                                  (double)minUnit.PrimaryQty /
                                  (double)thisUnitConversion.Qty;
            }

            var thisConversion = unitConversions.FirstOrDefault(x => x.UnitId == input.UnitId);
            if (minUnit != null)
                if (thisConversion != null)
                {
                    qty = (input.Qty * (double)minUnit.Qty * (double)thisConversion.PrimaryQty /
                           (double)minUnit.PrimaryQty /
                           (double)thisConversion.Qty) + sumQty;
                    rate = (input.Rate * (double)minUnit.Qty * (double)thisConversion.PrimaryQty /
                            (double)minUnit.PrimaryQty /
                            (double)thisConversion.Qty);
                }

            var dscUnitList = unitConversions.OrderByDescending(x => x.ConversionRate).ToList();
            foreach (var dscUnit in dscUnitList)
            {
                if (minUnit != null && minUnit.UnitId != dscUnit.UnitId)
                {
                    if (qty % ((double)minUnit.Qty * (double)dscUnit.PrimaryQty / (double)minUnit.PrimaryQty /
                               (double)dscUnit.Qty) == 0)
                    {
                        result.Qty = (decimal)(qty / ((double)minUnit.Qty * (double)dscUnit.PrimaryQty /
                                                      (double)minUnit.PrimaryQty / (double)dscUnit.Qty));
                        result.UnitId = dscUnit.UnitId;
                        result.Rate = (decimal)(rate / ((double)minUnit.Qty * (double)dscUnit.PrimaryQty /
                                                        (double)minUnit.PrimaryQty / (double)dscUnit.Qty));
                        result.UnitName = dscUnit.UnitFk.Name;
                        break;
                    }
                }
                else
                {
                    if (minUnit != null)
                    {
                        result.Qty = (decimal)(qty / ((double)minUnit.Qty * (double)dscUnit.PrimaryQty /
                                                      (double)minUnit.PrimaryQty / (double)dscUnit.Qty));
                        result.UnitId = dscUnit.UnitId;
                        result.Rate = (decimal)(rate / ((double)minUnit.Qty * (double)dscUnit.PrimaryQty /
                                                        (double)minUnit.PrimaryQty / (double)dscUnit.Qty));
                    }

                    result.UnitName = dscUnit.UnitFk.Name;
                }
            }

            result.ProductId = input.ProductId;
            return result;
        }
        public Guid GetMainBranchId()
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();
            return branchRepository.FirstOrDefault(x => x.TenantId == tenantId && x.IsMain).Id;
        }
        public decimal GetTaxRate(Guid taxId)
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();
            if (taxRepository.Count(x => x.Id == taxId && x.TenantId == tenantId) <= 0) return 0;
            {
                var rate = taxRepository.FirstOrDefault(x => x.Id == taxId && x.TenantId == tenantId).Rate;
                if (rate > 0) return rate / 100;
            }
            return 0;
        }
    }
}
