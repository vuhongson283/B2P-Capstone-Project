using Microsoft.AspNetCore.Http;
using B2P_API.Response;
using B2P_API.DTOs.ImageDTOs;

namespace B2P_API.Interface
{
    public interface IImageService
    {
        Task<ImageUploadResponse> UploadImageAsync(IFormFile file, string imageType, int entityId, string? caption = null);
        Task<ApiResponse<object>> UpdateImageAsync(int imageId, UpdateImageRequest request);
        Task<List<ImageResponseDto>> GetImagesByTypeAndEntityAsync(string imageType, int entityId);
        Task<List<ImageResponseDto>> GetFacilityImagesAsync(int facilityId);
        Task<List<ImageResponseDto>> GetBlogImagesAsync(int blogId);
        Task<List<ImageResponseDto>> GetUserImagesAsync(int userId);
        Task<List<ImageResponseDto>> GetSlideImagesAsync(int slideId);
        Task<bool> DeleteImageAsync(int imageId);
    }
}
