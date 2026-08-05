using Abp.Authorization;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Enums;
using NextWave.Erp.Inventory.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory
{
    [AbpAuthorize]
    public class StockManagementAppService(
      IRepository<StockFifoTable, Guid> stockFifoTableRepository,
      IRepository<UnitConversion, Guid> unitConversionRepository,
      IRepository<ProductGroup, Guid> productGroupRepository,
      IRepository<Product, Guid> productRepository,
      IRepository<StockMaintain, Guid> stockMaintainRepository) : ErpAppServiceBase
    {
        public async Task MaintainStock(StockMaintainDto data)
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();
            var unitConversion =
                await unitConversionRepository.GetAll().Where(x => x.ProductId == data.ProductId).ToListAsync();
            var unit = unitConversion.FirstOrDefault(x => x.UnitId == data.UnitId);
            var minUnit = unitConversion.MinBy(x => x.ConversionRate);
            decimal qty = 0;
            decimal rate = 0;
            if (data.Type == StockMaintainTypeEnum.Opening)
            {
                qty = data.Qty;
                rate = data.Rate;
            }
            else
            {
                if (unit != null && minUnit != null)
                {
                    qty = data.Qty * unit.PrimaryQty * minUnit.Qty / (unit.Qty * minUnit.PrimaryQty);
                    rate = data.Rate * unit.Qty * minUnit.PrimaryQty / (unit.PrimaryQty * minUnit.Qty);
                }
            }

            var databaseStock = await stockMaintainRepository.FirstOrDefaultAsync(x =>
                x.ProductId == data.ProductId );
            var fifo = new MaintainFifoDto
            {
                Date = DateConverter.ConvertToEnglish(data.DateMiti),
                DateMiti = data.DateMiti,
                ProductId = data.ProductId,
                IsIn = data.Type is StockMaintainTypeEnum.Inward or StockMaintainTypeEnum.Opening,
                Qty = data.Qty,
                Rate = data.Rate,
                UnitId = data.UnitId
            };
            await MaintainFifo(fifo);

            if (databaseStock == null)
            {
                var stock = new StockMaintain
                {
                    ProductId = data.ProductId,
                    LastModifiedDate = DateTime.Now,
                    OpeningQty = data.Type == StockMaintainTypeEnum.Opening ? qty : 0,
                    InwardQty = data.Type == StockMaintainTypeEnum.Inward ? qty : 0,
                    OutwardQty = data.Type == StockMaintainTypeEnum.Outward ? qty : 0,
                    OpeningRate = data.Type == StockMaintainTypeEnum.Opening ? rate : 0,
                    InwardRate = data.Type == StockMaintainTypeEnum.Inward ? rate : 0,
                    OutwardRate = data.Type == StockMaintainTypeEnum.Outward ? rate : 0,
                    OpeningAmt = data.Type == StockMaintainTypeEnum.Opening ? rate * qty : 0,
                    InwardAmt = data.Type == StockMaintainTypeEnum.Inward ? rate * qty : 0,
                    OutwardAmt = data.Type == StockMaintainTypeEnum.Outward ? rate * qty : 0,
                    TenantId = tenantId,
                    UnitId = minUnit?.UnitId ?? data.UnitId,
                    FinancialYearId = data.FinancialYearId
                };
                await stockMaintainRepository.InsertAsync(stock);
            }
            else
            {
                databaseStock.LastModifiedDate = DateTime.Now;
                switch (data.Type)
                {
                    case StockMaintainTypeEnum.Opening:
                        databaseStock.OpeningRate = rate;
                        databaseStock.OpeningQty = qty;
                        databaseStock.OpeningAmt = rate * qty;
                        break;
                    case StockMaintainTypeEnum.Inward:
                        {
                            var q = qty + databaseStock.InwardQty;
                            if (q > 0)
                                databaseStock.InwardRate = (databaseStock.InwardRate * databaseStock.InwardQty + rate * qty) /
                                                           (qty + databaseStock.InwardQty);
                            else
                                databaseStock.InwardRate = 0;
                            databaseStock.InwardQty += qty;
                            databaseStock.InwardAmt += rate * qty;
                            break;
                        }
                    case StockMaintainTypeEnum.Outward:
                        {
                            var q = qty + databaseStock.OutwardQty;
                            if (q > 0)
                                databaseStock.OutwardRate =
                                    (databaseStock.OutwardRate * databaseStock.OutwardQty + rate * qty) /
                                    (qty + databaseStock.OutwardQty);
                            else
                                databaseStock.OutwardRate = 0;
                            databaseStock.OutwardQty += qty;
                            databaseStock.OutwardAmt += rate * qty;
                            break;
                        }
                }

                await stockMaintainRepository.UpdateAsync(databaseStock);
            }
        }

        private async Task MaintainFifo(MaintainFifoDto data)
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();
            var unitConversion =
                await unitConversionRepository.GetAll().Where(x => x.ProductId == data.ProductId).ToListAsync();
            var unit = unitConversion.FirstOrDefault(x => x.UnitId == data.UnitId);
            var minUnit = unitConversion.MinBy(x => x.ConversionRate);
            decimal qty = 0;
            decimal rate = 0;
            if (unit == null)
            {
                qty = data.Qty;
                rate = data.Rate;
            }
            else
            {
                if (minUnit != null)
                {
                    qty = data.Qty * unit.PrimaryQty * minUnit.Qty / (unit.Qty * minUnit.PrimaryQty);
                    rate = data.Rate * unit.Qty * minUnit.PrimaryQty / (unit.PrimaryQty * minUnit.Qty);
                }
            }

            if ((data.IsIn && qty > 0) || (!data.IsIn && qty < 0))
            {
                var stockFifoData = new StockFifoTable
                {
                    Date = data.Date,
                    DateMiti = data.DateMiti,
                    ProductId = data.ProductId,
                    UnitId = minUnit?.UnitId ?? data.UnitId,
                    Qty = data.IsIn ? qty : -qty,
                    Rate = rate,
                    TenantId = tenantId
                };
                await stockFifoTableRepository.InsertAsync(stockFifoData);
            }
            else
            {
                if (qty > 0)
                {
                    var stockData = await stockFifoTableRepository.GetAll()
                        .Where(x => x.ProductId == data.ProductId)
                        .OrderBy(x => x.Date.Date).ToListAsync();
                    foreach (var stockDatum in stockData)
                        if (qty >= stockDatum.Qty)
                        {
                            await stockFifoTableRepository.DeleteAsync(stockDatum);
                            qty -= stockDatum.Qty;
                            if (qty == 0)
                                break;
                        }
                        else
                        {
                            stockDatum.Qty -= qty;
                            await stockFifoTableRepository.UpdateAsync(stockDatum);
                            break;
                        }
                }
                else
                {
                    qty = -qty;
                    var stockData = await stockFifoTableRepository.GetAll()
                        .Where(x => x.ProductId == data.ProductId)
                        .OrderBy(x => x.Date.Date).ToListAsync();
                    foreach (var stockDatum in stockData)
                        if (qty >= stockDatum.Qty)
                        {
                            await stockFifoTableRepository.DeleteAsync(stockDatum);
                            qty -= stockDatum.Qty;
                            if (qty == 0)
                                break;
                        }
                        else
                        {
                            stockDatum.Qty -= qty;
                            await stockFifoTableRepository.UpdateAsync(stockDatum);
                            break;
                        }
                }
            }
        }


        public async Task<List<Guid>> GetAllChildProductGroups(Guid id)
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();
            var result = new List<Guid>();
            var childIds = await productGroupRepository.GetAll().AsNoTracking()
                .Where(x => x.TenantId == tenantId).Where(x => x.GroupUnder == id).Select(x => x.Id)
                .ToListAsync();
            result.Add(id);
            result.AddRange(childIds);
            foreach (var childId in childIds) result.AddRange(await GetAllChildProductGroups(childId));
            return result.Distinct().ToList();
        }

        public async Task<List<StockCalculationDtoNew>> StockMaintainReport(Guid productGroupId,
            bool isSingleUnit = false)
        {
            var result = new List<StockCalculationDtoNew>();
            //var stockMaintainQuery = stockMaintainRepository.GetAll();
            //if (branchId != Guid.Empty)
            //    stockMaintainQuery = stockMaintainQuery.Where(x => x.BranchId == branchId);
            //if (productGroupId != Guid.Empty)
            //{
            //    var productGroupIds = await GetAllChildProductGroups(productGroupId);
            //   /// stockMaintainQuery = stockMaintainQuery.Where(x => productGroupIds.Contains(x.ProductFk.ProductGroupId));
            //}

            var data = await stockMaintainRepository.GetAll()
                .Include(x => x.ProductFk)
                .Include(x => x.UnitFk)
                .ToListAsync();
            var sn = 1;
            if (!isSingleUnit) return result;
            {
                result = data.Select(x => new StockCalculationDtoNew
                {
                    SlNo = sn++,
                    UnitName = data.FirstOrDefault()?.UnitFk.Name,
                    ProductId = x.ProductId,
                    ProductName = x.ProductFk.Name,
                    OpeningStockQty = x.OpeningQty,
                    OpeningStockValue = x.OpeningAmt,
                    InWardQty = x.InwardQty,
                    InWardValue = x.InwardAmt,
                    OutWardQty = x.OutwardQty,
                    OutWardValue = x.OutwardAmt,
                    ClosingQty = x.OpeningQty + x.InwardQty - x.OutwardQty,
                    ClosingValue = x.OpeningQty * x.OpeningRate + x.InwardQty * x.InwardRate - x.OutwardQty * x.InwardRate
                }).ToList();
                return result;
            }
        }


        public async Task MergeProduct(Guid oldProductId, Guid newProductId)
        {
            var oldProduct = await productRepository.FirstOrDefaultAsync(x => x.Id == oldProductId);
            var newProduct = await productRepository.FirstOrDefaultAsync(x => x.Id == newProductId);
            var oldProductStockMaintains =
                await stockMaintainRepository.FirstOrDefaultAsync(x => x.ProductId == oldProductId);
            var newProductStockMaintains =
                await stockMaintainRepository.FirstOrDefaultAsync(x => x.ProductId == newProductId);
            if (oldProductStockMaintains != null && newProductStockMaintains != null)
            {
                newProductStockMaintains.InwardQty += oldProductStockMaintains.InwardQty;
                newProductStockMaintains.OutwardQty += oldProductStockMaintains.OutwardQty;
                newProductStockMaintains.InwardAmt += oldProductStockMaintains.InwardAmt;
                newProductStockMaintains.OutwardAmt += oldProductStockMaintains.OutwardAmt;
                await stockMaintainRepository.DeleteAsync(x => x.ProductId == oldProductId);
            }

            var stockFifoTableData =
                await stockFifoTableRepository.GetAll().Where(x => x.ProductId == oldProductId).ToListAsync();
            foreach (var stockFifoTable in stockFifoTableData)
            {
                stockFifoTable.ProductId = newProductId;
                if (oldProduct.UnitId != newProduct.UnitId)
                    stockFifoTable.UnitId = newProduct.UnitId;
                await stockFifoTableRepository.UpdateAsync(stockFifoTable);
            }
        }

    }
}