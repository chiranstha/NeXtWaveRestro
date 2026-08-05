using System.Collections.Generic;
using Abp.Application.Services.Dto;
using NextWave.Erp.Editions.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Common;

public interface IFeatureEditViewModel
{
    List<NameValueDto> FeatureValues { get; set; }

    List<FlatFeatureDto> Features { get; set; }
}

