namespace B2P_API.Services
{
    using B2P_API.Interface;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Drive.v3;
    using Google.Apis.Services;
    using Google.Apis.Util.Store;
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly DriveService _driveService;

        public GoogleDriveService()
        {
            _driveService = GetDriveService();
        }

        private DriveService GetDriveService()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "credentials.json");
            GoogleCredential credential;

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(new[] { DriveService.Scope.Drive });
            }

            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "B2P Image Upload",
            });
        }


        public async Task<string> UploadImageAsync(byte[] imageBytes, string fileName)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = fileName,
                Parents = new List<string> { "0ABjfDp9o6-bOUk9PVA" }
            };

            using var stream = new MemoryStream(imageBytes);
            var request = _driveService.Files.Create(fileMetadata, stream, "image/jpeg");
            request.Fields = "id";
            request.SupportsAllDrives = true;
            
            var progress = await request.UploadAsync();

            if (progress.Status == Google.Apis.Upload.UploadStatus.Completed)
            {
                return request.ResponseBody.Id;
            }

            throw new Exception($"Upload failed: {progress.Exception?.Message}");
        }

        public async Task<string> CreatePublicLinkAsync(string fileId)
        {
            var permission = new Google.Apis.Drive.v3.Data.Permission()
            {
                Type = "anyone",
                Role = "reader"
            };

            var request = _driveService.Permissions.Create(permission, fileId);
            request.SupportsAllDrives = true; 
            await request.ExecuteAsync();     

            return $"https://drive.google.com/uc?id={fileId}";
        }

        public async Task<bool> DeleteFileAsync(string fileId)
        {
            try
            {
                if (string.IsNullOrEmpty(fileId))
                {
                    Console.WriteLine("DeleteFileAsync: FileId is null or empty");
                    return false;
                }

                Console.WriteLine($"DeleteFileAsync: Attempting to delete file with ID: {fileId}");

                var request = _driveService.Files.Delete(fileId);
                request.SupportsAllDrives = true; // Thêm dòng này nếu file trong Shared Drive
                await request.ExecuteAsync();

                Console.WriteLine($"DeleteFileAsync: Successfully deleted file with ID: {fileId}");
                return true;
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"DeleteFileAsync: File not found on Google Drive: {fileId}");
                return true; // File không tồn tại, coi như đã xóa thành công
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteFileAsync: Error deleting file from Google Drive: {ex.Message}");
                return false;
            }
        }
    }
}
