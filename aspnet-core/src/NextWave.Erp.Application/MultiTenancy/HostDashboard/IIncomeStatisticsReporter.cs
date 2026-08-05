using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextWave.Erp.MultiTenancy.HostDashboard.Dto;

namespace NextWave.Erp.MultiTenancy.HostDashboard;

public interface IIncomeStatisticsService
{
    Task<List<IncomeStastistic>> GetIncomeStatisticsData(DateTime startDate, DateTime endDate,
        ChartDateInterval dateInterval);
}
