using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Inventory;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.MultiTenancy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NextWave.Erp.ErpBaseService
{
    public class UnitConversionManager(
         IRepository<UnitConversion, Guid> unitConversionRepository)
         : ErpDomainServiceBase
    {
        public TenantManager TenantManager { get; set; }

        private List<UnitConversion> GetAll()
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();
            return unitConversionRepository.GetAll().Include(x => x.UnitFk).AsNoTracking()
                .Where(x => x.TenantId == tenantId).ToList();
        }

        [UnitOfWork]
        public decimal GetUnitConversion(Guid productId, Guid unitId)
        {
            var unit = GetAll().FirstOrDefault(x => x.ProductId == productId && x.UnitId == unitId);
            return unit?.ConversionRate ?? 0;
        }

        public decimal GetUnitBasePrice(Guid productId, Guid unitId)
        {
            var unit = GetAll().FirstOrDefault(x => x.ProductId == productId && x.UnitId == unitId);
            return unit?.Qty ?? 0;
        }

        public List<UnitConversionServiceDto> GetAllUnitConversion(decimal rate, Guid productId)
        {
            var unitConversions = GetAll().Where(x => x.ProductId == productId).ToList();
            var defaultConversion = unitConversions.FirstOrDefault(x => x.ConversionRate == 1);

            if (defaultConversion == null) return null;
            return (from unitConversion in unitConversions
                    select new UnitConversionServiceDto
                    {
                        UnitId = unitConversion.UnitId,
                        UnitName = unitConversion.UnitFk.Name,
                        Rate = rate * defaultConversion.Qty * unitConversion.PrimaryQty /
                               (defaultConversion.PrimaryQty * unitConversion.Qty)
                    }).ToList();
        }

        public List<UnitConversionServiceDto> GetAllUnitConversionsAll(Guid productId, decimal rate)
        {
            var result = new List<UnitConversionServiceDto>();
            var unitConversions = unitConversionRepository.GetAll().Include(x => x.UnitFk)
                .Where(x => x.ProductId == productId).AsNoTracking().ToList();

            var defaultConversion = unitConversions.FirstOrDefault(y => y.ConversionRate == 1);
            foreach (var unitConversion in unitConversions)
            {
                var data = new UnitConversionServiceDto();

                if (defaultConversion != null)
                {
                    data.Rate = rate * defaultConversion.Qty * unitConversion.PrimaryQty /
                                (defaultConversion.PrimaryQty * unitConversion.Qty);
                    data.UnitId = unitConversion.UnitId;
                    data.UnitName = unitConversion.UnitFk.Name;
                }
                result.Add(data);
            }
            return result;
        }

        public List<UnitConversionServiceDto> GetAllUnitConversions(Guid productId, decimal rate)
        {
            var unitConversions = unitConversionRepository.GetAll()
                .Include(x => x.UnitFk)
                .Where(x => x.ProductId == productId).ToList();
            var defaultConversion = unitConversions.FirstOrDefault(y => y.ConversionRate == 1);

            if (defaultConversion == null) return null;
            return (from unitConversion in unitConversions
                    select new UnitConversionServiceDto
                    {
                        Rate = rate * defaultConversion.Qty * unitConversion.PrimaryQty /
                               (defaultConversion.PrimaryQty * unitConversion.Qty),
                        UnitId = unitConversion.UnitId,
                        UnitName = unitConversion.UnitFk.Name
                    }).ToList();
        }

        public decimal GetStockAmount(Guid productId, Guid unitId, decimal rate, decimal qty)
        {
            var stockQty = GetUnitConversion(productId, unitId) * qty;
            var stockRate = GetUnitBasePrice(productId, unitId) * rate;
            return stockRate * stockQty;
        }
    }
}
