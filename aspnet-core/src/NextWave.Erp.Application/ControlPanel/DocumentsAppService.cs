using Abp.Domain.Repositories;
using NextWave.Erp.ControlPanel.Documents;
using NextWave.Erp.ControlPanel.Dtos;
using NextWave.Erp.Dto;
using System;
using System.Threading.Tasks;

namespace NextWave.Erp.ControlPanel
{

    public class DocumentsAppService(IRepository<Document, Guid> documentRepository)
        : ErpAppServiceBase, IDocumentsAppService
    {
        public async Task Create(CreateOrEditDocumentDto input)
        {
            var data = new Document
            {
                VoucherTypeId = input.VoucherTypeId,
                VoucherNo = input.VoucherNo,
            };
            if (AbpSession.TenantId != null)
                data.TenantId = AbpSession.TenantId;
        }

        public async Task Delete(Guid id)
        {
            await documentRepository.DeleteAsync(id);
        }

        public async Task<GetDocumentDto> GetById(Guid voucherTypeId, string voucherNo)
        {
            var doc = await documentRepository.FirstOrDefaultAsync(x =>
                x.VoucherTypeId == voucherTypeId && x.VoucherNo == voucherNo);
            var data = new GetDocumentDto
            {
                Id = doc.Id,
                VoucherTypeId = doc.VoucherTypeId,
                VoucherNo = doc.VoucherNo,
                Name = doc.Name,
                Image = doc.Image,
            };
            return data;
        }

        public async Task Update(CreateOrEditDocumentDto input)
        {
            var data1 = await documentRepository.FirstOrDefaultAsync(x => x.Id == input.Id);
            data1.VoucherTypeId = input.VoucherTypeId;
            data1.VoucherNo = input.VoucherNo;
            await documentRepository.UpdateAsync(data1);
        }
    }
}
