namespace B2P_API.Interface
{
    public interface IGoogleDriveService
    {
        Task<string> UploadImageAsync(byte[] imageBytes, string fileName);
        Task<string> CreatePublicLinkAsync(string fileId);
    }
}
