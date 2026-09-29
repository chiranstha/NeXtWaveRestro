using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Authorization;
using NextWave.Erp.Configuration;
using NextWave.Erp.Enums;
using NextWave.Erp.Restaurant.Dtos;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    [AbpAuthorize(AppPermissions.PagesRestaurantPos)]
    public class RestaurantCashShiftAppService(
        IRepository<RestaurantCashShift, Guid> shiftRepository,
        IRepository<RestaurantCashMovement, Guid> movementRepository,
        IRepository<RestaurantBillPayment, Guid> billPaymentRepository,
        IRepository<RestaurantBillTender, Guid> billTenderRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : ErpAppServiceBase, IRestaurantCashShiftAppService
    {
        public async Task<RestaurantCashShiftDto> GetCurrent(string registerName = "Main")
        {
            var userId = RequireUserId();
            var shift = await shiftRepository.FirstOrDefaultAsync(x =>
                x.TenantId == AbpSession.TenantId && !x.IsClosed && x.OpenedByUserId == userId &&
                x.RegisterName == NormalizeRegister(registerName));
            return shift == null ? null : await Map(shift);
        }

        public async Task<RestaurantCashShiftDto> Open(OpenRestaurantCashShiftDto input)
        {
            if (input == null || input.OpeningCash < 0)
                throw new UserFriendlyException("Opening cash cannot be negative");

            var tenantId = AbpSession.GetTenantId();
            var userId = RequireUserId();
            var registerName = NormalizeRegister(input.RegisterName);
            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });

            var registerOpen = await shiftRepository.CountAsync(x =>
                x.TenantId == tenantId && !x.IsClosed && x.RegisterName == registerName);
            if (registerOpen > 0)
                throw new UserFriendlyException("This cash register already has an open shift");

            var cashierOpen = await shiftRepository.CountAsync(x =>
                x.TenantId == tenantId && !x.IsClosed && x.OpenedByUserId == userId);
            if (cashierOpen > 0)
                throw new UserFriendlyException("Close your current cash shift before opening another register");

            var shift = new RestaurantCashShift
            {
                TenantId = tenantId,
                RegisterName = registerName,
                OpenedByUserId = userId,
                OpeningCash = input.OpeningCash,
                OpenedAt = DateTime.Now
            };
            await shiftRepository.InsertAsync(shift);
            await uow.CompleteAsync();
            return await Map(shift);
        }

        public async Task<RestaurantCashShiftDto> AddMovement(MoveRestaurantCashDto input)
        {
            if (input == null || input.ShiftId == Guid.Empty || input.Amount <= 0 || input.Amount > 1000000000)
                throw new UserFriendlyException("Enter a valid cash amount");
            var reason = input.Reason?.Trim();
            if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
                throw new UserFriendlyException("A reason of 1 to 500 characters is required");

            var shift = await GetOwnedOpenShift(input.ShiftId);
            await movementRepository.InsertAsync(new RestaurantCashMovement
            {
                TenantId = AbpSession.GetTenantId(),
                CashShiftId = shift.Id,
                IsCashIn = input.IsCashIn,
                Amount = input.Amount,
                Reason = reason,
                CreatedByUserId = RequireUserId(),
                CreatedAt = DateTime.Now
            });
            await CurrentUnitOfWork.SaveChangesAsync();
            return await Map(shift);
        }

        public async Task<RestaurantCashShiftDto> Close(CloseRestaurantCashShiftDto input)
        {
            if (input == null || input.ShiftId == Guid.Empty || input.CountedCash < 0)
                throw new UserFriendlyException("Enter the cash counted at the register");

            using var uow = unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });
            var shift = await GetOwnedOpenShift(input.ShiftId);
            var current = await Map(shift);
            var variance = input.CountedCash - current.ExpectedClosingCash;
            if (input.Note?.Trim().Length > 500)
                throw new UserFriendlyException("The closing note cannot exceed 500 characters");
            if (variance != 0)
                await ValidateManagerApproval(input.ManagerPin);

            shift.IsClosed = true;
            shift.ClosedByUserId = RequireUserId();
            shift.ClosedAt = DateTime.Now;
            shift.CountedClosingCash = input.CountedCash;
            shift.ExpectedClosingCash = current.ExpectedClosingCash;
            shift.CashVariance = variance;
            shift.CloseNote = input.Note?.Trim();
            await shiftRepository.UpdateAsync(shift);
            await uow.CompleteAsync();
            return await Map(shift);
        }

        private async Task<RestaurantCashShiftDto> Map(RestaurantCashShift shift)
        {
            var payments = await billPaymentRepository.GetAll()
                .Where(x => x.TenantId == shift.TenantId && x.CashShiftId == shift.Id)
                .ToListAsync();
            var paymentIds = payments.Select(x => x.Id).ToList();
            var tenders = paymentIds.Count == 0
                ? new System.Collections.Generic.List<RestaurantBillTender>()
                : await billTenderRepository.GetAll()
                    .Where(x => x.TenantId == shift.TenantId && paymentIds.Contains(x.BillPaymentId))
                    .ToListAsync();
            var tenderPayments = tenders.Select(x => x.BillPaymentId).ToHashSet();
            var cashSales = tenders.Where(x => x.PaymentMethod == PaymentMethod.Cash).Sum(x => x.Amount) +
                payments.Where(x => x.PaymentMethod == PaymentMethod.Cash && !tenderPayments.Contains(x.Id))
                    .Sum(x => x.CustomerPaidAmount - x.ReturnAmount);

            var movements = await movementRepository.GetAll()
                .Where(x => x.TenantId == shift.TenantId && x.CashShiftId == shift.Id)
                .ToListAsync();
            var cashIn = movements.Where(x => x.IsCashIn).Sum(x => x.Amount);
            var cashOut = movements.Where(x => !x.IsCashIn).Sum(x => x.Amount);
            return new RestaurantCashShiftDto
            {
                Id = shift.Id,
                RegisterName = shift.RegisterName,
                OpenedByUserId = shift.OpenedByUserId,
                OpenedAt = shift.OpenedAt,
                OpeningCash = shift.OpeningCash,
                CashSales = cashSales,
                CashIn = cashIn,
                CashOut = cashOut,
                ExpectedClosingCash = shift.IsClosed ? shift.ExpectedClosingCash ?? 0 : shift.OpeningCash + cashSales + cashIn - cashOut,
                IsClosed = shift.IsClosed,
                ClosedByUserId = shift.ClosedByUserId,
                ClosedAt = shift.ClosedAt,
                CountedClosingCash = shift.CountedClosingCash,
                CashVariance = shift.CashVariance,
                CloseNote = shift.CloseNote
            };
        }

        private async Task<RestaurantCashShift> GetOwnedOpenShift(Guid id)
        {
            var shift = await shiftRepository.FirstOrDefaultAsync(x =>
                x.Id == id && x.TenantId == AbpSession.TenantId && !x.IsClosed && x.OpenedByUserId == AbpSession.UserId);
            if (shift == null)
                throw new UserFriendlyException("Open cash shift not found for the signed-in cashier");
            return shift;
        }

        private async Task ValidateManagerApproval(string suppliedPin)
        {
            var enabled = string.Equals(await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantRequireManagerPinForSensitiveActions, AbpSession.GetTenantId()),
                "true", StringComparison.OrdinalIgnoreCase);
            var expected = await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantManagerPin, AbpSession.GetTenantId());
            if (!enabled || !RestaurantPinHasher.Verify(expected, suppliedPin, out var legacy))
                throw new UserFriendlyException("A manager approval PIN is required to close a shift with a cash difference");
            if (legacy)
                await SettingManager.ChangeSettingForTenantAsync(AbpSession.GetTenantId(),
                    AppSettings.ErpSettings.RestaurantManagerPin, RestaurantPinHasher.Hash(expected.Trim()));
        }

        private long RequireUserId()
        {
            return AbpSession.UserId ?? throw new UserFriendlyException("A signed-in cashier is required");
        }

        private static string NormalizeRegister(string name)
        {
            var value = name?.Trim();
            return string.IsNullOrWhiteSpace(value) ? "Main" : value.Length > 100 ? value.Substring(0, 100) : value;
        }
    }
}
