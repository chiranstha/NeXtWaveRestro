using System.Collections.Generic;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using NextWave.Erp.Editions.Dto;
using NextWave.Erp.Web.Areas.AppAreaName.Models.Common;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Editions;

[AutoMapFrom(typeof(GetEditionEditOutput))]
public class EditEditionModalViewModel : GetEditionEditOutput, IFeatureEditViewModel
{
    public bool IsEditMode => Edition.Id.HasValue;

    public IReadOnlyList<ComboboxItemDto> EditionItems { get; set; }

    public IReadOnlyList<ComboboxItemDto> FreeEditionItems { get; set; }
}

