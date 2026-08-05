using Abp.Dependency;

namespace NextWave.Erp.DashboardCustomization.Definitions.Cache;

public interface IDashboardDefinitionCacheManager : ITransientDependency
{
    DashboardDefinition Get(string name);

    void Set(DashboardDefinition definition);
}

