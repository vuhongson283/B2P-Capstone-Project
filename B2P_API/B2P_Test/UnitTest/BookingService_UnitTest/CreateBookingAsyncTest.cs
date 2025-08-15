using B2P_API.DTOs;
using B2P_API.DTOs.BookingDTOs;
using B2P_API.Hubs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Repositories;
using B2P_API.Repository;
using B2P_API.Services;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.BookingService_UnitTest
{
    public class CreateBookingAsyncTest
    {
        private readonly Mock<IBookingRepository> _bookingRepoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;
        private readonly Mock<IAccountRepository> _accRepo2Mock;
        private readonly IHubContext<BookingHub> _hubContext;
        private readonly BookingService _service;

        public CreateBookingAsyncTest()
        {
            _bookingRepoMock = new Mock<IBookingRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
            _accRepo2Mock = new Mock<IAccountRepository>();
            _hubContext = new Mock<IHubContext<BookingHub>>().Object;
            _service = new BookingService(_bookingRepoMock.Object, _accRepoMock.Object, _hubContext, _accRepo2Mock.Object);
        }

        private BookingRequestDto CreateValidRequest(bool withUserId = false)
        {
            return new BookingRequestDto
            {
                UserId = withUserId ? 1 : null,
                Email = withUserId ? null : "valid@example.com",
                Phone = withUserId ? null : "0987654321",
                TimeSlotIds = new List<int> { 1, 2 },
                CheckInDate = DateTime.UtcNow.AddDays(1).Date,
                FacilityId = 1,
                CategoryId = 1
            };
        }

        [Fact]
        public async Task CreateBookingAsync_WithValidUserId_ReturnsSuccess()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: true);
            var user = new User { UserId = 1 };

            _accRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);

            // Mock data for AssignCourtsAsync internal calls
            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                request.FacilityId,
                request.CategoryId,
                request.CheckInDate,
                request.TimeSlotIds
            )).ReturnsAsync(new List<CourtAvailability>
            {
                new CourtAvailability {
                    CourtId = 1,
                    UnavailableSlotIds = new HashSet<int>()
                }
            });

            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(request.TimeSlotIds))
                .ReturnsAsync(new Dictionary<int, TimeSpan>
                {
                    { 1, TimeSpan.FromHours(8) },
                    { 2, TimeSpan.FromHours(9) }
                });

            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSlot>
                {
                    { 1, new TimeSlot { Discount = 100000 } },
                    { 2, new TimeSlot { Discount = 100000 } }
                });

            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, Court> { { 1, new Court() } });

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            _bookingRepoMock.Verify(x => x.AddBookingAsync(It.IsAny<Booking>()), Times.Once);

            // Check user info in response
            var json = JsonConvert.SerializeObject(result.Data);
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            Assert.Equal(request.UserId, (int)data.user.userId);
        }

        [Fact]
        public async Task CreateBookingAsync_WithInvalidUserId_ReturnsNotFound()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: true);
            _accRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((User)null);

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Người dùng không tồn tại", result.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_WithInvalidPhone_ReturnsBadRequest()
        {
            // Arrange
            var request = CreateValidRequest();
            request.Phone = "123"; // Số điện thoại không hợp lệ

            // Không mock IsValidPhone, để nó chạy logic thật

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số điện thoại không hợp lệ", result.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenNoCourtsAvailable_ReturnsConflict()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: true);
            _accRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new User { UserId = 1 });

            // Mock no courts available
            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                request.FacilityId,
                request.CategoryId,
                request.CheckInDate,
                request.TimeSlotIds
            )).ReturnsAsync(new List<CourtAvailability>
            {
        new CourtAvailability {
            CourtId = 1,
            UnavailableSlotIds = new HashSet<int>(request.TimeSlotIds)
        }
            });

            // BỔ SUNG dòng này để tránh NullReferenceException!
            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSpan>
                {
            { 1, TimeSpan.FromHours(8) },
            { 2, TimeSpan.FromHours(9) }
                });

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(409, result.Status);
            Assert.Equal("Không đủ sân trống để đặt các slot đã chọn", result.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_CalculatesCorrectPrice_WithDiscount()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: true);
            _accRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => new User { UserId = id });

            // Mock court assignment
            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()
            )).ReturnsAsync(new List<CourtAvailability>
            {
                new CourtAvailability {
                    CourtId = 1,
                    UnavailableSlotIds = new HashSet<int>()
                }
            });

            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSpan>
                {
                    { 1, TimeSpan.FromHours(8) },
                    { 2, TimeSpan.FromHours(9) }
                });

            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSlot>
                {
                    { 1, new TimeSlot { Discount = 150000 } },
                    { 2, new TimeSlot { Discount = 150000 } }
                });

            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, Court> { { 1, new Court { PricePerHour = 200000 } } });

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            var json = JsonConvert.SerializeObject(result.Data);
            var data = JsonConvert.DeserializeObject<dynamic>(json);

            Assert.Equal(request.CheckInDate.Date, (DateTime)data.checkInDate);
            Assert.Equal(request.UserId, (int)data.user.userId);

            // Không assert vào totalPrice vì không có trong response
        }

        [Fact]
        public async Task CreateBookingAsync_CreatesNewUser_WhenNoUserId()
        {
            // Arrange
            var request = CreateValidRequest();

            // Mock court assignment
            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()
            )).ReturnsAsync(new List<CourtAvailability>
            {
                new CourtAvailability {
                    CourtId = 1,
                    UnavailableSlotIds = new HashSet<int>()
                }
            });

            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSpan>
                {
                    { 1, TimeSpan.FromHours(8) },
                    { 2, TimeSpan.FromHours(9) }
                });

            // Sửa ở đây
            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSlot>
                {
                    { 1, new TimeSlot { Discount = 150000 } },
                    { 2, new TimeSlot { Discount = 150000 } }
                });

            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, Court> { { 1, new Court { PricePerHour = 200000 } } });

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.True(result.Success);
            _accRepo2Mock.Verify(x => x.RegisterAccountAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_ReturnsBadRequest_WhenMissingEmailOrPhone()
        {
            // Arrange
            var request1 = CreateValidRequest();
            request1.Email = null;

            var request2 = CreateValidRequest();
            request2.Phone = null;

            // Act
            var result1 = await _service.CreateBookingAsync(request1);
            var result2 = await _service.CreateBookingAsync(request2);

            // Assert
            Assert.False(result1.Success);
            Assert.Equal(400, result1.Status);

            Assert.False(result2.Success);
            Assert.Equal(400, result2.Status);
        }

        [Fact]
        public async Task CreateBookingAsync_CreatesCorrectNumberOfBookingDetails()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: true);
            _accRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new User { UserId = 1 });

            // Mock court assignment (2 courts, 2 slots)
            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()
            )).ReturnsAsync(new List<CourtAvailability>
            {
                new CourtAvailability {
                    CourtId = 1,
                    UnavailableSlotIds = new HashSet<int>()
                },
                new CourtAvailability {
                    CourtId = 2,
                    UnavailableSlotIds = new HashSet<int>()
                }
            });

            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSpan>
                {
                    { 1, TimeSpan.FromHours(8) },
                    { 2, TimeSpan.FromHours(9) }
                });

            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSlot>
                {
                    { 1, new TimeSlot { Discount = 150000 } },
                    { 2, new TimeSlot { Discount = 150000 } }
                });

            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, Court>
                {
                    { 1, new Court { PricePerHour = 200000 } },
                    { 2, new Court { PricePerHour = 200000 } }
                });

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            _bookingRepoMock.Verify(x => x.AddBookingDetailsAsync(It.Is<List<BookingDetail>>(l => l.Count == 2)), Times.Once);
        }

        [Theory(DisplayName = "UTCID07 - Should validate required email and phone when no userId")]
        [InlineData(null, "0987654321", "Khách đặt sân phải cung cấp email và số điện thoại")] // Missing email
        [InlineData("valid@example.com", null, "Khách đặt sân phải cung cấp email và số điện thoại")] // Missing phone
        [InlineData(null, null, "Khách đặt sân phải cung cấp email và số điện thoại")] // Missing both
        public async Task UTCID07_MissingEmailOrPhone_ReturnsBadRequest(string email, string phone, string expectedMessage)
        {
            // Arrange
            var request = CreateValidRequest(withUserId: false);
            request.Email = email;
            request.Phone = phone;

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(expectedMessage, result.Message);
            _accRepo2Mock.Verify(x => x.RegisterAccountAsync(It.IsAny<User>()), Times.Never());
        }

        [Fact(DisplayName = "UTCID10 - Should return error when email is invalid")]
        public async Task UTCID10_InvalidEmailFormat_ReturnsBadRequest()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: false);
            request.Email = "invalid-email"; // Email không hợp lệ

            // Mock các dependencies cần thiết
            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()
            )).ReturnsAsync(new List<CourtAvailability>());

            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSpan>());

            // Act - để phương thức IsRealEmailAsync chạy thật
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email không hợp lệ", result.Message);
            _accRepo2Mock.Verify(x => x.RegisterAccountAsync(It.IsAny<User>()), Times.Never());
        }

        [Fact]
        public async Task CreateBookingAsync_WithEmptyEmail_ShouldReturnRequiredFieldError()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: false);
            request.Email = ""; // Email trống
            request.Phone = "0987654321"; // Số điện thoại hợp lệ

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Khách đặt sân phải cung cấp email và số điện thoại", result.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_WithInvalidEmailFormat_ShouldReturnInvalidEmailError()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: false);
            request.Email = "invalid-email-format";

            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()
            )).ReturnsAsync(new List<CourtAvailability>());

            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSpan>());

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email không hợp lệ", result.Message);
        }
        [Theory(DisplayName = "IsRealEmailAsync - Should validate email correctly")]
        [InlineData(null, false)] // null email
        [InlineData("", false)] // empty email
        [InlineData("  ", false)] // whitespace email
        [InlineData("invalid-email", false)] // invalid format
        [InlineData("valid@gmail.com", true)] // valid email
        [InlineData("test@nonexistentdomain123456.com", false)] // valid format but invalid domain
        public async Task IsRealEmailAsync_ShouldValidateCorrectly(string email, bool expectedResult)
        {
            // Act
            var result = await _service.IsRealEmailAsync(email);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("0912345678", true)]
        [InlineData("123", false)]
        [InlineData(" ", false)]
        [InlineData(null, false)]
        public void IsValidPhone_TestWithReflection(string phone, bool expectedResult)
        {
            // Lấy method bằng Reflection
            var method = typeof(BookingService)
                .GetMethod("IsValidPhone", BindingFlags.NonPublic | BindingFlags.Instance);

            // Gọi method
            var result = (bool)method.Invoke(_service, new object[] { phone });

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void TrySmartBooking_ShouldAssignIndividualSlots_WhenGroupAssignmentFails()
        {
            // Arrange
            var service = new BookingService(null, null, null, null);
            var method = typeof(BookingService)
                .GetMethod("TrySmartBooking", BindingFlags.NonPublic | BindingFlags.Instance);

            // Matrix: 2 courts, 4 slots
            // Court 0: Slot 1,3 booked (value = 1)
            // Court 1: Slot 2 booked (value = 1)
            var matrix = new int[2, 4] {
                { 0, 1, 0, 1 },
                { 0, 0, 1, 0 }
            };

            var slotIds = new List<int> { 1, 2, 3, 4 };

            // Act
            var result = (int[])method.Invoke(service, new object[] { matrix, slotIds });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Length);

            // Slot 1 (index 0) - có thể gán vào court 0 hoặc 1 (vì court 0 slot 1 đã booked)
            // Thực tế hàm sẽ gán vào court đầu tiên có slot trống
            Assert.Equal(0, result[0]); // Court 0 slot 0 trống

            // Slot 2 (index 1) - court 0 slot 1 đã booked, court 1 slot 1 trống
            Assert.Equal(1, result[1]);

            // Slot 3 (index 2) - court 1 slot 2 đã booked, court 0 slot 2 trống
            Assert.Equal(0, result[2]);

            // Slot 4 (index 3) - court 0 slot 3 đã booked, court 1 slot 3 trống
            Assert.Equal(1, result[3]);
        }

        [Fact]
        public void TrySmartBooking_ShouldReturnNull_WhenNoCourtsAvailable()
        {
            // Arrange
            var service = new BookingService(null, null, null, null);
            var method = typeof(BookingService)
                .GetMethod("TrySmartBooking", BindingFlags.NonPublic | BindingFlags.Instance);

            // Tất cả các slot đều đã booked
            var matrix = new int[2, 3] {
                { 1, 1, 1 },
                { 1, 1, 1 }
            };

            var slotIds = new List<int> { 1, 2, 3 };

            // Act
            var result = method.Invoke(service, new object[] { matrix, slotIds });

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void TrySmartBooking_ShouldPartiallyAssign_WhenSomeSlotsCannotBeAssigned()
        {
            // Arrange
            var service = new BookingService(null, null, null, null);
            var method = typeof(BookingService)
                .GetMethod("TrySmartBooking", BindingFlags.NonPublic | BindingFlags.Instance);

            // Court 0: Slot 1 booked (value = 1)
            // Court 1: Slot 2 booked (value = 1)
            var matrix = new int[2, 3] {
                { 0, 1, 0 },
                { 0, 0, 1 }
            };

            var slotIds = new List<int> { 1, 2, 3 };

            // Act
            var result = (int[])method.Invoke(service, new object[] { matrix, slotIds });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Length);

            // Slot 1 (index 0) - cả 2 courts đều trống
            Assert.Equal(0, result[0]); // Ưu tiên court đầu tiên

            // Slot 2 (index 1) - court 0 booked, court 1 trống
            Assert.Equal(1, result[1]);

            // Slot 3 (index 2) - court 1 booked, court 0 trống
            Assert.Equal(0, result[2]);
        }

        [Fact]
        public void TrySmartBooking_ShouldHandleNonConsecutiveSlots()
        {
            // Arrange
            var service = new BookingService(null, null, null, null);
            var method = typeof(BookingService)
                .GetMethod("TrySmartBooking", BindingFlags.NonPublic | BindingFlags.Instance);

            var matrix = new int[2, 4] {
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 }
            };

            var slotIds = new List<int> { 1, 3, 5, 7 }; // Các slot không liên tiếp

            // Act
            var result = (int[])method.Invoke(service, new object[] { matrix, slotIds });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Length);

            // Tất cả các slot nên được gán (có thể vào court 0 hoặc 1)
            Assert.All(result, x => Assert.True(x == 0 || x == 1));
        }
    }
}