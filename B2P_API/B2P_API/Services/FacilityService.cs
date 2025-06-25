<<<<<<< HEAD
﻿using B2P_API.DTOs.FacilityDTOs;
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
=======
﻿using B2P_API.DTOs.FacilityDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Utils;
using System.Linq;

namespace B2P_API.Services
{
    public class FacilityService
    {
        private readonly IFacilityRepository _facilityRepo;

        public FacilityService(IFacilityRepository facilityRepo)
        {
            _facilityRepo = facilityRepo;
        }
        public async Task<ApiResponse<PagedResponse<SearchFacilityResponse>>> GetAllFacilitiesByPlayer(SearchFormRequest request, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var facilities = await _facilityRepo.GetAllFacilitiesByPlayer();
                var activeFacilities = facilities?.Where(f => f.StatusId == 1).ToList();

                if (activeFacilities == null || activeFacilities.Count == 0)
                {
                    return new ApiResponse<PagedResponse<SearchFacilityResponse>>()
                    {
                        Success = false,
                        Message = MessagesCodes.MSG_44,
                        Status = 404,
                        Data = null
                    };
                }

                var filteredFacilities = activeFacilities.AsQueryable();

                if (!string.IsNullOrEmpty(request.Name))
                {
                    filteredFacilities = filteredFacilities.Where(f =>
                        f.FacilityName.Contains(request.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (request.Type != null && request.Type.Any())
                {
                    filteredFacilities = filteredFacilities.Where(f =>
                        f.Courts.Any(court => request.Type.Contains((int)court.CategoryId)));
                }

                if (!string.IsNullOrEmpty(request.City))
                {
                    filteredFacilities = filteredFacilities.Where(f =>
                        !string.IsNullOrEmpty(f.Location) &&
                        f.Location.Contains(request.City, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(request.Ward))
                {
                    filteredFacilities = filteredFacilities.Where(f =>
                        !string.IsNullOrEmpty(f.Location) &&
                        f.Location.Contains(request.Ward, StringComparison.OrdinalIgnoreCase));
                }

                var filteredList = filteredFacilities.ToList();

                if (filteredList.Count == 0)
                {
                    return new ApiResponse<PagedResponse<SearchFacilityResponse>>()
                    {
                        Success = false,
                        Message = "No facilities found matching the search criteria.",
                        Status = 404,
                        Data = null
                    };
                }

                var results = filteredList.Select(f => {
                    var pricePerHour = GetMinPriceForSearchedCategories(f, request.Type);

                    var facilityTimeSlots = f.TimeSlots?
                        .Where(ts => ts.StartTime.HasValue && ts.EndTime.HasValue)
                        .ToList() ?? new List<TimeSlot>();

                    TimeOnly? minStartTime = null;
                    TimeOnly? maxEndTime = null;

                    if (facilityTimeSlots.Any())
                    {
                        minStartTime = RoundTimeToMinute(facilityTimeSlots.Min(ts => ts.StartTime.Value));
                        maxEndTime = RoundTimeToMinute(facilityTimeSlots.Max(ts => ts.EndTime.Value));
                    }

                    var minDiscount = f.TimeSlots?.Where(ts => ts.Discount.HasValue).Any() == true
                        ? f.TimeSlots.Where(ts => ts.Discount.HasValue).Min(ts => ts.Discount.Value)
                        : 0;
                    var maxDiscount = f.TimeSlots?.Where(ts => ts.Discount.HasValue).Any() == true
                        ? f.TimeSlots.Where(ts => ts.Discount.HasValue).Max(ts => ts.Discount.Value)
                        : 0;

                    return new SearchFacilityResponse
                    {
                        FacilityId = f.FacilityId,
                        FacilityName = f.FacilityName,
                        Location = !string.IsNullOrEmpty(f.Location) && f.Location.Contains("$$")
                            ? f.Location.Substring(f.Location.IndexOf("$$") + 2)
                            : f.Location ?? string.Empty,

                        OpenTime = minStartTime.HasValue && maxEndTime.HasValue
                            ? $"{minStartTime.Value.ToString("HH:mm")} - {maxEndTime.Value.ToString("HH:mm")}"
                            : "Chưa có lịch",

                        FirstImage = f.Images?.OrderBy(img => img.Order).FirstOrDefault()?.ImageUrl,

                        AverageRating = CalculateAverageRating(f),

                        PricePerHour = pricePerHour,
                        MinPrice = pricePerHour * minDiscount,
                        MaxPrice = pricePerHour * maxDiscount
                    };
                }).ToList();


                if (request.Order == 1)
                {
                    results = results.OrderBy(r => r.PricePerHour).ToList();
                }
                else if (request.Order == 2) 
                {
                    results = results.OrderByDescending(r => r.PricePerHour).ToList();
                }
                else if (request.Order == 3) 
                {
                    results = results.OrderByDescending(r => r.AverageRating).ToList();
                }

                var totalItems = results.Count;
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                pageNumber = Math.Max(1, Math.Min(pageNumber, totalPages));

                var pagedItems = results
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedResponse = new PagedResponse<SearchFacilityResponse>
                {
                    CurrentPage = pageNumber,
                    ItemsPerPage = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = pagedItems
                };

                return new ApiResponse<PagedResponse<SearchFacilityResponse>>()
                {
                    Success = true,
                    Message = $"Found {totalItems} facilities matching search criteria.",
                    Status = 200,
                    Data = pagedResponse
>>>>>>> QuanCD
                };
            }
            catch (Exception ex)
            {
<<<<<<< HEAD
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
=======
                return new ApiResponse<PagedResponse<SearchFacilityResponse>>
                {
                    Data = null,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
>>>>>>> QuanCD
                };
            }
        }

<<<<<<< HEAD


=======
        private double CalculateAverageRating(Facility facility)
        {
            if (facility.Courts == null || !facility.Courts.Any())
                return 0;

            var allRatings = facility.Courts
                .Where(c => c.BookingDetails != null)
                .SelectMany(c => c.BookingDetails)
                .Where(bd => bd.Booking != null && bd.Booking.Ratings != null)
                .SelectMany(bd => bd.Booking.Ratings)
                .Where(r => r.Stars > 0)
                .ToList();

            if (!allRatings.Any())
                return 0;

            return Math.Round((double)allRatings.Average(r => r.Stars), 1);
        }

        private decimal GetMinPriceForSearchedCategories(Facility facility, List<int>? searchedTypes)
        {
            var courts = facility.Courts?.Where(c => c.PricePerHour.HasValue);

            if (courts == null || !courts.Any())
                return 0;

            // Nếu có filter theo Type, chỉ lấy giá của các sân thuộc Type đã tìm
            if (searchedTypes != null && searchedTypes.Any())
            {
                courts = courts.Where(c => searchedTypes.Contains((int)c.CategoryId));
            }

            return courts.Any() ? courts.Min(c => c.PricePerHour.Value) : 0;
        }
        private TimeOnly RoundTimeToMinute(TimeOnly time)
        {
            if (time.Second >= 30)
            {
                time = time.AddMinutes(1);
            }
            return new TimeOnly(time.Hour, time.Minute);
        }
>>>>>>> QuanCD


    }
}
