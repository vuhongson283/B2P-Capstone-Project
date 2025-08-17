using B2P_API.DTOs.CourtCategoryDTO;
using B2P_API.DTOs.FacilityDTO;
using B2P_API.DTOs.FacilityDTOs;
using B2P_API.DTOs.ImageDTOs;
using B2P_API.DTOs.StatuDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Utils;
using Microsoft.AspNetCore.Http.HttpResults;
using Twilio.TwiML.Messaging;

namespace B2P_API.Services
{
    public class FacilityService : IFacilityService
    {
        private readonly IFacilityManageRepository _facilityRepository;
        private readonly IFacilityRepositoryForUser _facilityRepositoryForUser;

        public FacilityService(IFacilityManageRepository facilityRepository, IFacilityRepositoryForUser facilityRepositoryForUser)
        {
            _facilityRepository = facilityRepository;
            _facilityRepositoryForUser = facilityRepositoryForUser;
        }

        public async Task<ApiResponse<PagedResponse<FacilityWithCourtCountDto>>> GetFacilitiesByUserAsync(
            int userId, string? facilityName = null, int? statusId = null, int currentPage = 1, int itemsPerPage = 3)
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

                var mappedFacilities = filteredFacilities.Select(f => new FacilityWithCourtCountDto
                {
                    FacilityId = f.FacilityId,
                    FacilityName = f.FacilityName,
                    CourtCount = f.Courts?.Count ?? 0,
                    Location = f.Location,
                    Status = f.Status == null ? null : new StatusDto
                    {
                        StatusId = f.Status.StatusId,
                        StatusName = f.Status.StatusName,
                        StatusDescription = f.Status.StatusDescription
                    },
                    Images = f.Images?
                        .OrderBy(i => i.Order) 
                        .Take(1) 
                        .Select(i => new ImageDto
                        {
                            ImageId = i.ImageId,
                            ImageUrl = i.ImageUrl,
                            Order = i.Order,
                            Caption = i.Caption
                        }).ToList()

                }).ToList();

                int totalItems = mappedFacilities.Count;
                int totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);

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

        public async Task<ApiResponse<PagedResponse<SearchFacilityResponse>>> GetAllFacilitiesByPlayer(
            SearchFormRequest request, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                // Fix 1: Move request null check to the beginning
                if (request == null)
                {
                    return new ApiResponse<PagedResponse<SearchFacilityResponse>>()
                    {
                        Success = false,
                        Message = MessagesCodes.MSG_80,
                        Status = 400, // Changed to 400 for bad request
                        Data = null
                    };
                }

                var facilities = await _facilityRepositoryForUser.GetAllFacilitiesByPlayer();
                var activeFacilities = facilities?.Where(f => f.StatusId == 1).ToList();

                if (activeFacilities == null || activeFacilities.Count == 0)
                {
                    return new ApiResponse<PagedResponse<SearchFacilityResponse>>()
                    {
                        Success = false,
                        Message = MessagesCodes.MSG_72,
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
                else
                {
                    return new ApiResponse<PagedResponse<SearchFacilityResponse>>()
                    {
                        Success = false,
                        Message = MessagesCodes.MSG_72,
                        Status = 404,
                        Data = null
                    };
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
                        Message = MessagesCodes.MSG_72,
                        Status = 404,
                        Data = null
                    };
                }

                var results = filteredList.Select(f =>
                {
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
                            ? $"{minStartTime:HH:mm} - {maxEndTime:HH:mm}"
                            : "Chưa có lịch",
                        FirstImage = f.Images?.OrderBy(img => img.Order).FirstOrDefault()?.ImageUrl,
                        AverageRating = CalculateAverageRating(f),
                        PricePerHour = pricePerHour,
                        MinPrice = pricePerHour * minDiscount,
                        MaxPrice = pricePerHour * maxDiscount
                    };
                }).ToList();

                // Sort
                if (request.Order == 1)
                    results = results.OrderBy(r => r.PricePerHour).ToList();
                else if (request.Order == 2)
                    results = results.OrderByDescending(r => r.PricePerHour).ToList();
                else if (request.Order == 3)
                    results = results.OrderByDescending(r => r.AverageRating).ToList();

                // Pagination
                var totalItems = results.Count;
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                if (pageNumber < 1 || pageNumber > totalPages)
                {
                    return new ApiResponse<PagedResponse<SearchFacilityResponse>>
                    {
                        Data = null,
                        Message = MessagesCodes.MSG_78,
                        Success = false,
                        Status = 400
                    };
                }

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

                return new ApiResponse<PagedResponse<SearchFacilityResponse>>
                {
                    Success = true,
                    Message = $"Tìm thấy {totalItems} cơ sở.",
                    Status = 200,
                    Data = pagedResponse
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResponse<SearchFacilityResponse>>
                {
                    Data = null,
                    Message = MessagesCodes.MSG_06 + ex.Message,
                    Success = false,
                    Status = 500
                };
            }
        }

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

            return allRatings.Any() ? Math.Round((double)allRatings.Average(r => r.Stars), 1) : 0;
        }

        private decimal GetMinPriceForSearchedCategories(Facility facility, List<int>? searchedTypes)
        {
            var courts = facility.Courts?.Where(c => c.PricePerHour.HasValue);

            if (courts == null || !courts.Any())
                return 0;

            if (searchedTypes != null && searchedTypes.Any())
            {
                courts = courts.Where(c => searchedTypes.Contains((int)c.CategoryId));
            }

            return courts.Any() ? courts.Min(c => c.PricePerHour.Value) : 0;
        }

        private TimeOnly RoundTimeToMinute(TimeOnly time)
        {
            if (time.Second >= 30)
                time = time.AddMinutes(1);

            return new TimeOnly(time.Hour, time.Minute);
        }

        public async Task<ApiResponse<Facility>> CreateFacility(CreateFacilityRequest request)
        {
            // Kiểm tra tên cơ sở
            if (string.IsNullOrWhiteSpace(request.FacilityName))
            {
                return new ApiResponse<Facility>
                {
                    Success = false,
                    Status = 400,
                    Message = "Tên cơ sở không được để trống hoặc chỉ chứa khoảng trắng",
                    Data = null
                };
            }

            // Kiểm tra status
            if (request.StatusId <= 0)
            {
                return new ApiResponse<Facility>
                {
                    Success = false,
                    Status = 400,
                    Message = "Trạng thái không hợp lệ",
                    Data = null
                };
            }

            // ✅ FIXED: Kiểm tra giờ mở/đóng cửa hợp lệ
            if (request.OpenHour < 0 || request.OpenHour > 23 ||
                request.CloseHour < 0 || request.CloseHour > 24)
            {
                return new ApiResponse<Facility>
                {
                    Success = false,
                    Status = 400,
                    Message = "Giờ mở/đóng cửa không hợp lệ (0-24)",
                    Data = null
                };
            }

            // Kiểm tra slot duration
            if (request.SlotDuration <= 0 || request.SlotDuration > 180)
            {
                return new ApiResponse<Facility>
                {
                    Success = false,
                    Status = 400,
                    Message = "Thời lượng mỗi lượt phải từ 1 đến 180 phút",
                    Data = null
                };
            }

            try
            {
                var facility = new Facility
                {
                    FacilityName = request.FacilityName.Trim(),
                    Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
                    Contact = string.IsNullOrWhiteSpace(request.Contact) ? null : request.Contact.Trim(),
                    StatusId = request.StatusId,
                    UserId = request.UserId
                };

                // ✅ FIXED: Logic tạo TimeSlots an toàn hơn
                var timeSlots = new List<TimeSlot>();

                // ✅ Handle cross-midnight scenario
                var startHour = request.OpenHour;
                var endHour = request.CloseHour;
                var duration = TimeSpan.FromMinutes(request.SlotDuration);

                // ✅ Nếu đóng cửa là 0 (midnight), convert thành 24
                if (endHour == 0) endHour = 24;

                // ✅ Kiểm tra logic giờ
                if (startHour >= endHour)
                {
                    return new ApiResponse<Facility>
                    {
                        Success = false,
                        Status = 400,
                        Message = "Giờ mở cửa phải nhỏ hơn giờ đóng cửa",
                        Data = null
                    };
                }

                // ✅ Tính toán số slots tối đa có thể
                var totalMinutes = (endHour - startHour) * 60;
                var maxPossibleSlots = totalMinutes / request.SlotDuration;

                // ✅ Giới hạn an toàn
                var slotLimit = Math.Min(maxPossibleSlots, 50); // Tối đa 50 slots

                Console.WriteLine($"🔍 Creating slots: {startHour}:00 to {endHour}:00, duration: {request.SlotDuration}min, max slots: {slotLimit}");

                // ✅ Tạo slots với logic đơn giản hơn
                for (int i = 0; i < slotLimit; i++)
                {
                    var slotStartMinutes = startHour * 60 + (i * request.SlotDuration);
                    var slotEndMinutes = slotStartMinutes + request.SlotDuration;

                    // ✅ Kiểm tra không vượt quá giờ đóng cửa
                    if (slotEndMinutes > endHour * 60)
                    {
                        Console.WriteLine($"⏹️ Stopping at slot {i}: would exceed closing time");
                        break;
                    }

                    var startTime = new TimeOnly(slotStartMinutes / 60, slotStartMinutes % 60);
                    var endTime = new TimeOnly(slotEndMinutes / 60, slotEndMinutes % 60);

                    timeSlots.Add(new TimeSlot
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        StatusId = 1 // ✅ Active by default
                    });

                    Console.WriteLine($"✅ Created slot {i + 1}: {startTime} - {endTime}");
                }

                Console.WriteLine($"🎯 Total slots created: {timeSlots.Count}");

                facility.TimeSlots = timeSlots;
                var created = await _facilityRepository.CreateFacilityAsync(facility);

                return new ApiResponse<Facility>
                {
                    Success = true,
                    Status = 200,
                    Message = $"Tạo cơ sở thành công với {timeSlots.Count} khung giờ",
                    Data = created
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating facility: {ex.Message}");
                return new ApiResponse<Facility>
                {
                    Success = false,
                    Status = 500,
                    Message = "Tạo cơ sở thất bại: " + ex.Message,
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<Facility>> UpdateFacility(UpdateFacilityRequest request, int facilityId)
        {
            var data = await _facilityRepository.GetByIdAsync(facilityId);
            if (data == null)
            {
                return new ApiResponse<Facility>
                {
                    Success = false,
                    Status = 404,
                    Message = "Không tìm thấy cơ sở hợp lệ",
                    Data = null
                };
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(request.FacilityName))
            {
                return new ApiResponse<Facility>
                {
                    Success = false,
                    Status = 400,
                    Message = "Tên cơ sở không được để trống hoặc chỉ chứa khoảng trắng",
                    Data = null
                };
            }

            if (request.StatusId <= 0)
            {
                return new ApiResponse<Facility>
                {
                    Success = false,
                    Status = 400,
                    Message = "Trạng thái không hợp lệ",
                    Data = null
                };
            }

            

            try
            {
                // Update các thuộc tính
                data.FacilityName = request.FacilityName.Trim();
                data.Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();
                data.Contact = string.IsNullOrWhiteSpace(request.Contact) ? null : request.Contact.Trim();
                data.StatusId = request.StatusId;

                
                

                // Gán TimeSlot mới (nếu bạn muốn xóa hết slot cũ)
               

                var updated = await _facilityRepository.UpdateAsync(data);
                if (updated == null)
                {
                    return new ApiResponse<Facility>
                    {
                        Success = false,
                        Status = 500,
                        Message = "Cập nhật thất bại",
                        Data = null
                    };
                }

                return new ApiResponse<Facility>
                {
                    Success = true,
                    Status = 200,
                    Message = "Cập nhật cơ sở thành công",
                    Data = updated
                };

            }
            catch (Exception ex)
            {
                return new ApiResponse<Facility>
                {
                    Success = false,
                    Status = 500,
                    Message = "Cập nhật cơ sở thất bại: " + ex.Message,
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<Facility>> DeleteFacility(int facilityId)
        {
            var facility = await _facilityRepository.GetByIdAsync(facilityId);
            if (facility == null)
            {
                return new ApiResponse<Facility>
                {
                    Success = false,
                    Status = 404,
                    Message = "Không tìm thấy cơ sở hợp lệ",
                    Data = null
                };
            }

            try
            {
                // Kiểm tra xem facility có booking nào đang hoạt động không
                var hasActiveBookings = await _facilityRepository.HasActiveBookingsAsync(facilityId);

                if (hasActiveBookings)
                {
                    return new ApiResponse<Facility>
                    {
                        Success = false,
                        Status = 400,
                        Message = "Không thể xóa cơ sở này vì đang có booking hoạt động",
                        Data = null
                    };
                }

                // Nếu không có booking hoạt động, tiến hành xóa cascade
                var deleted = await _facilityRepository.DeleteCascadeAsync(facilityId);

                if (!deleted)
                {
                    return new ApiResponse<Facility>
                    {
                        Success = false,
                        Status = 500,
                        Message = "Xóa cơ sở thất bại",
                        Data = null
                    };
                }

                return new ApiResponse<Facility>
                {
                    Success = true,
                    Status = 200,
                    Message = "Xóa cơ sở và tất cả dữ liệu liên quan thành công",
                    Data = facility
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Facility>
                {
                    Success = false,
                    Status = 500,
                    Message = $"Đã xảy ra lỗi khi xóa: {ex.Message}",
                    Data = null
                };
            }
        }

        // Replace the conflicted section with this clean version:

        public async Task<ApiResponse<FacilityDetailsDto>> GetFacilityDetails(int facilityId)
        {
            var dto = await _facilityRepositoryForUser.GetFacilityDetails(facilityId);

            if (dto == null)
            {
                return new ApiResponse<FacilityDetailsDto>
                {
                    Success = false,
                    Status = 404,
                    Message = "Không tìm thấy cơ sở.",
                    Data = null
                };
            }

            return new ApiResponse<FacilityDetailsDto>
            {
                Success = true,
                Status = 200,
                Message = "Lấy thông tin cơ sở thành công.",
                Data = dto
            };
        }

        public async Task<ApiResponse<Facility>> GetFacilityById(int facilityId)
        {
            var data = await _facilityRepository.GetByIdAsync(facilityId);
            if (data == null)
            {
                return new ApiResponse<Facility>
                {
                    Success = false,
                    Status = 404,
                    Message = "Không tìm thấy cơ sở hợp lệ",
                    Data = null
                };
            }

            return new ApiResponse<Facility>
            {
                Success = true,
                Status = 200,
                Message = "Lấy thông tin cơ sở thành công",
                Data = data
            };
        }




    }
}

