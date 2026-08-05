using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using NextWave.Erp.Dto;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Inventory.Exporting;
using NextWave.Erp.Notifications;
using NextWave.Erp.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Importing
{

    internal class ImportUnitToExcelJob(
        IBinaryObjectManager binaryObjectManager,
        IRepository<Unit, Guid> unitRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IAppNotifier appNotifier,
        IUnitsExcelExporter iUnitExcelExporter,
        IUnitListExcelDataReader unitListExcelDataReader)
        : BackgroundJob<ImportUniversalFromExcelJobArgs>, ITransientDependency
    {
        private readonly IAppNotifier _appNotifier = appNotifier;
        private readonly IUnitsExcelExporter _iUnitExcelExporter = iUnitExcelExporter;

        public override void Execute(ImportUniversalFromExcelJobArgs args)
        {
            var unit = GetUnitListFromExcelOrNull(args);
            if (unit == null || !unit.Any())
            {
                SendInvalidExcelNotification(args);
                return;
            }

            CreateUnit(args, unit);
        }

        private List<GetUnitForViewDto> GetUnitListFromExcelOrNull(ImportUniversalFromExcelJobArgs args)
        {
            using var uow = unitOfWorkManager.Begin();
            using (CurrentUnitOfWork.SetTenantId(args.TenantId))
            {
                try
                {
                    var file = AsyncHelper.RunSync(() => binaryObjectManager.GetOrNullAsync(args.BinaryObjectId));
                    return unitListExcelDataReader.GetUnitFromExcel(file.Bytes);
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

        private void SendInvalidExcelNotification(ImportUniversalFromExcelJobArgs args)
        {
            using var uow = unitOfWorkManager.Begin();
            using (CurrentUnitOfWork.SetTenantId(args.TenantId))
            {
            }

            uow.Complete();
        }

        private void CreateUnit(ImportUniversalFromExcelJobArgs args, List<GetUnitForViewDto> units)
        {
            var invalidUnits = new List<GetUnitForViewDto>();
            foreach (var unit in units)
            {
                using (var uow = unitOfWorkManager.Begin())
                {
                    using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                    {
                        //    if (unit.CanBeImported())
                        //try
                        //{
                        AsyncHelper.RunSync(() => CreateUnitAsync(unit));
                        //    }
                        //    catch (UserFriendlyException exception)
                        //    {
                        //     //   unit.Exception = exception.Message;
                        //        invalidUnits.Add(unit);
                        //    }
                        //    catch (Exception exception)
                        //    {
                        //    //    unit.Exception = exception.ToString();
                        //        invalidUnits.Add(unit);
                        //    }
                        //else
                        //    invalidUnits.Add(unit);
                    }

                    uow.Complete();
                }

                using (var uow = unitOfWorkManager.Begin())
                {
                    using (CurrentUnitOfWork.SetTenantId(args.TenantId))
                    {
                    }

                    uow.Complete();
                }
            }
        }

        private async Task CreateUnitAsync(GetUnitForViewDto input)
        {
            var unitData = await unitRepository.FirstOrDefaultAsync(x => x.Name == input.Name);
            if (unitData == null)
            {
                var tenantId = CurrentUnitOfWork.GetTenantId();

                var unit = new Unit
                {
                    Name = input.Name,
                    FormalName = input.FormalName,
                    TenantId = tenantId
                };
                await unitRepository.InsertAsync(unit);
            }
        }
    }
}
