using Abp.AutoMapper;
using NextWave.Erp.Localization.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Languages;

[AutoMapFrom(typeof(GetLanguageForEditOutput))]
public class CreateOrEditLanguageModalViewModel : GetLanguageForEditOutput
{
    public bool IsEditMode => Language.Id.HasValue;
}

