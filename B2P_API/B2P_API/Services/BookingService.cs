using B2P_API.DTOs;
using B2P_API.DTOs.BookingDTOs;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Response;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using DnsClient;
using System.Net.Mail;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace B2P_API.Services
{
    public class BookingService
    {
        private readonly BookingRepository _bookingRepo;
        private readonly SportBookingDbContext _context;
        private readonly AccountManagementService _accountservice;

        public BookingService(BookingRepository bookingRepo, SportBookingDbContext context, AccountManagementService accountservice)
        {
            _bookingRepo = bookingRepo;
            _context = context;
            _accountservice = accountservice;
        }


        public async Task<ApiResponse<object>> CreateBookingAsync(BookingRequestDto request)
        {
            User user;

            if (request.UserId.HasValue)
            {
                user = await _context.Users.FindAsync(request.UserId.Value);
                if (user == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Status = 404,
                        Message = "Người dùng không tồn tại",
                        Data = null
                    };
                }
            }
            else
            {
                bool isEmailvalid = await IsRealEmailAsync(request.Email);
                if (!isEmailvalid)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Status = 400,
                        Message = "Email không hợp lệ"
                    };
                }

                if (!IsValidPhone(request.Phone))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Status = 400,
                        Message = "Số điện thoại không hợp lệ"
                    };
                }


                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Phone))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Status = 400,
                        Message = "Khách đặt sân phải cung cấp email và số điện thoại",
                        Data = null
                    };
                }

                user = new User
                {
                    Email = request.Email,
                    Phone = request.Phone,
                    StatusId = 1,
                    RoleId = 3,
                    IsMale = true,
                    CreateAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var slotToCourt = await AssignCourtsAsync(request);
            if (slotToCourt == null)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Status = 409,
                    Message = "Không đủ sân trống để đặt các slot đã chọn",
                    Data = null
                };
            }

            var timeSlotIds = request.TimeSlotIds;
            var slotList = await _context.TimeSlots
                .Where(ts => timeSlotIds.Contains(ts.TimeSlotId))
                .ToDictionaryAsync(ts => ts.TimeSlotId);

            var courtIds = slotToCourt.Values.Distinct().ToList();
            var courtDict = await _context.Courts
                .Where(c => courtIds.Contains(c.CourtId))
                .ToDictionaryAsync(c => c.CourtId);

            decimal total = 0;

            foreach (var kvp in slotToCourt)
            {
                int slotId = kvp.Key;
                int courtId = kvp.Value;

                var slot = slotList[slotId];
                var court = courtDict[courtId];

                total += (decimal)(slot.Discount ?? court.PricePerHour);

            }


            var booking = new Booking
            {
                UserId = user.UserId,
                CreateAt = DateTime.UtcNow,
                StatusId = 1,
                TotalPrice = total,
                IsDayOff = false
            };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            foreach (var pair in slotToCourt)
            {
                var detail = new BookingDetail
                {
                    BookingId = booking.BookingId,
                    CourtId = pair.Value,
                    TimeSlotId = pair.Key,
                    CheckInDate = request.CheckInDate.Date,
                    StatusId = 1,
                    CreateAt = DateTime.UtcNow
                };
                _context.BookingDetails.Add(detail);
            }

            await _context.SaveChangesAsync();

            return new ApiResponse<object>
            {
                Success = true,
                Status = 200,
                Message = "Đặt sân thành công",
                Data = new
                {
                    bookingId = booking.BookingId,
                    checkInDate = request.CheckInDate.Date,
                    user = new
                    {
                        userId = booking.UserId,
                        email = user?.Email,
                        phone = user?.Phone
                    },
                    slots = booking.BookingDetails.Select(d => new
                    {
                        timeSlotId = d.TimeSlotId,
                        startTime = d.TimeSlot?.StartTime,
                        endTime = d.TimeSlot?.EndTime,
                        courtId = d.CourtId,
                        courtName = d.Court?.CourtName
                    })
                }
            };
        }


        /// <summary>
        /// Gán sân tối ưu cho các TimeSlot được yêu cầu
        /// </summary>
        /// <returns>Dictionary TimeSlotId -> CourtId nếu thành công, null nếu không thể xếp</returns>
        public async Task<Dictionary<int, int>?> AssignCourtsAsync(BookingRequestDto request)
        {
            var availability = await _bookingRepo.GetCourtAvailabilityAsync(
    request.FacilityId,
    request.CategoryId,
    request.CheckInDate,
    request.TimeSlotIds
);

            // 🧠 Lấy StartTime tương ứng với từng TimeSlotId
            var slotTimes = await _context.TimeSlots
                .Where(ts => request.TimeSlotIds.Contains(ts.TimeSlotId))
                .ToDictionaryAsync(ts => ts.TimeSlotId, ts => ts.StartTime);

            // 💥 Sort theo StartTime để thuật toán hiểu đúng thứ tự thời gian
            request.TimeSlotIds = request.TimeSlotIds
                .OrderBy(id => slotTimes[id])
                .ToList();

            // Chuẩn bị ma trận: hàng = court, cột = slot
            var courtCount = availability.Count;
            var slotCount = request.TimeSlotIds.Count;

            // Map từ TimeSlotId sang index
            var slotIdToIndex = request.TimeSlotIds
                .Select((slotId, idx) => new { slotId, idx })
                .ToDictionary(x => x.slotId, x => x.idx);

            int[,] matrix = new int[courtCount, slotCount]; // 0: trống, -1: đã đặt

            for (int i = 0; i < courtCount; i++)
            {
                var court = availability[i];
                for (int j = 0; j < slotCount; j++)
                {
                    var slotId = request.TimeSlotIds[j];
                    matrix[i, j] = court.UnavailableSlotIds.Contains(slotId) ? -1 : 0;

                }
            }

            // Gọi thuật toán thông minh
            var slotToCourtIndex = TrySmartBooking(matrix, request.TimeSlotIds);

            if (slotToCourtIndex == null) return null;

            // Chuyển kết quả index về CourtId
            var result = new Dictionary<int, int>();

            for (int j = 0; j < slotCount; j++)
            {
                var slotId = request.TimeSlotIds[j];
                var courtIdx = slotToCourtIndex[j];
                result[slotId] = availability[courtIdx].CourtId;
            }

            return result;

        }

        /*  public async Task<Dictionary<int, int>?> AssignCourtsAsync(BookingRequestDto request)
          {
              // Lấy danh sách sân thuộc Facility và Category được chỉ định
              var courts = await _context.Courts
                  .Where(c => c.FacilityId == request.FacilityId && c.CategoryId == request.CategoryId)
                  .Select(c => new { c.CourtId })
                  .ToListAsync();

              if (!courts.Any()) return null;

              var courtIds = courts.Select(c => c.CourtId).ToList();

              // Lấy danh sách các BookingDetail đang chiếm chỗ các slot đó vào ngày đặt
              var booked = await _context.BookingDetails
                  .Where(d =>
                      courtIds.Contains(d.CourtId) &&
                      d.Booking. == DateOnly.FromDateTime(request.CheckInDate) &&
                      d.Booking.StatusId != 3 // 3 = đã huỷ
                  )
                  .Select(d => new { d.CourtId, d.TimeSlotId })
                  .ToListAsync();

              // Lấy giờ bắt đầu của các TimeSlot để sắp xếp
              var timeSlotTimes = await _context.TimeSlots
                  .Where(ts => request.TimeSlotIds.Contains(ts.TimeSlotId))
                  .ToDictionaryAsync(ts => ts.TimeSlotId, ts => ts.StartTime);

              // Sắp xếp danh sách TimeSlotIds theo StartTime, giữ nguyên duplicate
              var sortedTimeSlotIds = request.TimeSlotIds
                  .OrderBy(id => timeSlotTimes[id])
                  .ToList();

              int courtCount = courtIds.Count;
              int slotCount = sortedTimeSlotIds.Count;

              // Tạo ma trận: hàng = sân, cột = yêu cầu slot cụ thể
              int[,] matrix = new int[courtCount, slotCount];

              for (int i = 0; i < courtCount; i++)
              {
                  int courtId = courtIds[i];
                  var unavailable = booked
                      .Where(b => b.CourtId == courtId)
                      .Select(b => b.TimeSlotId)
                      .ToHashSet();

                  for (int j = 0; j < slotCount; j++)
                  {
                      int slotId = sortedTimeSlotIds[j];
                      matrix[i, j] = unavailable.Contains(slotId) ? -1 : 0;
                  }
              }

              // Thử gán slot
              var result = TrySmartBookingAllowDuplicate(matrix);
              if (result == null) return null;

              // Map kết quả: mỗi slot cụ thể -> sân
              var slotToCourtMap = new Dictionary<int, int>();
              for (int i = 0; i < result.Length; i++)
              {
                  int courtIndex = result[i];
                  if (courtIndex == -1) continue;

                  int slotId = sortedTimeSlotIds[i];
                  int courtId = courtIds[courtIndex];

                  slotToCourtMap.Add(i, courtId); // Key = thứ tự slot trong danh sách gốc
              }

              return slotToCourtMap;
          }*/



        /// <summary>
        /// Thuật toán gán slot vào sân tối ưu.
        /// Ưu tiên các slot liền nhau cùng sân.
        /// </summary>
        private int[]? TrySmartBooking(int[,] matrix, List<int> slotIds)
        {
            int courts = matrix.GetLength(0);
            int slots = slotIds.Count;
            var result = new int[slots];
            Array.Fill(result, -1);

            var slotIdToIndex = slotIds
                .Select((id, idx) => new { id, idx })
                .ToDictionary(x => x.id, x => x.idx);

            var sortedSlotIds = slotIds.OrderBy(x => x).ToList();

            var groups = new List<List<int>>();
            int i = 0;
            while (i < sortedSlotIds.Count)
            {
                var group = new List<int> { sortedSlotIds[i] };
                while (i + 1 < sortedSlotIds.Count && sortedSlotIds[i + 1] == sortedSlotIds[i] + 1)
                {
                    group.Add(sortedSlotIds[i + 1]);
                    i++;
                }
                groups.Add(group);
                i++;
            }

            var tempMatrix = (int[,])matrix.Clone();

            foreach (var groupSlotIds in groups)
            {
                var groupIndices = groupSlotIds.Select(id => slotIdToIndex[id]).ToList();
                int assignedCourt = -1;

                // Ưu tiên gán cả nhóm vào sân đầu tiên có đủ chỗ trống
                for (int c = 0; c < courts; c++)
                {
                    if (groupIndices.All(idx => tempMatrix[c, idx] == 0))
                    {
                        assignedCourt = c;
                        break;
                    }
                }

                if (assignedCourt != -1)
                {
                    foreach (var idx in groupIndices)
                    {
                        tempMatrix[assignedCourt, idx] = 999;
                        result[idx] = assignedCourt;
                    }
                    continue;
                }

                // Nếu không thể gán cả nhóm, gán từng slot lẻ
                foreach (var idx in groupIndices)
                {
                    int courtFound = -1;
                    for (int c = 0; c < courts; c++)
                    {
                        if (tempMatrix[c, idx] == 0)
                        {
                            courtFound = c;
                            break;
                        }
                    }

                    if (courtFound == -1)
                        return null;

                    tempMatrix[courtFound, idx] = 999;
                    result[idx] = courtFound;
                }
            }

            return result;
        }

        public async Task<bool> IsRealEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new MailAddress(email);
                var domain = addr.Host;

                var lookup = new LookupClient();
                var result = await lookup.QueryAsync(domain, QueryType.MX);

                return result.Answers.MxRecords().Any();
            }
            catch
            {
                return false;
            }
        }

        bool IsValidPhone(string? phone)
        {
            return !string.IsNullOrWhiteSpace(phone) &&
                   Regex.IsMatch(phone, @"^(03|05|07|08|09)\d{8}$");
        }

        public async Task<ApiResponse<PagedResponse<BookingResponseDto>>> GetByUserIdAsync(int? userId, BookingQueryParameters queryParams)
        {
            // Validate paging
            if (queryParams.Page <= 0)
                return new() { Success = false, Status = 400, Message = "Page phải lớn hơn 0." };

            if (queryParams.PageSize <= 0)
                return new() { Success = false, Status = 400, Message = "PageSize phải lớn hơn 0." };

            var validSortBy = new[] { "checkindate", "createdate" };
            if (!validSortBy.Contains(queryParams.SortBy?.ToLower()))
                return new()
                {
                    Success = false,
                    Status = 400,
                    Message = $"SortBy không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validSortBy)}"
                };

            var dir = queryParams.SortDirection?.ToLower();
            if (dir != "asc" && dir != "desc")
                return new()
                {
                    Success = false,
                    Status = 400,
                    Message = "SortDirection không hợp lệ. Chỉ chấp nhận: asc hoặc desc."
                };

            // Get count
            var totalItems = await _bookingRepo.CountByUserIdAsync(userId, queryParams.StatusId);
            var totalPages = (int)Math.Ceiling(totalItems / (double)queryParams.PageSize);

            if (totalPages > 0 && queryParams.Page > totalPages)
                return new()
                {
                    Success = false,
                    Status = 400,
                    Message = $"Page vượt quá số trang tối đa ({totalPages})."
                };

            // Get data
            var bookings = await _bookingRepo.GetByUserIdAsync(userId, queryParams);

            // Tải thêm court và timeslot để join tay
            var courtDict = await _context.Courts
                .Include(c => c.Category)
                .ToDictionaryAsync(c => c.CourtId);

            var slotDict = await _context.TimeSlots
                .ToDictionaryAsync(s => s.TimeSlotId);

            var dtoList = bookings.Select(b => new BookingResponseDto
            {
                UserId = userId,
                BookingId = b.BookingId,
                TotalPrice = b.TotalPrice ?? 0,
                CheckInDate = b.BookingDetails.Min(d => d.CheckInDate),
                Status = b.Status?.StatusName ?? "",
                Slots = b.BookingDetails.Select(d =>
                {
                    var court = courtDict.GetValueOrDefault(d.CourtId);
                    var slot = slotDict.GetValueOrDefault(d.TimeSlotId);

                    return new BookingSlotDto
                    {
                        CourtId = d.CourtId,
                        TimeSlotId = d.TimeSlotId,
                        StartTime = slot?.StartTime.GetValueOrDefault().ToTimeSpan() ?? TimeSpan.Zero,
                        EndTime = slot?.EndTime.GetValueOrDefault().ToTimeSpan() ?? TimeSpan.Zero,
                        CourtName = court?.CourtName ?? "",
                        CategoryName = court?.Category?.CategoryName ?? ""
                    };
                }).ToList()
            }).ToList();

            return new ApiResponse<PagedResponse<BookingResponseDto>>
            {
                Success = true,
                Status = 200,
                Message = "Lấy danh sách booking thành công",
                Data = new PagedResponse<BookingResponseDto>
                {
                    CurrentPage = queryParams.Page,
                    ItemsPerPage = queryParams.PageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Items = dtoList
                }
            };
        }

        public async Task<ApiResponse<BookingResponseDto>> GetByIdAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Status)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Court)
                        .ThenInclude(c => c.Category)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.TimeSlot)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                return new ApiResponse<BookingResponseDto>
                {
                    Success = false,
                    Status = 404,
                    Message = $"Không tìm thấy booking với ID = {bookingId}"
                };
            }

            // Tạo Dict cache như bên danh sách
            var courtDict = booking.BookingDetails
                .Select(d => d.Court)
                .DistinctBy(c => c.CourtId)
                .ToDictionary(c => c.CourtId);

            var slotDict = booking.BookingDetails
                .Select(d => d.TimeSlot)
                .DistinctBy(s => s.TimeSlotId)
                .ToDictionary(s => s.TimeSlotId);

            var dto = new BookingResponseDto
            {
                BookingId = booking.BookingId,
                TotalPrice = booking.TotalPrice ?? 0,
                CheckInDate = booking.BookingDetails.Min(d => d.CheckInDate),
                Status = booking.Status?.StatusName ?? "",
                Slots = booking.BookingDetails.Select(d =>
                {
                    var court = courtDict.GetValueOrDefault(d.CourtId);
                    var slot = slotDict.GetValueOrDefault(d.TimeSlotId);

                    return new BookingSlotDto
                    {
                        CourtId = d.CourtId,
                        TimeSlotId = d.TimeSlotId,
                        StartTime = slot?.StartTime.GetValueOrDefault().ToTimeSpan() ?? TimeSpan.Zero,
                        EndTime = slot?.EndTime.GetValueOrDefault().ToTimeSpan() ?? TimeSpan.Zero,
                        CourtName = court?.CourtName ?? "",
                        CategoryName = court?.Category?.CategoryName ?? ""
                    };
                }).ToList()
            };

            return new ApiResponse<BookingResponseDto>
            {
                Success = true,
                Status = 200,
                Message = "Lấy chi tiết booking thành công",
                Data = dto
            };
        }

        public async Task<ApiResponse<string>> MarkBookingCompleteAsync(int bookingId)
        {
            var booking = _bookingRepo.GetById(bookingId);

            if (booking == null)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Status = 404,
                    Message = "Không tìm thấy booking."
                };
            }

            if (booking.StatusId == 10)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Status = 400,
                    Message = "Booking đã hoàn thành trước đó."
                };
            }

            var today = DateTime.Today;
            var earliestCheckIn = booking.BookingDetails.Min(d => d.CheckInDate.Date);

            if (earliestCheckIn > today)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Status = 400,
                    Message = $"Không thể hoàn thành booking trước ngày {earliestCheckIn:dd/MM/yyyy}."
                };
            }

            var allowedStatusToComplete = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }; // Ví dụ 1 = Đã đặt, 2 = Đang sử dụng
            if (!allowedStatusToComplete.Contains(booking.StatusId))
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Status = 400,
                    Message = "Trạng thái hiện tại không cho phép hoàn thành booking."
                };
            }

            booking.StatusId = 10;

            foreach (var detail in booking.BookingDetails)
            {
                detail.StatusId = 10;
            }

            await _context.SaveChangesAsync();

            return new ApiResponse<string>
            {
                Success = true,
                Status = 200,
                Message = "Đã đánh dấu booking là hoàn thành thành công."
            };
        }

        public async Task<ApiResponse<List<TimeSlotAvailability>>> GetTimeSlotAvailabilityAsync(
        int facilityId, int categoryId, DateTime checkInDate)
        {
            var result = await _bookingRepo.GetAvailableCourtCountPerSlotAsync(
                facilityId, categoryId, checkInDate);

            return new ApiResponse<List<TimeSlotAvailability>>
            {
                Success = true,
                Status = 200,
                Message = "Lấy danh sách slot trống thành công.",
                Data = result
            };
        }

        private int[]? TrySmartBookingAllowDuplicate(int[,] matrix)
        {
            int courts = matrix.GetLength(0);
            int slots = matrix.GetLength(1);

            var result = new int[slots];
            Array.Fill(result, -1);

            var used = new bool[courts]; // đánh dấu sân đã dùng tại từng vòng lặp

            for (int j = 0; j < slots; j++)
            {
                bool assigned = false;

                for (int c = 0; c < courts; c++)
                {
                    if (matrix[c, j] == 0 && !used[c])
                    {
                        result[j] = c;
                        used[c] = true;
                        assigned = true;
                        break;
                    }
                }

                if (!assigned)
                {
                    // Không tìm được sân phù hợp
                    return null;
                }

                // Reset used cho slot tiếp theo (vì mỗi slot là yêu cầu độc lập)
                Array.Fill(used, false);
            }

            return result;
        }

    }
}
