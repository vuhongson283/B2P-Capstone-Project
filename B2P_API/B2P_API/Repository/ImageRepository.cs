using B2P_API.DTOs.ImageDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using Google;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Repository
{
    public class ImageRepository : IImageRepository
    {
        private readonly SportBookingDbContext _context;

        public ImageRepository(SportBookingDbContext context)
        {
            _context = context;
        }

        public async Task<Image> CreateAsync(Image image)
        {
            _context.Images.Add(image);
            await _context.SaveChangesAsync();
            return image;
        }

        public async Task<List<ImageResponseDto>> GetByFacilityIdAsync(int facilityId)
        {
            return await _context.Images
                .Where(i => i.FacilityId == facilityId)
                .OrderBy(i => i.Order)
                .Select(i => new ImageResponseDto
                {
                    ImageId = i.ImageId,
                    FacilityId = i.FacilityId,
                    BlogId = i.BlogId,
                    UserId = i.UserId,
                    SlideId = i.SlideId,
                    ImageUrl = i.ImageUrl,
                    Order = i.Order,
                    Caption = i.Caption
                })
                .ToListAsync();
        }

        public async Task<List<ImageResponseDto>> GetByBlogIdAsync(int blogId)
        {
            return await _context.Images
                .Where(i => i.BlogId == blogId)
                .OrderBy(i => i.Order)
                .Select(i => new ImageResponseDto
                {
                    ImageId = i.ImageId,
                    FacilityId = i.FacilityId,
                    BlogId = i.BlogId,
                    UserId = i.UserId,
                    SlideId = i.SlideId,
                    ImageUrl = i.ImageUrl,
                    Order = i.Order,
                    Caption = i.Caption
                })
                .ToListAsync();
        }

        public async Task<List<ImageResponseDto>> GetByUserIdAsync(int userId)
        {
            return await _context.Images
                .Where(i => i.UserId == userId)
                .OrderBy(i => i.Order)
                .Select(i => new ImageResponseDto
                {
                    ImageId = i.ImageId,
                    FacilityId = i.FacilityId,
                    BlogId = i.BlogId,
                    UserId = i.UserId,
                    SlideId = i.SlideId,
                    ImageUrl = i.ImageUrl,
                    Order = i.Order,
                    Caption = i.Caption
                })
                .ToListAsync();
        }

        public async Task<List<ImageResponseDto>> GetBySlideIdAsync(int slideId)
        {
            return await _context.Images
                .Where(i => i.SlideId == slideId)
                .OrderBy(i => i.Order)
                .Select(i => new ImageResponseDto
                {
                    ImageId = i.ImageId,
                    FacilityId = i.FacilityId,
                    BlogId = i.BlogId,
                    UserId = i.UserId,
                    SlideId = i.SlideId,
                    ImageUrl = i.ImageUrl,
                    Order = i.Order,
                    Caption = i.Caption
                })
                .ToListAsync();
        }

        public async Task<List<ImageResponseDto>> GetByTypeAndEntityIdAsync(string imageType, int entityId)
        {
            return imageType.ToLower() switch
            {
                "facility" => await GetByFacilityIdAsync(entityId),
                "blog" => await GetByBlogIdAsync(entityId),
                "user" => await GetByUserIdAsync(entityId),
                "slide" => await GetBySlideIdAsync(entityId),
                _ => new List<ImageResponseDto>()
            };
        }

        public async Task<Image?> GetByIdAsync(int imageId)
        {
            return await _context.Images.FindAsync(imageId);
        }

        public async Task<bool> DeleteAsync(int imageId)
        {
            var image = await _context.Images.FindAsync(imageId);
            if (image == null) return false;

            _context.Images.Remove(image);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetNextOrderAsync(string imageType, int entityId)
        {
            var query = _context.Images.AsQueryable();

            query = imageType.ToLower() switch
            {
                "facility" => query.Where(i => i.FacilityId == entityId),
                "blog" => query.Where(i => i.BlogId == entityId),
                "user" => query.Where(i => i.UserId == entityId),
                "slide" => query.Where(i => i.SlideId == entityId),
                _ => query.Where(i => false) // Empty result
            };

            var maxOrder = await query.MaxAsync(i => (int?)i.Order) ?? 0;
            return maxOrder + 1;
        }
        
        public async Task<bool> UpdateAsync(Image image)
        {
            _context.Images.Update(image);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsImageExisting(int imageId)
        {
            var data = _context.Images.FirstOrDefault(x => x.ImageId == imageId);
            if(data == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
