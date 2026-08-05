using System.Threading.Tasks;
using NextWave.Erp.Sessions.Dto;

namespace NextWave.Erp.Web.Session;

public interface IPerRequestSessionCache
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformationsAsync();
}

