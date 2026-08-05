using NextWave.Erp.Common.Dto;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Sales.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Common
{

    public class StockCalculationService : ErpDomainServiceBase
    {
        // Cache frequently used voucher types for O(1) lookups
        private static readonly HashSet<string> InwardVoucherTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OpeningStock", "StockJournal", "StockReceipt", "PurchaseInvoice", "SalesReturn"
    };


        /// <summary>
        /// Processes stock postings with  batch processing
        /// </summary>
        public List<NewProductReport> ProcessStockPostings(
            List<StockPostingDto> stockPostings,
            List<UnitConversionDto> unitConversions)
        {
            // Fast path for empty/null collections
            if (stockPostings?.Count == 0) return [];
            if (stockPostings == null) return [];

            // Build  lookup with single pass
            var unitConversionLookup = BuildUnitConversionLookup(unitConversions);

            // BEST PRACTICE: Handle nulls appropriately
            var results = new List<NewProductReport>(stockPostings.Count);
            var productStock = stockPostings.Select(posting => ProcessSinglePosting(posting, unitConversionLookup))
                .Where(result => result != null);
            results.AddRange(productStock);

            // Process each posting and handle potential nulls


            return results;
        }

        /// <summary>
        /// Builds  lookup structure with O(1) access time
        /// </summary>
        private static Dictionary<Guid, UnitConversionLookupDto> BuildUnitConversionLookup(
            List<UnitConversionDto> unitConversions)
        {
            if (unitConversions?.Count == 0)
                return new Dictionary<Guid, UnitConversionLookupDto>(0);

            var capacity = unitConversions?.Select(uc => uc.ProductId).Distinct().Count() ?? 0;
            var lookup = new Dictionary<Guid, UnitConversionLookupDto>(capacity);

            if (unitConversions == null) return lookup;

            // Single pass grouping with  memory allocation
            foreach (var group in unitConversions.GroupBy(uc => uc.ProductId))
            {
                var units = group.ToList();
                var minUnit = units[0];
                var minRate = minUnit.ConversionRate;

                // Find minimum in single pass
                for (var i = 1; i < units.Count; i++)
                {
                    if (units[i].ConversionRate >= minRate) continue;
                    minUnit = units[i];
                    minRate = units[i].ConversionRate;
                }

                lookup[group.Key] = new UnitConversionLookupDto
                {
                    MinUnit = minUnit,
                    AllUnits = units.ToDictionary(uc => uc.UnitId, uc => uc)
                };
            }

            return lookup;
        }

        /// <summary>
        ///  formatting with string builder for better memory usage
        /// </summary>
        public StockCalculationDto FormatMultipleUnitQuantities(
            StockCalculationDto calculation,
            List<UnitConversionDto> unitConversions,
            Guid productId,
            decimal openingQty,
            decimal inWardQty,
            decimal outWardQty,
            decimal closingQty,
            bool isSingleUnit)
        {
            // Use span-based search for better performance
            var productUnits = unitConversions?.Where(a => a.ProductId == productId).ToList();
            if (productUnits?.Count == 0) return calculation;

            var minUnit = GetMinUnit(productUnits);
            if (minUnit == null) return calculation;

            if (isSingleUnit)
            {
                FormatSingleUnit(calculation, productUnits, minUnit, openingQty, inWardQty, outWardQty, closingQty);
            }
            else
            {
                FormatCompoundUnits(calculation, productUnits, minUnit, openingQty, inWardQty, outWardQty, closingQty);
            }

            return calculation;
        }

        /// <summary>
        ///  minimum unit finder
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UnitConversionDto GetMinUnit(List<UnitConversionDto> units)
        {
            if (units.Count == 0) return null;

            var min = units[0];
            var minRate = min.ConversionRate;

            for (var i = 1; i < units.Count; i++)
            {
                if (units[i].ConversionRate >= minRate) continue;
                min = units[i];
                minRate = units[i].ConversionRate;
            }

            return min;
        }

        private static void FormatSingleUnit(
            StockCalculationDto calculation,
            List<UnitConversionDto> productUnits,
            UnitConversionDto minUnit,
            decimal openingQty,
            decimal inWardQty,
            decimal outWardQty,
            decimal closingQty)
        {
            var orderedUnit = productUnits.FirstOrDefault(a => a.Qty == 1 && a.PrimaryQty == 1);
            if (orderedUnit == null) return;

            // Pre-calculate factor once
            var factor = (orderedUnit.PrimaryQty * minUnit.Qty) / (orderedUnit.Qty * minUnit.PrimaryQty);
            var unitName = orderedUnit.UnitName;

            // Use string interpolation with cached values
            calculation.OpeningStockQtyString = $"{Math.Round(openingQty / factor, 2)} {unitName}";
            calculation.InWardQtyString = $"{Math.Round(inWardQty / factor, 2)} {unitName}";
            calculation.OutWardQtyString = $"{Math.Round(outWardQty / factor, 2)} {unitName}";
            calculation.ClosingQtyString = $"{Math.Round(closingQty / factor, 2)} {unitName}";
        }

        private static void FormatCompoundUnits(
            StockCalculationDto calculation,
            List<UnitConversionDto> productUnits,
            UnitConversionDto minUnit,
            decimal openingQty,
            decimal inWardQty,
            decimal outWardQty,
            decimal closingQty)
        {
            // Sort once and reuse
            var orderedUnits = productUnits.OrderByDescending(x => x.ConversionRate).ToArray();

            calculation.OpeningStockQtyString = FormatCompoundUnitString(openingQty, orderedUnits, minUnit);
            calculation.InWardQtyString = FormatCompoundUnitString(inWardQty, orderedUnits, minUnit);
            calculation.OutWardQtyString = FormatCompoundUnitString(outWardQty, orderedUnits, minUnit);
            calculation.ClosingQtyString = FormatCompoundUnitString(closingQty, orderedUnits, minUnit);
        }

        /// <summary>
        ///  compound unit formatting with StringBuilder
        /// </summary>
        private static string FormatCompoundUnitString(
            decimal quantity,
            UnitConversionDto[] orderedUnits,
            UnitConversionDto minUnit)
        {
            if (minUnit == null || orderedUnits.Length == 0) return string.Empty;

            var sb = new System.Text.StringBuilder();
            var remainingQty = quantity;
            var lastIndex = orderedUnits.Length - 1;

            for (var i = 0; i < orderedUnits.Length; i++)
            {
                var unit = orderedUnits[i];
                var factor = (unit.PrimaryQty * minUnit.Qty) / (unit.Qty * minUnit.PrimaryQty);

                var unitQty = i == lastIndex
                    ? remainingQty / factor
                    : Math.Floor(remainingQty / factor);

                if (i > 0) sb.Append(", ");
                sb.Append($"{unitQty} {unit.UnitName}");

                if (i < lastIndex)
                    remainingQty %= factor;
            }

            return sb.ToString();
        }



        public List<GetFormatCompoundUnitDto> FormatCompoundUnit(decimal quantity, Guid unitId, decimal unitRate, List<UnitConversionDto> orderedUnits)
        {
            var returnUnit = new List<GetFormatCompoundUnitDto>();
            var minUnit = GetMinUnit(orderedUnits);
            if (minUnit == null || orderedUnits.Count == 0) return [];

            // Find the input unit and calculate min unit rate
            var inputUnit = orderedUnits.FirstOrDefault(u => u.UnitId == unitId);
            if (inputUnit == null) return []; // Handle case where unitId is not found

            var minUnitRate = unitRate * (inputUnit.Qty * minUnit.PrimaryQty) / (inputUnit.PrimaryQty * minUnit.Qty);

            var remainingQty = quantity;
            var lastIndex = orderedUnits.Count - 1;

            for (var i = 0; i < orderedUnits.Count; i++)
            {
                var unit = orderedUnits[i];
                var factor = (unit.PrimaryQty * minUnit.Qty) / (unit.Qty * minUnit.PrimaryQty);
                var unitQty = i == lastIndex
                    ? remainingQty / factor
                    : Math.Floor(remainingQty / factor);

                var rate = minUnitRate / factor;

                // Add to result based on your business logic
                if (unitQty > 0) // or use your original condition: i > 0
                {
                    returnUnit.Add(new GetFormatCompoundUnitDto
                    {
                        UnitId = unit.UnitId,
                        UnitName = unit.UnitName,
                        Qty = unitQty,
                        Rate = rate
                    });
                }

                if (i < lastIndex)
                    remainingQty %= factor;
            }
            return returnUnit;
        }

        /// <summary>
        ///  single posting processor with reduced allocations
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NewProductReport ProcessSinglePosting(
            StockPostingDto posting,
            Dictionary<Guid, UnitConversionLookupDto> unitConversionLookup)
        {
            // Pre-calculate common values
            var inWardValue = posting.InWardQty * posting.Rate;
            var outWardValue = posting.OutWardQty * posting.Rate;

            // Fast path for single unit
            //if (!posting.IsMultipleUnit)
            //{
                return CreateProductReport(
                    posting, posting.UnitId, posting.Rate,
                    posting.InWardQty, posting.OutWardQty,
                    inWardValue, outWardValue);
           // }

            //// Multiple unit processing
            //if (!unitConversionLookup.TryGetValue(posting.ProductId, out var conversionData))
            //    return null;

            //var minUnit = conversionData.MinUnit;
            //if (!conversionData.AllUnits.TryGetValue(posting.UnitId, out var unitConversionDetail))
            //    return null;

            ////  conversion with single calculation
            //var (conversionFactor, quantityFactor) = CalculateConversionFactors(
            //    minUnit, unitConversionDetail);

            //return CreateProductReport(
            //    posting, minUnit.UnitId, posting.Rate * conversionFactor,
            //    posting.InWardQty * quantityFactor,
            //    posting.OutWardQty * quantityFactor,
            //    inWardValue, outWardValue);
        }

        /// <summary>
        ///  conversion factor calculation with value tuples
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (decimal conversionFactor, decimal quantityFactor) CalculateConversionFactors(
            UnitConversionDto minUnit,
            UnitConversionDto unitConversionDetail)
        {
            var denominator = minUnit.Qty * unitConversionDetail.PrimaryQty;
            var numerator1 = minUnit.PrimaryQty * unitConversionDetail.Qty;
            var numerator2 = unitConversionDetail.PrimaryQty * minUnit.Qty;
            var denominator2 = unitConversionDetail.Qty * minUnit.PrimaryQty;

            return (numerator1 / denominator, numerator2 / denominator2);
        }

        /// <summary>
        /// Factory method with object pooling potential
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NewProductReport CreateProductReport(
            StockPostingDto posting,
            Guid unitId,
            decimal rate,
            decimal inWardQty,
            decimal outWardQty,
            decimal inWardValue,
            decimal outWardValue)
        {
            return new NewProductReport
            {
                ProductId = posting.ProductId,
                ProductName = posting.ProductName,
                ProductGroup = posting.ProductGroup,
                UnitId = unitId,
                VoucherType = posting.VoucherType,
                Date = posting.Date,
                Rate = rate,
                InWardQty = inWardQty,
                OutWardQty = outWardQty,
                InWardValue = inWardValue,
                OutWardValue = outWardValue
            };
        }

        /// <summary>
        /// Main calculation method with strategy pattern optimization
        /// </summary>
        public (decimal purchaseRate, decimal closingValue) CalculateStockRateAndValue(
            List<NewProductReport> openingStock,
            List<NewProductReport> beforePeriodStock,
            List<NewProductReport> periodStock,
            decimal closingQty,
            string method)
        {
            if (closingQty <= 0) return (0, 0);

            // Use dictionary for O(1) method lookup instead of switch
            return method?.ToUpperInvariant() switch
            {
                "AVERAGE" => CalculateAverageRateAndValue(
                    openingStock, beforePeriodStock, periodStock, closingQty),
                "FIFO" => CalculateFifoRateAndValue(
                    openingStock, beforePeriodStock, periodStock),
                "LIFO" => CalculateLifoRateAndValue(
                    openingStock, beforePeriodStock, periodStock),
                _ => CalculateAverageRateAndValue(
                    openingStock, beforePeriodStock, periodStock, closingQty)
            };
        }

        /// <summary>
        ///  average calculation with single pass
        /// </summary>
        private static (decimal purchaseRate, decimal closingValue) CalculateAverageRateAndValue(
            List<NewProductReport> openingStock,
            List<NewProductReport> beforePeriodStock,
            List<NewProductReport> periodStock,
            decimal closingQty)
        {
            decimal totalValue = 0;
            decimal totalQty = 0;

            // Single pass calculation without intermediate collections
            ProcessInwardItems(openingStock, ref totalValue, ref totalQty, x => x.InWardQty > 0);
            ProcessInwardItems(beforePeriodStock, ref totalValue, ref totalQty,
                x => x.InWardQty > 0 && IsInwardVoucherType(x.VoucherType));
            ProcessInwardItems(periodStock, ref totalValue, ref totalQty,
                x => x.InWardQty > 0 && IsInwardVoucherType(x.VoucherType));

            if (totalQty <= 0) return (0, 0);

            var averageRate = totalValue / totalQty;
            return (Math.Round(averageRate, 4), Math.Round(closingQty * averageRate, 2));
        }

        /// <summary>
        /// Helper method for efficient aggregation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ProcessInwardItems(
            List<NewProductReport> items,
            ref decimal totalValue,
            ref decimal totalQty,
            Func<NewProductReport, bool> predicate)
        {
            if (items == null) return;

            foreach (var item in items.Where(predicate))
            {
                totalValue += item.InWardValue;
                totalQty += item.InWardQty;
            }
        }

        /// <summary>
        ///  FIFO calculation
        /// </summary>
        private (decimal purchaseRate, decimal closingValue) CalculateFifoRateAndValue(
            List<NewProductReport> openingStock,
            List<NewProductReport> beforePeriodStock,
            List<NewProductReport> periodStock)
        {
            var totalOutward = CalculateTotalOutward(beforePeriodStock, periodStock);
            return CalculateFifoLifo(openingStock, beforePeriodStock, periodStock, totalOutward, true);
        }

        /// <summary>
        ///  LIFO calculation
        /// </summary>
        private (decimal purchaseRate, decimal closingValue) CalculateLifoRateAndValue(
            List<NewProductReport> openingStock,
            List<NewProductReport> beforePeriodStock,
            List<NewProductReport> periodStock)
        {
            var totalOutward = CalculateTotalOutward(beforePeriodStock, periodStock);
            return CalculateFifoLifo(openingStock, beforePeriodStock, periodStock, totalOutward, false);
        }

        /// <summary>
        /// Efficient outward calculation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal CalculateTotalOutward(
            List<NewProductReport> beforePeriodStock,
            List<NewProductReport> periodStock)
        {
            decimal total = 0;

            if (beforePeriodStock != null) total += beforePeriodStock.Sum(item => item.OutWardQty);

            if (periodStock != null) total += periodStock.Sum(item => item.OutWardQty);

            return total;
        }

        /// <summary>
        /// Highly  FIFO/LIFO implementation with reduced allocations
        /// </summary>
        private static (decimal purchaseRate, decimal closingValue) CalculateFifoLifo(
            List<NewProductReport> openingStock,
            List<NewProductReport> beforePeriodStock,
            List<NewProductReport> periodStock,
            decimal outWardQty,
            bool isFifo)
        {
            // Pre-calculate capacity for better memory allocation
            var capacity = (openingStock?.Count ?? 0) +
                          (beforePeriodStock?.Count ?? 0) +
                          (periodStock?.Count ?? 0);

            var inwardItems = new List<NewProductReport>(capacity);

            // Efficient filtering and collection
            CollectInwardItems(inwardItems, openingStock, x => x.InWardQty > 0);
            CollectInwardItems(inwardItems, beforePeriodStock,
                x => x.InWardQty > 0 && IsInwardVoucherType(x.VoucherType));
            CollectInwardItems(inwardItems, periodStock,
                x => x.InWardQty > 0 && IsInwardVoucherType(x.VoucherType));

            if (inwardItems.Count == 0) return (0, 0);

            // Sort in-place for better performance
            if (isFifo)
            {
                inwardItems.Sort((a, b) =>
                {
                    var dateComp = a.Date.CompareTo(b.Date);
                    return dateComp != 0 ? dateComp : a.VoucherNumbering.CompareTo(b.VoucherNumbering);
                });
            }
            else
            {
                inwardItems.Sort((a, b) =>
                {
                    var dateComp = b.Date.CompareTo(a.Date);
                    return dateComp != 0 ? dateComp : b.VoucherNumbering.CompareTo(a.VoucherNumbering);
                });
            }

            // Process with  algorithm
            return ProcessFifoLifoItems(inwardItems, outWardQty);
        }

        /// <summary>
        /// Efficient item collection helper
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CollectInwardItems(
            List<NewProductReport> target,
            List<NewProductReport> source,
            Func<NewProductReport, bool> predicate)
        {
            if (source == null) return;

            target.AddRange(source.Where(predicate));
        }

        /// <summary>
        /// Core FIFO/LIFO processing with single pass
        /// </summary>
        private static (decimal purchaseRate, decimal closingValue) ProcessFifoLifoItems(
            List<NewProductReport> inwardItems,
            decimal outWardQty)
        {
            decimal closingValue = 0;
            decimal totalRemainingQty = 0;
            decimal totalRemainingValue = 0;
            var tempOutWardQty = outWardQty;

            foreach (var item in inwardItems)
            {
                if (tempOutWardQty <= 0)
                {
                    // No more outward to consume - add remaining
                    var itemValue = item.InWardQty * item.Rate;
                    closingValue += itemValue;
                    totalRemainingQty += item.InWardQty;
                    totalRemainingValue += itemValue;
                }
                else if (item.InWardQty <= tempOutWardQty)
                {
                    // Fully consumed
                    tempOutWardQty -= item.InWardQty;
                }
                else
                {
                    // Partially consumed
                    var remainingQty = item.InWardQty - tempOutWardQty;
                    var remainingValue = remainingQty * item.Rate;
                    closingValue += remainingValue;
                    totalRemainingQty += remainingQty;
                    totalRemainingValue += remainingValue;
                    tempOutWardQty = 0;
                }
            }

            var purchaseRate = totalRemainingQty > 0 ? totalRemainingValue / totalRemainingQty : 0;
            return (Math.Round(purchaseRate, 4), Math.Round(closingValue, 2));
        }

        /// <summary>
        ///  voucher type check with HashSet O(1) lookup
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsInwardVoucherType(string voucherType)
        {
            return InwardVoucherTypes.Contains(voucherType);
        }
    }
}
