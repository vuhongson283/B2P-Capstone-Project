using B2P_API.DTOs.FacilityDTOs;
using B2P_API.DTOs.ImageDTOs;
using B2P_API.DTOs.StatuDTOs;
using B2P_API.Interface;
using B2P_API.Response;

namespace B2P_API.Services
{
    public class FacilityService : IFacilityService
    {
        private readonly IFacilityRepository _facilityRepository;
        public FacilityService(IFacilityRepository facilityRepository)
        {
            _facilityRepository = facilityRepository;
        }
        public async Task<ApiResponse<PagedResponse<FacilityWithCourtCountDto>>> GetFacilitiesByUserAsync(
    int userId, string? facilityName = null, int? statusId = null)
        {
            if (userId <= 0)
            {
                return new ApiResponse<PagedResponse<FacilityWithCourtCountDto>>
                {
                    Message = "UserId không hợp lệ",
                    Status = 400,
                    Success = false,
                    Data = new PagedResponse<FacilityWithCourtCountDto>
                    {
                        CurrentPage = 1,
                        ItemsPerPage = 10,
                        Items = new List<FacilityWithCourtCountDto>(),
                        TotalItems = 0,
                        TotalPages = 0,
                    }
                };
            }

            try
            {
                var allFacilities = await _facilityRepository.GetByUserIdAsync(userId);

                // Apply filters
                var filteredFacilities = allFacilities.AsEnumerable();

                if (!string.IsNullOrEmpty(facilityName))
                {
                    filteredFacilities = filteredFacilities
                        .Where(f => f.FacilityName.Contains(facilityName, StringComparison.OrdinalIgnoreCase));
                }

                if (statusId.HasValue)
                {
                    filteredFacilities = filteredFacilities
                        .Where(f => f.Status?.StatusId == statusId.Value);
                }

                // Map to DTO
                var mappedFacilities = filteredFacilities.Select(f => new FacilityWithCourtCountDto
                {
                    FacilityId = f.FacilityId,
                    FacilityName = f.FacilityName,
                    CourtCount = f.Courts?.Count ?? 0,
                    Status = f.Status == null ? null : new StatusDto
                    {
                        StatusId = f.Status.StatusId,
                        StatusName = f.Status.StatusName,
                        StatusDescription = f.Status.StatusDescription
                    },
                    Images = f.Images?.Select(i => new ImageDto
                    {
                        ImageId = i.ImageId,
                        ImageUrl = i.ImageUrl,
                        Order = i.Order,
                        Caption = i.Caption
                    }).ToList()
                }).ToList();

                // Pagination (hiện tại trả page 1 mặc định)
                int totalItems = mappedFacilities.Count;
                int itemsPerPage = 10;
                int totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
                int currentPage = 1;

                var pagedItems = mappedFacilities
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();

                return new ApiResponse<PagedResponse<FacilityWithCourtCountDto>>
                {
                    Message = "Tải dữ liệu cơ sở thành công",
                    Status = 200,
                    Success = true,
                    Data = new PagedResponse<FacilityWithCourtCountDto>
                    {
                        CurrentPage = currentPage,
                        ItemsPerPage = itemsPerPage,
                        Items = pagedItems,
                        TotalItems = totalItems,
                        TotalPages = totalPages
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResponse<FacilityWithCourtCountDto>>
                {
                    Message = ex.Message,
                    Status = 500,
                    Success = false,
                    Data = new PagedResponse<FacilityWithCourtCountDto>
                    {
                        CurrentPage = 1,
                        ItemsPerPage = 10,
                        Items = new List<FacilityWithCourtCountDto>(),
                        TotalItems = 0,
                        TotalPages = 0,
                    }
                };
            }
        }





    }
}
