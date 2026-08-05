using GraphQL.Types;
using NextWave.Erp.Dto;

namespace NextWave.Erp.Types;

public class RoleType : ObjectGraphType<RoleDto>
{
    public RoleType()
    {
        Name = "RoleType";

        Field(x => x.Id);
        Field(x => x.IsDefault);
        Field(x => x.IsStatic);
        Field(x => x.Name);
        Field(x => x.DisplayName);
        Field(x => x.CreationTime);
        Field(x => x.CreatorUserId, nullable: true);
        Field(x => x.LastModificationTime, nullable: true);
        Field(x => x.LastModifierUserId, nullable: true);
        Field(x => x.TenantId, nullable: true);
    }
}

