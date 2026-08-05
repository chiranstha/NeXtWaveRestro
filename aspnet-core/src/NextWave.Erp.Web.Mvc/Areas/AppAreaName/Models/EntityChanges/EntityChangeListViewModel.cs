using Abp.AutoMapper;
using NextWave.Erp.EntityChanges.Dto;
using System.Collections.Generic;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.EntityChanges;

[AutoMapFrom(typeof(EntityAndPropertyChangeListDto))]
public class EntityChangeListViewModel
{
    public List<EntityAndPropertyChangeListDto> EntityAndPropertyChanges { get; set; }
}

