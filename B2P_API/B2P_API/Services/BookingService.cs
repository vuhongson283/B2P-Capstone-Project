using B2P_API.DTOs;
using B2P_API.DTOs.BookingDTOs;
using B2P_API.Models;
using B2P_API.Repository;
using B2P_API.Response;
using Microsoft.EntityFrameworkCore;

namespace B2P_API.Services
{
    public class BookingService
    {
        private readonly BookingRepository _bookingRepo;
        private readonly SportBookingDbContext _context;

        public BookingService(BookingRepository bookingRepo, SportBookingDbContext context)
        {
            _bookingRepo = bookingRepo;
            _context = context;
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



    }
}
