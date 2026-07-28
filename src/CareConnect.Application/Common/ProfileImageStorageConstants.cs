namespace CareConnect.Application.Common;

public static class ProfileImageStorageConstants
{
    public const string RelativeDirectory = "wwwroot/uploads/profile-images";
    public const string RequestPath = "/uploads/profile-images";
    public const long MaximumUploadBytes = 5 * 1024 * 1024;
    public const int MaximumOutputDimension = 1024;
}
