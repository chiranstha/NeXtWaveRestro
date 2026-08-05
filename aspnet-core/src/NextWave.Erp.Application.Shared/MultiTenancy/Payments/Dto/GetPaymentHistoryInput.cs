using Abp.Runtime.Validation;
using NextWave.Erp.Common;
using NextWave.Erp.Dto;

namespace NextWave.Erp.MultiTenancy.Payments.Dto;

public class GetPaymentHistoryInput : PagedAndSortedInputDto, IShouldNormalize
{
    public void Normalize()
    {
        if (string.IsNullOrEmpty(Sorting))
        {
            Sorting = "CreationTime";
        }

        Sorting = DtoSortingHelper.ReplaceSorting(Sorting, s =>
        {
            return s.Replace("editionDisplayName", "Edition.DisplayName");
        });
    }
}

