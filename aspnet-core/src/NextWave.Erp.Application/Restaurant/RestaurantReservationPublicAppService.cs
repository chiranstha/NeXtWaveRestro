using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Configuration;
using NextWave.Erp.MultiTenancy;
using NextWave.Erp.Restaurant.Dtos;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    [AbpAllowAnonymous]
    public class RestaurantReservationPublicAppService(
        IRepository<RestaurantReservationOtpChallenge, Guid> challengeRepository,
        IRepository<RestaurantReservation, Guid> reservationRepository,
        IRepository<RestaurantSmsOutbox, Guid> smsOutboxRepository,
        IRestaurantSmsSender smsSender,
        TenantManager tenantManager) : ErpAppServiceBase, IRestaurantReservationPublicAppService
    {
        private static readonly TimeZoneInfo RestaurantTimeZone = ResolveRestaurantTimeZone();

        public async Task<RequestRestaurantReservationOtpResultDto> RequestOtp(RequestRestaurantReservationOtpDto input)
        {
            var tenantId = await ResolveTenantId(input?.TenantId, input?.TenancyName);
            await EnsureBookingsEnabled(tenantId);
            var phone = NormalizePhone(input?.PhoneNumber);
            var since = DateTime.UtcNow.AddMinutes(-15);
            var sentRecently = await challengeRepository.CountAsync(x =>
                x.TenantId == tenantId && x.PhoneNumber == phone && x.CreatedAtUtc >= since);
            if (sentRecently >= 3)
                throw new UserFriendlyException("Too many verification codes requested. Please try again in 15 minutes.");

            var challengeId = Guid.NewGuid();
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var now = DateTime.UtcNow;
            await smsSender.SendAsync(phone, $"Your restaurant booking verification code is {code}. It expires in 5 minutes.");
            await challengeRepository.InsertAsync(new RestaurantReservationOtpChallenge
            {
                Id = challengeId,
                TenantId = tenantId,
                PhoneNumber = phone,
                CodeHash = HashCode(challengeId, code),
                ExpiresAtUtc = now.AddMinutes(5),
                CreatedAtUtc = now
            });
            return new RequestRestaurantReservationOtpResultDto
            {
                ChallengeId = challengeId.ToString("N"),
                ExpiresAtUtc = now.AddMinutes(5)
            };
        }

        public async Task<CreateRestaurantReservationResultDto> CreateReservation(CreateRestaurantReservationRequestDto input)
        {
            if (input == null) throw new UserFriendlyException("Booking details are required");
            var tenantId = await ResolveTenantId(input.TenantId, input.TenancyName);
            await EnsureBookingsEnabled(tenantId);
            var phone = NormalizePhone(input.PhoneNumber);
            if (string.IsNullOrWhiteSpace(input.GuestName) || input.GuestName.Trim().Length > 200)
                throw new UserFriendlyException("Please enter your name");
            if (input.PartySize is < 1 or > 30)
                throw new UserFriendlyException("Party size must be between 1 and 30");
            if (!Guid.TryParseExact(input.ChallengeId, "N", out var challengeId))
                throw new UserFriendlyException("Request a new verification code before booking");

            var challenge = await challengeRepository.FirstOrDefaultAsync(x =>
                x.Id == challengeId && x.TenantId == tenantId && x.PhoneNumber == phone);
            if (challenge == null || challenge.ConsumedAtUtc.HasValue || challenge.ExpiresAtUtc <= DateTime.UtcNow || challenge.Attempts >= 5)
                return new CreateRestaurantReservationResultDto { Error = "Verification code expired or was already used. Request a new code." };
            challenge.Attempts++;
            if (!FixedHashEquals(challenge.CodeHash, HashCode(challengeId, input.VerificationCode ?? string.Empty)))
            {
                await challengeRepository.UpdateAsync(challenge);
                return new CreateRestaurantReservationResultDto { Error = "Verification code is incorrect" };
            }
            challenge.ConsumedAtUtc = DateTime.UtcNow;
            await challengeRepository.UpdateAsync(challenge);

            var startLocal = DateTime.SpecifyKind(input.StartsAt, DateTimeKind.Unspecified);
            var startsAtUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, RestaurantTimeZone);
            var nowUtc = DateTime.UtcNow;
            if (startsAtUtc < nowUtc.AddMinutes(30) || startsAtUtc > nowUtc.AddDays(90))
                throw new UserFriendlyException("Choose a booking time between 30 minutes and 90 days from now");
            var configuredDuration = int.TryParse(await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantDefaultReservationDurationMinutes, tenantId), out var configured)
                ? configured : 90;
            var durationMinutes = Math.Clamp(configuredDuration, 30, 240);
            var endsAtUtc = startsAtUtc.AddMinutes(durationMinutes);
            var statusToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var reservation = new RestaurantReservation
            {
                TenantId = tenantId,
                Status = NextWave.Erp.Enums.RestaurantReservationStatus.Requested,
                GuestName = input.GuestName.Trim(),
                PhoneNumber = phone,
                Notes = input.Notes?.Trim(),
                PartySize = input.PartySize,
                StartsAtUtc = startsAtUtc,
                EndsAtUtc = endsAtUtc,
                GuestStatusTokenHash = HashToken(statusToken),
                CreatedAtUtc = nowUtc
            };
            reservation.Id = await reservationRepository.InsertAndGetIdAsync(reservation);
            await smsOutboxRepository.InsertAsync(new RestaurantSmsOutbox
            {
                TenantId = tenantId,
                ReservationId = reservation.Id,
                PhoneNumber = phone,
                Message = $"Your reservation request for {ToRestaurantLocal(startsAtUtc):g} is queued for staff review. We will send an update after it is reviewed.",
                Status = NextWave.Erp.Enums.RestaurantSmsOutboxStatus.Pending,
                CreatedAtUtc = nowUtc
            });
            return new CreateRestaurantReservationResultDto
            {
                Id = reservation.Id,
                StatusAccessToken = statusToken,
                Status = reservation.Status,
                StartsAt = ToRestaurantLocal(startsAtUtc),
                EndsAt = ToRestaurantLocal(endsAtUtc)
            };
        }

        public async Task<RestaurantReservationStatusDto> GetStatus(GetRestaurantReservationStatusDto input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.StatusAccessToken) || input.StatusAccessToken.Length > 128)
                throw new UserFriendlyException("Reservation not found");
            var tokenHash = HashToken(input.StatusAccessToken);
            var row = await reservationRepository.GetAll().Include(x => x.TableFk)
                .FirstOrDefaultAsync(x => x.Id == input.ReservationId && x.GuestStatusTokenHash != null);
            if (row == null || !FixedHashEquals(row.GuestStatusTokenHash, tokenHash))
                throw new UserFriendlyException("Reservation not found");
            return new RestaurantReservationStatusDto
            {
                Id = row.Id,
                Status = row.Status,
                StartsAt = ToRestaurantLocal(row.StartsAtUtc),
                EndsAt = ToRestaurantLocal(row.EndsAtUtc),
                TableName = row.TableFk?.Name
            };
        }

        private async Task<int> ResolveTenantId(int? requestedTenantId, string tenancyName)
        {
            var tenant = requestedTenantId.HasValue
                ? await tenantManager.GetByIdAsync(requestedTenantId.Value)
                : await tenantManager.FindByTenancyNameAsync((tenancyName ?? string.Empty).Trim());
            if (tenant == null || !tenant.IsActive)
                throw new UserFriendlyException("Restaurant not found or unavailable");
            return tenant.Id;
        }

        private async Task EnsureBookingsEnabled(int tenantId)
        {
            var enabled = string.Equals(await SettingManager.GetSettingValueForTenantAsync(
                AppSettings.ErpSettings.RestaurantReservationBookingEnabled, tenantId), "true", StringComparison.OrdinalIgnoreCase);
            if (!enabled) throw new UserFriendlyException("Online reservation requests are not enabled for this restaurant");
        }

        private static string NormalizePhone(string raw)
        {
            var value = new string((raw ?? string.Empty).Where(char.IsDigit).ToArray());
            if (value.StartsWith("00977")) value = value[2..];
            if (value.StartsWith("977") && value.Length == 13) value = "+" + value;
            else if (value.Length == 10 && value.StartsWith("9")) value = "+977" + value;
            else if (value.Length is >= 10 and <= 15) value = "+" + value;
            else throw new UserFriendlyException("Enter a valid phone number including country code");
            return value;
        }

        private static string HashCode(Guid challengeId, string code) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{challengeId:N}:{code}")));

        private static string HashToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        private static bool FixedHashEquals(string saved, string candidate)
        {
            if (string.IsNullOrWhiteSpace(saved) || saved.Length != candidate.Length) return false;
            try { return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(saved), Convert.FromHexString(candidate)); }
            catch (FormatException) { return false; }
        }

        private static DateTime ToRestaurantLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), RestaurantTimeZone);

        private static TimeZoneInfo ResolveRestaurantTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kathmandu"); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Nepal Standard Time"); }
        }
    }
}
