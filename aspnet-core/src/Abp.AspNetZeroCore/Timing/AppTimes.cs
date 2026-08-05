using Abp.Dependency;
using System;

namespace Abp.AspNetZeroCore.Timing
{
    public class AppTimes : ISingletonDependency
    {
        public DateTime StartupTime { get; set; }
    }
}