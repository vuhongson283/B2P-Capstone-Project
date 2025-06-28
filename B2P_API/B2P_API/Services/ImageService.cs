using B2P_API.DTOs.ImageDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Response;
using Microsoft.AspNetCore.Http;

namespace B2P_API.Services
{
    public class ImageService : IImageService
    {
        private readonly IImageRepository _imageRepository;
        private readonly IGoogleDriveService _googleDriveService;
        private readonly ILogger<ImageService> _logger;

        public ImageService(IImageRepository imageRepository, IGoogleDriveService googleDriveService, ILogger<ImageService> logger)
        {
            _imageRepository = imageRepository;
            _googleDriveService = googleDriveService;
            _logger = logger;
        }

        public async Task<ImageUploadResponse> UploadImageAsync(IFormFile file, string imageType, int entityId, string? caption = null)
        {
            try
            {
                // Validate image type
                ValidateImageType(imageType);

                // Upload to Google Drive
                var imageUrl = await UploadToGoogleDriveAsync(file, imageType, entityId);

                // Get next order
                var nextOrder = await _imageRepository.GetNextOrderAsync(imageType, entityId);

                // Create image entity
                var image = CreateImageEntity(imageType, entityId, imageUrl, caption, nextOrder);

                // Save to database
                var savedImage = await _imageRepository.CreateAsync(image);

                return new ImageUploadResponse
                {
                    ImageId = savedImage.ImageId,
                    ImageUrl = savedImage.ImageUrl,
                    Order = savedImage.Order ?? 0,
                    Message = "Image uploaded successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image for {ImageType} with ID {EntityId}", imageType, entityId);
                throw;
            }
        }

        private Image CreateImageEntity(string imageType, int entityId, string imageUrl, string? caption, int order)
        {
            var image = new Image
            {
                ImageUrl = imageUrl,
                Caption = caption,
                Order = order
            };

            switch (imageType.ToLower())
            {
                case "facility":
                    image.FacilityId = entityId;
                    break;
                case "blog":
                    image.BlogId = entityId;
                    break;
                case "user":
                    image.UserId = entityId;
                    break;
                case "slide":
                    image.SlideId = entityId;
                    break;
            }

            return image;
        }

        private async Task<string> UploadToGoogleDriveAsync(IFormFile file, string imageType, int entityId)
        {
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            string fileName = $"{imageType}_{entityId}_{DateTime.Now.Ticks}_{file.FileName}";
            string fileId = await _googleDriveService.UploadImageAsync(fileBytes, fileName);
            return await _googleDriveService.CreatePublicLinkAsync(fileId);
        }

        private void ValidateImageType(string imageType)
        {
            var validTypes = new[] { "facility", "blog", "user", "slide" };
            if (!validTypes.Contains(imageType.ToLower()))
            {
                throw new ArgumentException($"Invalid image type: {imageType}");
            }
        }

        public async Task<List<ImageResponseDto>> GetImagesByTypeAndEntityAsync(string imageType, int entityId)
        {
            return await _imageRepository.GetByTypeAndEntityIdAsync(imageType, entityId);
        }

        public async Task<List<ImageResponseDto>> GetFacilityImagesAsync(int facilityId)
        {
            return await _imageRepository.GetByFacilityIdAsync(facilityId);
        }

        public async Task<List<ImageResponseDto>> GetBlogImagesAsync(int blogId)
        {
            return await _imageRepository.GetByBlogIdAsync(blogId);
        }

        public async Task<List<ImageResponseDto>> GetUserImagesAsync(int userId)
        {
            return await _imageRepository.GetByUserIdAsync(userId);
        }

        public async Task<List<ImageResponseDto>> GetSlideImagesAsync(int slideId)
        {
            return await _imageRepository.GetBySlideIdAsync(slideId);
        }

        public async Task<bool> DeleteImageAsync(int imageId)
        {
            return await _imageRepository.DeleteAsync(imageId);
        }

        public async Task<ApiResponse<object>> UpdateImageAsync(int imageId, UpdateImageRequest request)
        {
            try
            {
                var data = await _imageRepository.GetByIdAsync(imageId);
                if (data == null)
                {
                    return new ApiResponse<object>
                    {
                        Data = false,
                        Message = "Image not found",
                        Status = 404,
                        Success = false
                    };
                }

                // Nếu có file mới, upload lên Google Drive và tạo URL mới
                if (request.File != null)
                {
                    // Upload ảnh mới lên Google Drive
                    byte[] fileBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        await request.File.CopyToAsync(memoryStream);
                        fileBytes = memoryStream.ToArray();
                    }

                    // Tạo tên file mới
                    string fileName = $"updated_{imageId}_{DateTime.Now.Ticks}_{request.File.FileName}";

                    // Upload và tạo URL mới
                    string newFileId = await _googleDriveService.UploadImageAsync(fileBytes, fileName);
                    string newImageUrl = await _googleDriveService.CreatePublicLinkAsync(newFileId);

                    // Cập nhật URL mới vào database
                    data.ImageUrl = newImageUrl;
                }

                // Cập nhật các trường khác nếu có
                if (request.Order.HasValue)
                    data.Order = request.Order;

                if (!string.IsNullOrEmpty(request.Caption))
                    data.Caption = request.Caption;

                // Lưu thay đổi vào database
                var result = await _imageRepository.UpdateAsync(data);
                if (!result)
                {
                    return new ApiResponse<object>
                    {
                        Data = false,
                        Message = "Update failed",
                        Status = 500,
                        Success = false
                    };
                }

                return new ApiResponse<object>
                {
                    Data = new
                    {
                        imageId = data.ImageId,
                        imageUrl = data.ImageUrl, // URL mới đã được cập nhật
                        order = data.Order,
                        caption = data.Caption
                    },
                    Message = "Update Image Successfully!!!",
                    Status = 200,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Data = false,
                    Message = ex.Message,
                    Status = 500,
                    Success = false
                };
            }
        }



    }
}
