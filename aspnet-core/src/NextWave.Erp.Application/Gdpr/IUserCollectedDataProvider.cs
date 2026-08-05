using System.Collections.Generic;
using System.Threading.Tasks;
using Abp;
using NextWave.Erp.Dto;

namespace NextWave.Erp.Gdpr;

public interface IUserCollectedDataProvider
{
    Task<List<FileDto>> GetFiles(UserIdentifier user);
}
