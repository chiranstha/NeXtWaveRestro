using System.Collections.Generic;
using Abp.Application.Services.Dto;
using NextWave.Erp.Editions.Dto;

namespace NextWave.Erp.MultiTenancy.Dto;

public class GetTenantFeaturesEditOutput
{
    public List<NameValueDto> FeatureValues { get; set; }

    public List<FlatFeatureDto> Features { get; set; }
}

