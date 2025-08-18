using B2P_API.Models;
using B2P_API.DTOs.ImageDTOs;

namespace B2P_API.Interface
{
    public interface IImageRepository
    {
        Task<Image> CreateAsync(Image image);
        Task<bool> IsImageExisting(int imageId);
        Task<bool> UpdateAsync(Image image);
        Task<List<ImageResponseDto>> GetByFacilityIdAsync(int facilityId);
        Task<List<ImageResponseDto>> GetByBlogIdAsync(int blogId);
        Task<List<ImageResponseDto>> GetByUserIdAsync(int userId);
        Task<List<ImageResponseDto>> GetBySlideIdAsync(int slideId);
        Task<List<ImageResponseDto>> GetByTypeAndEntityIdAsync(string imageType, int entityId);
        Task<Image?> GetByIdAsync(int imageId);
        Task<bool> DeleteAsync(int imageId);
        Task<int> GetNextOrderAsync(string imageType, int entityId);
        Task<Image> CreateUserDefaultImageAsync(int userId);
    }
}
