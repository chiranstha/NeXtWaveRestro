#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Collections.Extensions;
using NextWave.Erp.Authorization.Users.Dto;
using NextWave.Erp.DataExporting.Excel.MiniExcel;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;

namespace NextWave.Erp.Authorization.Users.Exporting;

public class UserListExcelExporter : MiniExcelExcelExporterBase, IUserListExcelExporter
{
    private readonly IPropertyInfoHelper _propertyInfoHelper;
    
    public UserListExcelExporter(ITempFileCacheManager tempFileCacheManager, IPropertyInfoHelper propertyInfoHelper) : base(tempFileCacheManager)
    {
        _propertyInfoHelper = propertyInfoHelper;
    }
    
    public async Task<FileDto> ExportToFile(List<UserListDto> userList, List<string> selectedColumns)
    {
        var items = new List<Dictionary<string, object>>();

        foreach (var item in userList)
        {
            if (selectedColumns is { Count: 0 })
            {
                break;
            }

            var rowItem = new Dictionary<string, object>();

            foreach (var selectedColumn in selectedColumns)
            {
                // if the property is found, it will be added to the list of items
                if (typeof(UserListDto).GetProperty(selectedColumn) is { } property)
                {
                    rowItem.Add(property.Name, _propertyInfoHelper.GetConvertedPropertyValue(property, item, HandleLists) ?? "");
                }
            }

            items.Add(rowItem);
        }

        return await CreateExcelPackageAsync("UserList.xlsx", items);
    }

    private static string? HandleLists(PropertyInfo property, object item)
    {
        var propertyType = property.PropertyType;

        if (!typeof(IEnumerable).IsAssignableFrom(propertyType) &&
            !propertyType.IsGenericType &&
            propertyType.GetGenericTypeDefinition() != typeof(List<>))
        {
        }
        
        var genericType = propertyType.GetGenericArguments()[0];

        return genericType switch
        {
            { } when genericType == typeof(UserListRoleDto) => HandleUserListRoleDto(genericType, item),
            _ => null
        };
    }

    private static string? HandleUserListRoleDto(Type genericType, object item)
    {
        if (genericType != typeof(UserListRoleDto)) return null;

        var complexType = (UserListDto)item;
        return complexType.Roles.Select(r => r.RoleName).JoinAsString(", ");
    }
}
