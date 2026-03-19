using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace QuizAI.Api.Services;

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<string> UploadRawAsync(Stream fileStream, string fileName, string folder = "quizai-docs")
    {
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = folder,
            PublicId = $"{Guid.NewGuid()}_{Path.GetFileNameWithoutExtension(fileName)}",
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
            throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

        return result.SecureUrl.ToString();
    }

    public async Task DeleteAsync(string publicIdOrUrl)
    {
        var publicId = publicIdOrUrl;

        if (publicIdOrUrl.StartsWith("http"))
        {
            var uri = new Uri(publicIdOrUrl);
            var segments = uri.AbsolutePath.Split('/');
            var uploadIndex = Array.IndexOf(segments, "upload");
            if (uploadIndex >= 0 && uploadIndex + 2 < segments.Length)
            {
                var pathAfterVersion = string.Join("/", segments.Skip(uploadIndex + 2));
                publicId = Path.ChangeExtension(pathAfterVersion, null);
            }
        }

        var deleteParams = new DeletionParams(publicId) { ResourceType = ResourceType.Raw };
        await _cloudinary.DestroyAsync(deleteParams);
    }
}
