using Abp.Application.Services;
using NextWave.Erp.ControlPanel.Dtos;
using NextWave.Erp.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.ControlPanel.Documents
{

    public interface IDocumentsAppService : IApplicationService
    {
        Task<GetDocumentDto> GetById(Guid voucherTypeId, string voucherNo);
        Task Update(CreateOrEditDocumentDto input);
        Task Delete(Guid id);
        Task Create(CreateOrEditDocumentDto input);
    }
}
