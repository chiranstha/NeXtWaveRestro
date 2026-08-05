using Abp.UI;
using SkiaSharp;

namespace NextWave.Erp.Graphics;

public interface IImageValidator
{
   void ValidateDimensions(byte[] imageBytes, int maxWidth, int maxHeight);
}

public class SkiaSharpImageValidator : ErpDomainServiceBase, IImageValidator
{
    public void ValidateDimensions(byte[] imageBytes, int maxWidth, int maxHeight)
    {
        using (var skImage = SKImage.FromEncodedData(imageBytes))
        {
            if (skImage == null)
            {
                throw new UserFriendlyException(L("IncorrectImageFormat"));
            }

            if (skImage.Width > maxWidth || skImage.Height > maxHeight)
            {
                throw new UserFriendlyException(L("IncorrectImageDimensions"));
            }
        }
    }
}

