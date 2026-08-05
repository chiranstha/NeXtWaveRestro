using Abp.Configuration;
using Abp.UI;
using NextWave.Erp.Configuration;
using NextWave.Erp.Graphics;
using System;
using System.Threading.Tasks;

namespace NextWave.Erp.Authorization.Users.Profile;

public class ProfilePictureValidator : ErpDomainServiceBase, IProfilePictureValidator
{
    private readonly IImageValidator _imageValidator;

    public ProfilePictureValidator(IImageValidator imageValidator)
    {
        _imageValidator = imageValidator;
    }

    public async Task ValidateProfilePictureDimensions(byte[] imageBytes)
    {
        var (maxWidth, maxHeight) = await GetMaxProfilePictureDimensions();

        _imageValidator.ValidateDimensions(imageBytes, maxWidth, maxHeight);
    }

    public async Task ValidateProfilePictureSize(byte[] imageBytes)
    {
        var maxProfilePictureSizeInByte = await GetMaxProfilePictureSizeInByte();

        if (imageBytes.Length > maxProfilePictureSizeInByte)
        {
            throw new UserFriendlyException(L("ResizedProfilePicture_Warn_SizeLimit", await GetMaxProfilePictureSizeInMb()));
        }
    }

    private async Task<int> GetMaxProfilePictureSizeInByte()
    {
        var maxProfilePictureSizeInMB = await GetMaxProfilePictureSizeInMb();

        var maxProfilePictureSizeInByte = (int)Math.Floor(maxProfilePictureSizeInMB * 1024 * 1024);

        return maxProfilePictureSizeInByte;
    }

    private async Task<float> GetMaxProfilePictureSizeInMb()
    {
        return await SettingManager.GetSettingValueAsync<float>(AppSettings.UserManagement.MaxProfilePictureSizeInMB);
    }

    private async Task<(int Width, int Height)> GetMaxProfilePictureDimensions()
    {
        var maxProfilePictureWidth = await SettingManager.GetSettingValueAsync<int>(AppSettings.UserManagement.MaxProfilePictureWidth);

        var maxProfilePictureHeight = await SettingManager.GetSettingValueAsync<int>(AppSettings.UserManagement.MaxProfilePictureHeight);

        return (maxProfilePictureWidth, maxProfilePictureHeight);
    }
}
