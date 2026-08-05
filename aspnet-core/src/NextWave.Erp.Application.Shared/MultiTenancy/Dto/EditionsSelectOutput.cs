using System.Collections.Generic;
using NextWave.Erp.Editions.Dto;

namespace NextWave.Erp.MultiTenancy.Dto;

public class EditionsSelectOutput
{
    public EditionsSelectOutput()
    {
        AllFeatures = new List<FlatFeatureSelectDto>();
        EditionsWithFeatures = new List<EditionWithFeaturesDto>();
    }

    public List<FlatFeatureSelectDto> AllFeatures { get; set; }

    public List<EditionWithFeaturesDto> EditionsWithFeatures { get; set; }
}

