using B2P_API.DTOs.FacilityDTOs;
using B2P_API.DTOs.StatuDTOs;
using B2P_API.DTOs.TimeslotDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Response;
using Microsoft.AspNetCore.Http;

namespace B2P_API.Services
{
    public class TimeslotManagementService : ITimeSlotManagementService
    {
        private readonly SportBookingDbContext _context;
        private readonly ITimeSlotManagementRepository _repository;

        public TimeslotManagementService (SportBookingDbContext context, ITimeSlotManagementRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        public async Task<ApiResponse<TimeSlot>> CreateNewTimeSlot(CreateTimeslotRequestDTO request)
        {
            if(request.StartTime >= request.EndTime)
            {
                return new ApiResponse<TimeSlot>
                {
                    Success = false,
                    Status = 400,
                    Message = "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc",
                    Data = null
                };
            }
            var existingSlot = await _repository.GetByFacilityIdAsync(request.FacilityId);

            bool isOverlapping = existingSlot.Any(slot => 
                request.StartTime < slot.EndTime && request.EndTime > slot.StartTime);
            if (isOverlapping)
            {
                return new ApiResponse<TimeSlot>
                {
                    Success = false,
                    Status = 409,
                    Message = "Khung giờ bị trùng với TimeSlot đã tồn tại",
                    Data = null
                };
            }

            var newSlot = new TimeSlot
            {
                FacilityId = request.FacilityId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                StatusId = request.StatusId,
                Discount = request.Discount
            };

            var created = await _repository.CreateAsync(newSlot);
            return new ApiResponse<TimeSlot>
            {
                Success = true,
                Status = 200,
                Message = "Tạo TimeSlot thành công",
                Data = created
            };
        }

        public async Task<ApiResponse<TimeSlot>> DeleteTimeSlot(int timeslotId)
        {
            var slot = await _repository.GetByIdAsync(timeslotId);
            if (slot == null)
            {
                return new ApiResponse<TimeSlot>
                {
                    Success = false,
                    Status = 404,
                    Message = "Không tìm thấy TimeSlot",
                    Data = null
                };
            }

            var result = await _repository.DeleteAsync(timeslotId);
            if (!result)
            {
                return new ApiResponse<TimeSlot>
                {
                    Success = false,
                    Status = 500,
                    Message = "Xóa TimeSlot thất bại",
                    Data = null
                };
            }

            return new ApiResponse<TimeSlot>
            {
                Success = true,
                Status = 200,
                Message = "Xóa TimeSlot thành công",
                Data = slot
            };
        }

        public async Task<ApiResponse<PagedResponse<GetTimeslotDTO>>> GetTimeslotByFacilityIdAsync(
    int facilityId, int? statusId = null, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                // ✅ VALIDATION
                if (facilityId <= 0)
                {
                    return new ApiResponse<PagedResponse<GetTimeslotDTO>>
                    {
                        Success = false,
                        Status = 400,
                        Message = "Facility ID phải lớn hơn 0",
                        Data = null
                    };
                }

                if (pageNumber <= 0 || pageSize <= 0)
                {
                    return new ApiResponse<PagedResponse<GetTimeslotDTO>>
                    {
                        Success = false,
                        Status = 400,
                        Message = "Số trang và kích thước trang phải lớn hơn 0",
                        Data = null
                    };
                }

                // ✅ GET DATA TỪ REPOSITORY
                var all = await _repository.GetByFacilityIdAsync(facilityId);

                // ✅ FILTER BY STATUS
                if (statusId.HasValue)
                {
                    all = all.Where(t => t.StatusId == statusId.Value).ToList();
                }

                // ✅ EMPTY RESULT
                if (!all.Any())
                {
                    return new ApiResponse<PagedResponse<GetTimeslotDTO>>
                    {
                        Success = true,
                        Status = 200,
                        Message = "Không có TimeSlot nào cho facility này",
                        Data = new PagedResponse<GetTimeslotDTO>
                        {
                            CurrentPage = pageNumber,
                            ItemsPerPage = pageSize,
                            Items = new List<GetTimeslotDTO>(),
                            TotalItems = 0,
                            TotalPages = 0
                        }
                    };
                }

                // ✅ PAGINATION
                var totalItems = all.Count;
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var pagedTimeSlots = all
                    .OrderBy(t => t.StartTime)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // ✅ MAP TO DTO - SỬA LẠI CONSISTENT
                var items = pagedTimeSlots.Select(t => new GetTimeslotDTO
                {
                    TimeSlotId = t.TimeSlotId,
                    FacilityId = t.FacilityId,
                    StatusId = t.StatusId,
                    StartTime = t.StartTime?.ToString("HH:mm:ss"), // ✅ FIX: Convert TimeOnly to string
                    EndTime = t.EndTime?.ToString("HH:mm:ss"),     // ✅ FIX: Consistent format
                    Discount = t.Discount,
                    Status = new StatusDto
                    {
                        StatusId = t.Status.StatusId,
                        StatusName = t.Status.StatusName,
                        StatusDescription = t.Status.StatusDescription
                    }
                }).ToList();

                return new ApiResponse<PagedResponse<GetTimeslotDTO>> // ✅ FIX: Correct return type
                {
                    Success = true,
                    Status = 200,
                    Message = "Lấy TimeSlot thành công",
                    Data = new PagedResponse<GetTimeslotDTO> // ✅ FIX: Correct DTO type
                    {
                        CurrentPage = pageNumber,
                        ItemsPerPage = pageSize, // ✅ FIX: Correct variable name
                        Items = items,
                        TotalItems = totalItems,
                        TotalPages = totalPages
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResponse<GetTimeslotDTO>> // ✅ FIX: Correct return type
                {
                    Success = false,
                    Status = 500,
                    Message = "Đã có lỗi xảy ra khi lấy TimeSlot",
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<TimeSlot>> UpdateTimeSlot(CreateTimeslotRequestDTO request, int timeslotId)
        {
            var existing = await _repository.GetByIdAsync(timeslotId);
            if (existing == null)
            {
                return new ApiResponse<TimeSlot>
                {
                    Success = false,
                    Status = 404,
                    Message = "Không tìm thấy TimeSlot cần cập nhật",
                    Data = null
                };
            }

            if (request.StartTime >= request.EndTime)
            {
                return new ApiResponse<TimeSlot>
                {
                    Success = false,
                    Status = 400,
                    Message = "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc",
                    Data = null
                };
            }

            var allSlots = await _repository.GetByFacilityIdAsync(existing.FacilityId ?? 0);

            bool isOverlapping = allSlots
                .Where(x => x.TimeSlotId != timeslotId) // bỏ qua chính nó
                .Any(slot => request.StartTime < slot.EndTime && request.EndTime > slot.StartTime);

            if (isOverlapping)
            {
                return new ApiResponse<TimeSlot>
                {
                    Success = false,
                    Status = 409,
                    Message = "Khung giờ cập nhật bị trùng với TimeSlot khác",
                    Data = null
                };
            }

            // Áp dụng cập nhật
            existing.StartTime = request.StartTime;
            existing.EndTime = request.EndTime;
            existing.StatusId = request.StatusId;
            existing.Discount = request.Discount;

            var updated = await _repository.UpdateAsync(existing);
            if (updated == null)
            {
                return new ApiResponse<TimeSlot>
                {
                    Success = false,
                    Status = 500,
                    Message = "Cập nhật TimeSlot thất bại",
                    Data = null
                };
            }

            return new ApiResponse<TimeSlot>
            {
                Success = true,
                Status = 200,
                Message = "Cập nhật TimeSlot thành công",
                Data = updated
            };
        }
    }
}
