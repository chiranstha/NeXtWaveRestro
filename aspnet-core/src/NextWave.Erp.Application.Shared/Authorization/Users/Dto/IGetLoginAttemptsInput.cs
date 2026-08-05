using Abp.Application.Services.Dto;

namespace NextWave.Erp.Authorization.Users.Dto;

public interface IGetLoginAttemptsInput : ISortedResultRequest
{
    string Filter { get; set; }
}

