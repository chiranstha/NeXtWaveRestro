using AutoMapper;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Dto;

namespace NextWave.Erp.Startup;

public static class CustomDtoMapper
{
    public static void CreateMappings(IMapperConfigurationExpression configuration)
    {
        configuration.CreateMap<User, UserDto>()
            .ForMember(dto => dto.Roles, options => options.Ignore())
            .ForMember(dto => dto.OrganizationUnits, options => options.Ignore());
    }
}

