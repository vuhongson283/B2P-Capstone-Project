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
using B2P_API.Interface;
using B2P_API.DTOs.RatingDTO;
using B2P_API.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography;

namespace B2P_API.Services
{
    public class BookingService
    {
        private readonly IBookingRepository _bookingRepo;
        private readonly IAccountManagementRepository _accRepo;
        private readonly IAccountRepository _accRepo2;
        private readonly IHubContext<BookingHub> _hubContext;

        public BookingService(
            IBookingRepository bookingRepo,  
            IAccountManagementRepository accRepo,
            IHubContext<BookingHub> hubContext,
            IAccountRepository accRepo2)
        {
            _bookingRepo = bookingRepo;          
            _accRepo = accRepo;
            _accRepo2 = accRepo2;
            _hubContext = hubContext;
        }


        public async Task<ApiResponse<object>> CreateBookingAsync(BookingRequestDto request)
        {
            User user;

            // Kiểm tra user theo Id
            if (request.UserId.HasValue)
            {
                user = await _accRepo.GetByIdAsync(request.UserId.Value);
                if (user == null)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Status = 404,
                        Message = "Người dùng không tồn tại"
                    };
                }
            }
            else
            {
                // Bắt buộc có cả email và số điện thoại
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Phone))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Status = 400,
                        Message = "Khách đặt sân phải cung cấp email và số điện thoại"
                    };
                }

                // Kiểm tra email hợp lệ
                bool isEmailValid = await IsRealEmailAsync(request.Email);
                if (!isEmailValid)
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Status = 400,
                        Message = "Email không hợp lệ"
                    };
                }

                // Kiểm tra số điện thoại hợp lệ
                if (!IsValidPhone(request.Phone))
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Status = 400,
                        Message = "Số điện thoại không hợp lệ"
                    };
                }

                // Tạo user mới
                user = new User
                {
                    Email = request.Email,
                    Phone = request.Phone,
                    StatusId = 1,
                    RoleId = 3,
                    IsMale = true,
                    CreateAt = DateTime.UtcNow
                };

                await _accRepo2.RegisterAccountAsync(user);
            }

            // Gán slot vào sân
            var slotToCourt = await AssignCourtsAsync(request);
            if (slotToCourt == null)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Status = 409,
                    Message = "Không đủ sân trống để đặt các slot đã chọn"
                };
            }

            // Lấy danh sách time slot và court
            var timeSlotIds = request.TimeSlotIds;
            var slotList = await _bookingRepo.GetTimeSlotsByIdsAsync(timeSlotIds);

            var courtIds = slotToCourt.Values.Distinct().ToList();
            var courtDict = await _bookingRepo.GetCourtsByIdsAsync(courtIds);

            // Tính tổng tiền
            decimal total = 0;
            foreach (var kvp in slotToCourt)
            {
                int slotId = kvp.Key;
                int courtId = kvp.Value;

                var slot = slotList[slotId];
                var court = courtDict[courtId];

                total += (decimal)(slot.Discount ?? court.PricePerHour);
            }

            // Tạo booking
            var booking = new Booking
            {
                UserId = user.UserId,
                CreateAt = DateTime.UtcNow,
                StatusId = 8,
                TotalPrice = total,
                IsDayOff = false
            };

            await _bookingRepo.AddBookingAsync(booking);

            // Tạo chi tiết đặt sân
            var details = slotToCourt.Select(pair => new BookingDetail
            {
                BookingId = booking.BookingId,
                CourtId = pair.Value,
                TimeSlotId = pair.Key,
                CheckInDate = request.CheckInDate.Date,
                StatusId = 1,
                CreateAt = DateTime.UtcNow
            }).ToList();

            await _bookingRepo.AddBookingDetailsAsync(details);

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
                    slots = details.Select(d => new
                    {
                        timeSlotId = d.TimeSlotId,
                        startTime = slotList[d.TimeSlotId].StartTime,
                        endTime = slotList[d.TimeSlotId].EndTime,
                        courtId = d.CourtId,
                        courtName = courtDict[d.CourtId].CourtName
                    })
                }
            };
        }
		public async Task<ApiResponse<object>> MarkSmartSlot(BookingRequestDto request)
		{
			User user;

			// Kiểm tra user theo Id
			if (request.UserId.HasValue)
			{
				user = await _accRepo.GetByIdAsync(request.UserId.Value);
				if (user == null)
				{
					return new ApiResponse<object>
					{
						Success = false,
						Status = 404,
						Message = "Người dùng không tồn tại"
					};
				}
			}
			else
			{
				// Bắt buộc có cả email và số điện thoại
				if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Phone))
				{
					return new ApiResponse<object>
					{
						Success = false,
						Status = 400,
						Message = "Khách đặt sân phải cung cấp email và số điện thoại"
					};
				}

				// Kiểm tra email hợp lệ
				bool isEmailValid = await IsRealEmailAsync(request.Email);
				if (!isEmailValid)
				{
					return new ApiResponse<object>
					{
						Success = false,
						Status = 400,
						Message = "Email không hợp lệ"
					};
				}

				// Kiểm tra số điện thoại hợp lệ
				if (!IsValidPhone(request.Phone))
				{
					return new ApiResponse<object>
					{
						Success = false,
						Status = 400,
						Message = "Số điện thoại không hợp lệ"
					};
				}

				// Tạo user mới
				user = new User
				{
					Email = request.Email,
					Phone = request.Phone,
					StatusId = 1,
					RoleId = 3,
					IsMale = true,
					CreateAt = DateTime.UtcNow
				};

				await _accRepo2.RegisterAccountAsync(user);
			}

			// Gán slot vào sân
			var slotToCourt = await AssignCourtsAsync(request);
			if (slotToCourt == null)
			{
				return new ApiResponse<object>
				{
					Success = false,
					Status = 409,
					Message = "Không đủ sân trống để đặt các slot đã chọn"
				};
			}

			// Lấy danh sách time slot và court
			var timeSlotIds = request.TimeSlotIds;
			var slotList = await _bookingRepo.GetTimeSlotsByIdsAsync(timeSlotIds);

			var courtIds = slotToCourt.Values.Distinct().ToList();
			var courtDict = await _bookingRepo.GetCourtsByIdsAsync(courtIds);

			// Tính tổng tiền
			decimal total = 0;
			foreach (var kvp in slotToCourt)
			{
				int slotId = kvp.Key;
				int courtId = kvp.Value;

				var slot = slotList[slotId];
				var court = courtDict[courtId];

				total += (decimal)(slot.Discount ?? court.PricePerHour);
			}

			// Tạo booking
			var booking = new Booking
			{
				UserId = user.UserId,
				CreateAt = DateTime.UtcNow,
				StatusId = 7,
				TotalPrice = total,
				IsDayOff = false
			};

			await _bookingRepo.AddBookingAsync(booking);

			// Tạo chi tiết đặt sân
			var details = slotToCourt.Select(pair => new BookingDetail
			{
				BookingId = booking.BookingId,
				CourtId = pair.Value,
				TimeSlotId = pair.Key,
				CheckInDate = request.CheckInDate.Date,
				StatusId = 1,
				CreateAt = DateTime.UtcNow
			}).ToList();

			await _bookingRepo.AddBookingDetailsAsync(details);

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
					slots = details.Select(d => new
					{
						timeSlotId = d.TimeSlotId,
						startTime = slotList[d.TimeSlotId].StartTime,
						endTime = slotList[d.TimeSlotId].EndTime,
						courtId = d.CourtId,
						courtName = courtDict[d.CourtId].CourtName
					})
				}
			};
		}




		public async Task<ApiResponse<object>> CreateSimpleBookingAsync(SimpleBookingDto request)
		{
			User user;

			// ✅ COPY: Logic user từ hàm cũ
			if (request.UserId.HasValue)
			{
				user = await _accRepo.GetByIdAsync(request.UserId.Value);
				if (user == null)
				{
					return new ApiResponse<object>
					{
						Success = false,
						Status = 404,
						Message = "Người dùng không tồn tại"
					};
				}
			}
			else
			{
				// Bắt buộc có cả email và số điện thoại
				if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Phone))
				{
					return new ApiResponse<object>
					{
						Success = false,
						Status = 400,
						Message = "Khách đặt sân phải cung cấp email và số điện thoại"
					};
				}

				// Kiểm tra email hợp lệ
				bool isEmailValid = await IsRealEmailAsync(request.Email);
				if (!isEmailValid)
				{
					return new ApiResponse<object>
					{
						Success = false,
						Status = 400,
						Message = "Email không hợp lệ"
					};
				}

				// Kiểm tra số điện thoại hợp lệ
				if (!IsValidPhone(request.Phone))
				{
					return new ApiResponse<object>
					{
						Success = false,
						Status = 400,
						Message = "Số điện thoại không hợp lệ"
					};
				}

				// Tạo user mới
				user = new User
				{
					Email = request.Email,
					Phone = request.Phone,
					StatusId = 1,
					RoleId = 3,
					IsMale = true,
					CreateAt = DateTime.UtcNow
				};

				await _accRepo2.RegisterAccountAsync(user);
			}

			// ✅ THAY: Thay vì AssignCourtsAsync - dùng direct mapping
			var slotToCourt = new Dictionary<int, int>
	{
		{ request.TimeSlotId, request.CourtId }
	};

			// ✅ COPY: Logic lấy slot và court từ hàm cũ
			var timeSlotIds = new List<int> { request.TimeSlotId };
			var slotList = await _bookingRepo.GetTimeSlotsByIdsAsync(timeSlotIds);

			var courtIds = new List<int> { request.CourtId };
			var courtDict = await _bookingRepo.GetCourtsByIdsAsync(courtIds);

			// ✅ COPY: Logic tính tổng tiền từ hàm cũ
			decimal total = 0;
			foreach (var kvp in slotToCourt)
			{
				int slotId = kvp.Key;
				int courtId = kvp.Value;

				var slot = slotList[slotId];
				var court = courtDict[courtId];

				total += (decimal)(slot.Discount ?? court.PricePerHour);
			}

			// ✅ COPY: Logic tạo booking từ hàm cũ
			var booking = new Booking
			{
				UserId = user.UserId,
				CreateAt = DateTime.UtcNow,
				StatusId = 7,
				TotalPrice = total,
				IsDayOff = false
			};

			await _bookingRepo.AddBookingAsync(booking);

			// ✅ COPY: Logic tạo chi tiết đặt sân từ hàm cũ
			var details = slotToCourt.Select(pair => new BookingDetail
			{
				BookingId = booking.BookingId,
				CourtId = pair.Value,
				TimeSlotId = pair.Key,
				CheckInDate = request.CheckInDate.Date,
				StatusId = 1,
				CreateAt = DateTime.UtcNow
			}).ToList();

			await _bookingRepo.AddBookingDetailsAsync(details);

			// ✅ COPY: Logic response từ hàm cũ
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
					slots = details.Select(d => new
					{
						timeSlotId = d.TimeSlotId,
						startTime = slotList[d.TimeSlotId].StartTime,
						endTime = slotList[d.TimeSlotId].EndTime,
						courtId = d.CourtId,
						courtName = courtDict[d.CourtId].CourtName
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
            // Lấy thông tin sân và các slot không khả dụng
            var availability = await _bookingRepo.GetCourtAvailabilityAsync(
                request.FacilityId,
                request.CategoryId,
                request.CheckInDate,
                request.TimeSlotIds
            );

            // Lấy thời gian bắt đầu tương ứng với mỗi TimeSlotId
            var slotTimes = await _bookingRepo.GetSlotStartTimesByIdsAsync(request.TimeSlotIds);

            // Sắp xếp lại TimeSlotIds theo StartTime
            request.TimeSlotIds = request.TimeSlotIds
                .OrderBy(id => slotTimes[id])
                .ToList();

            // Tạo ma trận trạng thái: hàng = sân, cột = slot
            var courtCount = availability.Count;
            var slotCount = request.TimeSlotIds.Count;

            // Ánh xạ slotId => index
            var slotIdToIndex = request.TimeSlotIds
                .Select((slotId, idx) => new { slotId, idx })
                .ToDictionary(x => x.slotId, x => x.idx);

            int[,] matrix = new int[courtCount, slotCount]; // 0 = trống, -1 = đã đặt

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

            // Kết quả ánh xạ slot -> courtId
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
            if (queryParams.Page <= 0 || queryParams.PageSize <= 0)
            {
                return new()
                {
                    Success = false,
                    Status = 400,
                    Message = "Page và PageSize phải lớn hơn 0."
                };
            }

            var validSortBy = new[] { "checkindate", "createdate" };
            if (!validSortBy.Contains(queryParams.SortBy?.ToLower()))
            {
                return new()
                {
                    Success = false,
                    Status = 400,
                    Message = $"SortBy không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validSortBy)}"
                };
            }

            var dir = queryParams.SortDirection?.ToLower();
            if (dir != "asc" && dir != "desc")
            {
                return new()
                {
                    Success = false,
                    Status = 400,
                    Message = "SortDirection không hợp lệ. Chỉ chấp nhận: asc hoặc desc."
                };
            }

            // Get count
            var totalItems = await _bookingRepo.CountByUserIdAsync(userId, queryParams.StatusId);
            var totalPages = (int)Math.Ceiling(totalItems / (double)queryParams.PageSize);

            if (totalPages > 0 && queryParams.Page > totalPages)
            {
                return new()
                {
                    Success = false,
                    Status = 400,
                    Message = $"Page vượt quá số trang tối đa ({totalPages})."
                };
            }

            // Get booking list
            var bookings = await _bookingRepo.GetByUserIdAsync(userId, queryParams);

            // 🔁 Lấy Court và Slot qua repository
            var courtDict = await _bookingRepo.GetCourtsWithCategoryAsync();
            var slotDict = await _bookingRepo.GetTimeSlotsAsync();

            var dtoList = bookings.Select(b => new BookingResponseDto
            {
                UserId = b.UserId,
                BookingId = b.BookingId,
                CreateDate = b.CreateAt,
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
                }).ToList(),

                Ratings = b.Ratings?.Select(r => new RatingDto
                {
                    RatingId = r.RatingId,
                    Stars = r.Stars ?? 0,
                    Comment = r.Comment,
                    BookingId = r.BookingId ?? 0
                }).ToList() ?? new List<RatingDto>()
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
            var booking = await _bookingRepo.GetBookingWithDetailsByIdAsync(bookingId);

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
            var user = await _accRepo.GetByIdAsync(booking.UserId.Value);
            var dto = new BookingResponseDto
            {
                UserId = booking.UserId,
                Phone = user.Phone,
                Email = user.Email,
                BookingId = booking.BookingId,
                TotalPrice = booking.TotalPrice ?? 0,
                CreateDate =booking.CreateAt,
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
            var booking = await _bookingRepo.GetBookingWithDetailsAsync(bookingId);

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

            var allowedStatusToComplete = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

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

            var success = await _bookingRepo.SaveAsync();

            if (!success)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Status = 500,
                    Message = "Đã xảy ra lỗi khi lưu thay đổi."
                };
            }

            // Gửi SignalR message
            try
            {
                await _hubContext.Clients.All.SendAsync("BookingStatusChanged", new
                {
                    BookingId = bookingId
                });

                Console.WriteLine($"[SignalR] Message sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error sending message: {ex.Message}");
            }

            return new ApiResponse<string>
            {
                Success = true,
                Status = 200,
                Message = "Đã đánh dấu booking là hoàn thành thành công."
            };
        }

        public async Task<ApiResponse<string>> MarkBookingCancelledAsync(int bookingId)
        {
            var booking = await _bookingRepo.GetBookingWithDetailsAsync(bookingId);

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

           

            var allowedStatusToComplete = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            if (!allowedStatusToComplete.Contains(booking.StatusId))
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Status = 400,
                    Message = "Trạng thái hiện tại không cho phép cancel booking."
                };
            }

            booking.StatusId = 10;

            foreach (var detail in booking.BookingDetails)
            {
                detail.StatusId = 10;
            }

            var success = await _bookingRepo.SaveAsync();

            if (!success)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Status = 500,
                    Message = "Đã xảy ra lỗi khi lưu thay đổi."
                };
            }

            // Gửi SignalR message
            try
            {
                await _hubContext.Clients.All.SendAsync("BookingStatusChanged", new
                {
                    BookingId = bookingId
                });

                Console.WriteLine($"[SignalR] Message sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error sending message: {ex.Message}");
            }

            return new ApiResponse<string>
            {
                Success = true,
                Status = 200,
                Message = "Đã đánh dấu cancel booking thành công."
            };
        }
        public async Task<ApiResponse<string>> MarkBookingPaidAsync(int bookingId)
        {
            var booking = await _bookingRepo.GetBookingWithDetailsAsync(bookingId);

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

            

            var allowedStatusToComplete = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            if (!allowedStatusToComplete.Contains(booking.StatusId))
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Status = 400,
                    Message = "Trạng thái hiện tại không cho phép hoàn thành thanh toan."
                };
            }

            booking.StatusId = 7;

            foreach (var detail in booking.BookingDetails)
            {
                detail.StatusId = 7;
            }

            var success = await _bookingRepo.SaveAsync();

            if (!success)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Status = 500,
                    Message = "Đã xảy ra lỗi khi lưu thay đổi."
                };
            }
            
            // Gửi SignalR message
            try
            {
                await _hubContext.Clients.All.SendAsync("BookingStatusChanged", new
                {
                    BookingId = bookingId
                });

                Console.WriteLine($"[SignalR] Message sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Error sending message: {ex.Message}");
            }
            return new ApiResponse<string>
            {
                Success = true,
                Status = 200,
                Message = "Đã đánh dấu booking là paid thành công."
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

       

    }
}
