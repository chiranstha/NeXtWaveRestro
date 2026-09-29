using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    public class RestaurantSmsOutboxWorker : PeriodicBackgroundWorkerBase, ISingletonDependency
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IRepository<RestaurantSmsOutbox, Guid> _outboxRepository;
        private readonly IRestaurantSmsSender _smsSender;

        public RestaurantSmsOutboxWorker(
            AbpTimer timer,
            IUnitOfWorkManager unitOfWorkManager,
            IRepository<RestaurantSmsOutbox, Guid> outboxRepository,
            IRestaurantSmsSender smsSender) : base(timer)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _outboxRepository = outboxRepository;
            _smsSender = smsSender;
            Timer.Period = 30_000;
            Timer.RunOnStart = true;
        }

        protected override void DoWork() => AsyncHelper.RunSync(ProcessAsync);

        private async Task ProcessAsync()
        {
            using var uow = _unitOfWorkManager.Begin();
            var now = DateTime.UtcNow;
            var pending = await _outboxRepository.GetAll()
                .Where(x => (x.Status == RestaurantSmsOutboxStatus.Pending || x.Status == RestaurantSmsOutboxStatus.Failed) &&
                            (!x.NextAttemptAtUtc.HasValue || x.NextAttemptAtUtc <= now))
                .OrderBy(x => x.CreatedAtUtc)
                .Take(20)
                .ToListAsync();

            foreach (var message in pending)
            {
                try
                {
                    await _smsSender.SendAsync(message.PhoneNumber, message.Message);
                    message.Status = RestaurantSmsOutboxStatus.Sent;
                    message.SentAtUtc = DateTime.UtcNow;
                    message.LastError = null;
                    message.NextAttemptAtUtc = null;
                }
                catch (Exception exception)
                {
                    message.Attempts++;
                    message.Status = RestaurantSmsOutboxStatus.Failed;
                    message.LastError = exception.Message.Length > 500 ? exception.Message[..500] : exception.Message;
                    message.NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(Math.Min(60, Math.Pow(2, Math.Min(message.Attempts, 6))));
                }
                await _outboxRepository.UpdateAsync(message);
            }

            await uow.CompleteAsync();
        }
    }
}
