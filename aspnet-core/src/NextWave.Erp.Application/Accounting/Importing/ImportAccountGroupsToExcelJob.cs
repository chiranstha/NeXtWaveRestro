using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.UI;
using NextWave.Erp.Accounting.Importing.Dto;
using NextWave.Erp.Dto;
using NextWave.Erp.Notifications;
using NextWave.Erp.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Accounting.Importing
{
    public class ImportAccountGroupsToExcelJob(
     IAppNotifier appNotifier,
     IBinaryObjectManager binaryObjectManager,
     IAccountGroupListExcelDataReader accountGroupListExcelDataReader,
     IRepository<AccountGroup, Guid> accountGroupRepository,
     IUnitOfWorkManager unitOfWorkManager)
     : BackgroundJob<ImportUniversalFromExcelJobArgs>, ITransientDependency
    {
        private readonly IAppNotifier _appNotifier = appNotifier;

        public override void Execute(ImportUniversalFromExcelJobArgs args)
        {
            var accountGroups = GetAccountGroupListFromExcelOrNull(args);
            if (accountGroups == null || !accountGroups.Any())
            {
                SendInvalidExcelNotification(args);
                return;
            }

            CreateAccountGroups(args, accountGroups);
        }

        private List<ImportAccountGroupDto> GetAccountGroupListFromExcelOrNull(ImportUniversalFromExcelJobArgs args)
        {
            using (var uow = unitOfWorkManager.Begin())
            {
                using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                {
                    try
                    {
                        var file = AsyncHelper.RunSync(() => binaryObjectManager.GetOrNullAsync(args.BinaryObjectId));
                        return accountGroupListExcelDataReader.GetAccountGroupsFromExcel(file.Bytes);
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

        private void SendInvalidExcelNotification(ImportUniversalFromExcelJobArgs args)
        {
            using (var uow = unitOfWorkManager.Begin())
            {
                using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                {
                    //AsyncHelper.RunSync(() => _appNotifier.SendMessageAsync(
                    //    args.BinaryObjectId,
                    //    new LocalizableString("FileCantBeConvertedToUserList", ERPConsts.LocalizationSourceName),
                    //    null,
                    //    NotificationSeverity.Warn));
                }

                uow.Complete();
            }
        }

        private void CreateAccountGroups(ImportUniversalFromExcelJobArgs args, List<ImportAccountGroupDto> users)
        {
            var invalidUsers = new List<ImportAccountGroupDto>();

            foreach (var user in users)
                using (var uow = unitOfWorkManager.Begin())
                {
                    using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                    {
                        try
                        {
                            AsyncHelper.RunSync(() => CreateAccountGroupAsync(user));
                        }
                        catch (UserFriendlyException exception)
                        {
                            // user.Exception = exception.Message;
                            invalidUsers.Add(user);
                        }
                        catch (Exception exception)
                        {
                            //  user.Exception = exception.ToString();
                            invalidUsers.Add(user);
                        }
                    }

                    uow.Complete();
                }

            using (var uow = unitOfWorkManager.Begin())
            {
                using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                {
                    //       AsyncHelper.RunSync(() => ProcessImportUsersResultAsync(args, invalidUsers));
                }

                uow.Complete();
            }
        }


        private async Task CreateAccountGroupAsync(ImportAccountGroupDto input)
        {
            var tenantId = CurrentUnitOfWork.GetTenantId();

            //   if (tenantId.HasValue) await _userPolicy.CheckMaxUserCountAsync(tenantId.Value);

            var accountGroup = new AccountGroup
            {
                Name = input.Name,
                Narration = input.Narration,
                IsDefault = input.IsDefault,
                AffectGrossProfit = input.AffectGrossProfit,
                Nature = input.Nature,
                GroupUnder = input.GroupUnder,
                TenantId = (int)tenantId
            };
            await accountGroupRepository.InsertAsync(accountGroup);
        }
    }
}
