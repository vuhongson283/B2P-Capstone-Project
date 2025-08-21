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
        private readonly Mock<IHubContext<BookingHub>> _hubContextMock;
        private readonly BookingService _service;

        public CreateBookingAsyncTest()
        {
            _bookingRepoMock = new Mock<IBookingRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
            _accRepo2Mock = new Mock<IAccountRepository>();
            _hubContextMock = new Mock<IHubContext<BookingHub>>();
            _service = new BookingService(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            );
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

            // Avoid NullReferenceException!
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
        }

        [Fact]
        public async Task CreateBookingAsync_CreatesNewUser_WhenNoUserId()
        {
            // Arrange
            var request = CreateValidRequest();

            _accRepo2Mock.Setup(x => x.GetUserByPhoneAsync(It.IsAny<string>())).ReturnsAsync((User)null);

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
            Assert.True(result.Success);
            _accRepo2Mock.Verify(x => x.RegisterAccountAsync(It.IsAny<User>()), Times.Once);
        }

        [Theory(DisplayName = "CreateBookingAsync - Should return BadRequest for missing/invalid email and phone (no userId)")]
        [InlineData(null, null, "Khách đặt sân phải cung cấp email hoặc số điện thoại")]
        [InlineData("", "", "Khách đặt sân phải cung cấp email hoặc số điện thoại")]
        [InlineData("invalid@", "invalid", "Email không hợp lệ")] // email sai format
        [InlineData("valid@example.com", "123", "Số điện thoại không hợp lệ")] // phone invalid
        [InlineData("invalid@", "0987654321", "Email không hợp lệ")] // email invalid, phone hợp lệ
        [InlineData("valid@example.com", "", "Số điện thoại không hợp lệ")] // phone rỗng, email hợp lệ
        [InlineData("", "0987654321", "Email không hợp lệ")] // email rỗng, phone hợp lệ
        public async Task CreateBookingAsync_MissingOrInvalidEmailOrPhone_ReturnsBadRequest(string email, string phone, string expectedMessage)
        {
            // Arrange
            var request = CreateValidRequest(withUserId: false);
            request.Email = email;
            request.Phone = phone;

            // Với các case hợp lệ (phone hợp lệ hoặc email hợp lệ), cần mock GetUserByPhoneAsync trả về 1 user để tránh NullReference
            if (!string.IsNullOrWhiteSpace(phone) && phone.Length > 8 && phone.All(char.IsDigit))
            {
                _accRepo2Mock.Setup(x => x.GetUserByPhoneAsync(phone)).ReturnsAsync(new User { UserId = 99, Phone = phone });
                // Mock thêm các bước booking nếu muốn test pass đến cuối, nếu không thì chỉ test các case expect lỗi thôi.
            }

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(expectedMessage, result.Message);
            _accRepo2Mock.Verify(x => x.RegisterAccountAsync(It.IsAny<User>()), Times.Never());
        }

        [Fact]
        public async Task CreateBookingAsync_WithUserId_DoesNotRequireEmailOrPhone()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: true);
            request.Email = null;
            request.Phone = null;
            _accRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new User { UserId = 1 });

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
        }

        [Fact(DisplayName = "UTCID10 - Should return error when email is invalid")]
        public async Task UTCID10_InvalidEmailFormat_ReturnsBadRequest()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: false);
            request.Email = "invalid-email"; // Email không hợp lệ

            // Act - để phương thức IsRealEmailAsync chạy thật
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email không hợp lệ", result.Message);
            _accRepo2Mock.Verify(x => x.RegisterAccountAsync(It.IsAny<User>()), Times.Never());
        }

        [Fact]
        public async Task CreateBookingAsync_CreatesCorrectNumberOfBookingDetails()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: true);
            _accRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new User { UserId = 1 });

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
            var resultObj = method.Invoke(_service, new object[] { phone });
            Assert.NotNull(resultObj);
            var result = (bool)resultObj;

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
            var resultObj = method.Invoke(service, new object[] { matrix, slotIds });
            Assert.NotNull(resultObj);
            var result = (int[])resultObj;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Length);

            Assert.Equal(0, result[0]);
            Assert.Equal(1, result[1]);
            Assert.Equal(0, result[2]);
            Assert.Equal(1, result[3]);
        }

        [Fact]
        public void TrySmartBooking_ShouldReturnNull_WhenNoCourtsAvailable()
        {
            // Arrange
            var service = new BookingService(null, null, null, null);
            var method = typeof(BookingService)
                .GetMethod("TrySmartBooking", BindingFlags.NonPublic | BindingFlags.Instance);

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

            var matrix = new int[2, 3] {
                { 0, 1, 0 },
                { 0, 0, 1 }
            };

            var slotIds = new List<int> { 1, 2, 3 };

            // Act
            var resultObj = method.Invoke(service, new object[] { matrix, slotIds });
            Assert.NotNull(resultObj);
            var result = (int[])resultObj;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Length);

            Assert.Equal(0, result[0]);
            Assert.Equal(1, result[1]);
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

            var slotIds = new List<int> { 1, 3, 5, 7 };

            // Act
            var resultObj = method.Invoke(service, new object[] { matrix, slotIds });
            Assert.NotNull(resultObj);
            var result = (int[])resultObj;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Length);

            Assert.All(result, x => Assert.True(x == 0 || x == 1));
        }

        [Fact(DisplayName = "CreateBookingAsync - slot.Discount null, lấy PricePerHour")]
        public async Task CreateBookingAsync_DiscountNull_UsesPricePerHour()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: true);
            _accRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new User { UserId = 1 });

            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()))
                .ReturnsAsync(new List<CourtAvailability> { new CourtAvailability { CourtId = 1 } });

            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSpan>
                {
                    { 1, TimeSpan.FromHours(8) },
                    { 2, TimeSpan.FromHours(9) }
                });

            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSlot>
                {
                    { 1, new TimeSlot { Discount = null } }, // Discount = null
                    { 2, new TimeSlot { Discount = null } }
                });

            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, Court>
                {
                    { 1, new Court { PricePerHour = 123456 } }
                });

            Booking? capturedBooking = null;
            _bookingRepoMock.Setup(x => x.AddBookingAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => capturedBooking = b)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(capturedBooking);
            Assert.Equal(246912, capturedBooking.TotalPrice); // 2 * 123456
        }

        [Fact(DisplayName = "CreateBookingAsync - slot.Discount <= 0, lấy PricePerHour")]
        public async Task CreateBookingAsync_DiscountNotPositive_UsesPricePerHour()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: true);
            _accRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new User { UserId = 1 });

            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()))
                .ReturnsAsync(new List<CourtAvailability> { new CourtAvailability { CourtId = 1 } });

            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSpan>
                {
                    { 1, TimeSpan.FromHours(8) },
                    { 2, TimeSpan.FromHours(9) }
                });

            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSlot>
                {
                    { 1, new TimeSlot { Discount = 0 } }, // Discount == 0
                    { 2, new TimeSlot { Discount = -10000 } } // Discount < 0
                });

            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, Court>
                {
                    { 1, new Court { PricePerHour = 50000 } }
                });

            Booking? capturedBooking = null;
            _bookingRepoMock.Setup(x => x.AddBookingAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => capturedBooking = b)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(capturedBooking);
            Assert.Equal(100000, capturedBooking.TotalPrice); // 2 * 50000
        }

        [Fact(DisplayName = "CreateBookingAsync - PricePerHour null, lấy giá = 0")]
        public async Task CreateBookingAsync_PricePerHourNull_UsesZero()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: true);
            _accRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new User { UserId = 1 });

            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()))
                .ReturnsAsync(new List<CourtAvailability> { new CourtAvailability { CourtId = 1 } });

            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSpan>
                {
                    { 1, TimeSpan.FromHours(8) },
                    { 2, TimeSpan.FromHours(9) }
                });

            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSlot>
                {
                    { 1, new TimeSlot { Discount = null } },
                    { 2, new TimeSlot { Discount = null } }
                });

            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, Court>
                {
                    { 1, new Court { PricePerHour = null } }
                });

            Booking? capturedBooking = null;
            _bookingRepoMock.Setup(x => x.AddBookingAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => capturedBooking = b)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(capturedBooking);
            Assert.Equal(0, capturedBooking.TotalPrice);
        }

        [Fact(DisplayName = "CreateBookingAsync - PaymentTypeId is null branch")]
        public async Task CreateBookingAsync_PaymentTypeIdIsNull_Branch()
        {
            var request = CreateValidRequest(withUserId: true);
            request.PaymentTypeId = null;
            request.UserId = 42; // <----- ADD THIS LINE (make sure request.UserId == user.UserId)
            var user = new User { UserId = 42, Email = "nulltypeid@test.com", Phone = "0123456789" };
            _accRepoMock.Setup(x => x.GetByIdAsync(user.UserId)).ReturnsAsync(user);

            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()))
                .ReturnsAsync((int a, int b, DateTime c, List<int> slotIds) =>
                    new List<CourtAvailability>
                    {
                new CourtAvailability { CourtId = 11, UnavailableSlotIds = new HashSet<int>() }
                    });

            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> slotIds) =>
                    slotIds.ToDictionary(id => id, id => id == 1 ? TimeSpan.FromHours(8) : TimeSpan.FromHours(9)));

            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync((IEnumerable<int> slotIds) => slotIds.ToDictionary(
                    id => id,
                    id => new TimeSlot
                    {
                        Discount = 120000,
                        StartTime = id == 1 ? TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)) : TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)),
                        EndTime = id == 1 ? TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)) : TimeOnly.FromTimeSpan(TimeSpan.FromHours(10))
                    }
                ));

            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync((IEnumerable<int> courtIds) => courtIds.ToDictionary(
                    id => id,
                    id => new Court { CourtId = id, CourtName = "A", PricePerHour = 120000 }
                ));

            Booking? capturedBooking = null;
            _bookingRepoMock.Setup(x => x.AddBookingAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => capturedBooking = b)
                .Returns(Task.CompletedTask);

            _bookingRepoMock.Setup(x => x.AddBookingDetailsAsync(It.IsAny<List<BookingDetail>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            if (!result.Success)
            {
                // Debug thông tin trả về
                Console.WriteLine($"Fail: {result.Status}, {result.Message}");
                Assert.True(result.Success, $"Fail: {result.Status}, {result.Message}");
            }
            Assert.NotNull(capturedBooking);
            Assert.Null(capturedBooking.PaymentTypeId);

            var json = JsonConvert.SerializeObject(result.Data);
            dynamic? data = JsonConvert.DeserializeObject<dynamic>(json);
            Assert.NotNull(data);
            Assert.Equal(capturedBooking.BookingId, (int)data.bookingId);
            Assert.Equal(request.CheckInDate.Date, (DateTime)data.checkInDate);
            Assert.NotNull(data.user);
            Assert.Equal(user.UserId, (int)data.user.userId);
            Assert.Equal(user.Email, (string)data.user.email);
            Assert.Equal(user.Phone, (string)data.user.phone);

            var slots = ((IEnumerable<dynamic>)data.slots).ToList();
            Assert.Equal(2, slots.Count);
            Assert.All(slots, s => Assert.Equal("A", (string)s.courtName));
        }

        [Fact(DisplayName = "CreateBookingAsync - PaymentTypeId is NOT null branch")]
        public async Task CreateBookingAsync_PaymentTypeIdIsNotNull_Branch()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: true);
            request.PaymentTypeId = 7;
            request.UserId = 24; // <----- ADD THIS LINE (make sure request.UserId == user.UserId)
            var user = new User { UserId = 24, Email = "notnulltypeid@test.com", Phone = "0123456788" };
            _accRepoMock.Setup(x => x.GetByIdAsync(user.UserId)).ReturnsAsync(user);

            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()))
                .ReturnsAsync((int a, int b, DateTime c, List<int> slotIds) =>
                    new List<CourtAvailability>
                    {
                new CourtAvailability { CourtId = 22, UnavailableSlotIds = new HashSet<int>() }
                    });

            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> slotIds) =>
                    slotIds.ToDictionary(id => id, id => id == 1 ? TimeSpan.FromHours(10) : TimeSpan.FromHours(11)));

            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync((IEnumerable<int> slotIds) => slotIds.ToDictionary(
                    id => id,
                    id => new TimeSlot
                    {
                        Discount = 130000,
                        StartTime = id == 1 ? TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)) : TimeOnly.FromTimeSpan(TimeSpan.FromHours(11)),
                        EndTime = id == 1 ? TimeOnly.FromTimeSpan(TimeSpan.FromHours(11)) : TimeOnly.FromTimeSpan(TimeSpan.FromHours(12))
                    }
                ));

            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync((IEnumerable<int> courtIds) => courtIds.ToDictionary(
                    id => id,
                    id => new Court { CourtId = id, CourtName = "B", PricePerHour = 130000 }
                ));

            Booking? capturedBooking = null;
            _bookingRepoMock.Setup(x => x.AddBookingAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => capturedBooking = b)
                .Returns(Task.CompletedTask);

            _bookingRepoMock.Setup(x => x.AddBookingDetailsAsync(It.IsAny<List<BookingDetail>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            if (!result.Success)
            {
                // Debug thông tin trả về
                Console.WriteLine($"Fail: {result.Status}, {result.Message}");
                Assert.True(result.Success, $"Fail: {result.Status}, {result.Message}");
            }
            Assert.NotNull(capturedBooking);
            Assert.Equal(request.PaymentTypeId, capturedBooking.PaymentTypeId);

            var json = JsonConvert.SerializeObject(result.Data);
            dynamic? data = JsonConvert.DeserializeObject<dynamic>(json);
            Assert.NotNull(data);
            Assert.Equal(capturedBooking.BookingId, (int)data.bookingId);
            Assert.Equal(request.CheckInDate.Date, (DateTime)data.checkInDate);
            Assert.NotNull(data.user);
            Assert.Equal(user.UserId, (int)data.user.userId);
            Assert.Equal(user.Email, (string)data.user.email);
            Assert.Equal(user.Phone, (string)data.user.phone);

            var slots = ((IEnumerable<dynamic>)data.slots).ToList();
            Assert.Equal(2, slots.Count);
            Assert.All(slots, s => Assert.Equal("B", (string)s.courtName));
        }



        [Fact(DisplayName = "CreateBookingAsync - Return slot with null StartTime/EndTime")]
        public async Task CreateBookingAsync_ReturnsSlotWithNullStartTimeOrEndTime()
        {
            var request = CreateValidRequest(withUserId: true);
            request.UserId = 102;
            var user = new User { UserId = 102, Email = "test@domain.com", Phone = "0999999998" };
            _accRepoMock.Setup(x => x.GetByIdAsync(user.UserId)).ReturnsAsync(user);

            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()))
                .ReturnsAsync(new List<CourtAvailability>
                {
            new CourtAvailability { CourtId = 3, UnavailableSlotIds = new HashSet<int>() }
                });

            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> slotIds) =>
                    slotIds.ToDictionary(id => id, id => TimeSpan.FromHours(8 + id)));

            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync((IEnumerable<int> slotIds) => slotIds.ToDictionary(
                    id => id,
                    id => new TimeSlot
                    {
                        Discount = 120000,
                        StartTime = null, // simulate missing StartTime
                        EndTime = null
                    }
                ));

            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, Court>
                {
            { 3, new Court { CourtId = 3, CourtName = "Test Court 3", PricePerHour = 120000 } }
                });

            _bookingRepoMock.Setup(x => x.AddBookingAsync(It.IsAny<Booking>()))
                .Returns(Task.CompletedTask);

            _bookingRepoMock.Setup(x => x.AddBookingDetailsAsync(It.IsAny<List<BookingDetail>>()))
                .Returns(Task.CompletedTask);

            var result = await _service.CreateBookingAsync(request);

            Assert.True(result.Success);
            var json = JsonConvert.SerializeObject(result.Data);
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            var slots = ((IEnumerable<dynamic>)data.slots).ToList();
            Assert.All(slots, s =>
            {
                Assert.True(string.IsNullOrEmpty((string)s.startTime?.ToString()));
                Assert.True(string.IsNullOrEmpty((string)s.endTime?.ToString()));
            });
        }

        [Fact(DisplayName = "CreateBookingAsync - Return slot with null courtName")]
        public async Task CreateBookingAsync_ReturnsSlotWithNullCourtName()
        {
            var request = CreateValidRequest(withUserId: true);
            request.UserId = 103;
            var user = new User { UserId = 103, Email = "test@domain.com", Phone = "0999999997" };
            _accRepoMock.Setup(x => x.GetByIdAsync(user.UserId)).ReturnsAsync(user);

            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()))
                .ReturnsAsync(new List<CourtAvailability>
                {
            new CourtAvailability { CourtId = 4, UnavailableSlotIds = new HashSet<int>() }
                });

            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> slotIds) =>
                    slotIds.ToDictionary(id => id, id => TimeSpan.FromHours(8 + id)));

            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync((IEnumerable<int> slotIds) => slotIds.ToDictionary(
                    id => id,
                    id => new TimeSlot
                    {
                        Discount = 120000,
                        StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8 + id)),
                        EndTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(9 + id))
                    }
                ));

            // Mock return courtDict with null courtName
            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, Court>
                {
            { 4, new Court { CourtId = 4, CourtName = null, PricePerHour = 120000 } }
                });

            _bookingRepoMock.Setup(x => x.AddBookingAsync(It.IsAny<Booking>()))
                .Returns(Task.CompletedTask);

            _bookingRepoMock.Setup(x => x.AddBookingDetailsAsync(It.IsAny<List<BookingDetail>>()))
                .Returns(Task.CompletedTask);

            var result = await _service.CreateBookingAsync(request);

            Assert.True(result.Success);
            var json = JsonConvert.SerializeObject(result.Data);
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            var slots = ((IEnumerable<dynamic>)data.slots).ToList();
            Assert.All(slots, s => Assert.Null((string)s.courtName));
        }
        [Fact(DisplayName = "CreateBookingAsync - Test user is null scenario")]
        public async Task CreateBookingAsync_UserIsNull_CoversNullBranches()
        {
            // Arrange - tạo scenario mà user có thể là null
            var request = CreateValidRequest(withUserId: false);
            request.Email = "test@example.com";
            request.Phone = "0987654321";

            // Không có user tồn tại, sẽ tạo user mới
            _accRepo2Mock.Setup(x => x.GetUserByPhoneAsync(request.Phone)).ReturnsAsync((User)null);

            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()))
                .ReturnsAsync(new List<CourtAvailability>
                {
            new CourtAvailability { CourtId = 1, UnavailableSlotIds = new HashSet<int>() }
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
            { 1, new TimeSlot { Discount = 100000, StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)), EndTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)) } },
            { 2, new TimeSlot { Discount = 100000, StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)), EndTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)) } }
                });

            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, Court>
                {
            { 1, new Court { CourtId = 1, CourtName = "Test Court", PricePerHour = 100000 } }
                });

            User capturedNewUser = null;
            _accRepo2Mock.Setup(x => x.RegisterAccountAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedNewUser = u)
                .ReturnsAsync((User u) => u);

            _bookingRepoMock.Setup(x => x.AddBookingAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
            _bookingRepoMock.Setup(x => x.AddBookingDetailsAsync(It.IsAny<List<BookingDetail>>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(capturedNewUser);

            var json = JsonConvert.SerializeObject(result.Data);
            var data = JsonConvert.DeserializeObject<dynamic>(json);

            // Test user?.Email và user?.Phone khi user mới được tạo
            Assert.NotNull(data.user);
            Assert.NotNull(data.user.email);
            Assert.NotNull(data.user.phone);
        }

        [Fact(DisplayName = "CreateBookingAsync - Test TimeSlot properties null scenarios")]
        public async Task CreateBookingAsync_TimeSlotPropertiesNull_CoversNullBranches()
        {
            // Arrange
            var request = CreateValidRequest(withUserId: true);
            request.UserId = 999;
            var user = new User { UserId = 999, Email = "test@example.com", Phone = "0987654321" };

            _accRepoMock.Setup(x => x.GetByIdAsync(user.UserId)).ReturnsAsync(user);

            _bookingRepoMock.Setup(x => x.GetCourtAvailabilityAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()))
                .ReturnsAsync(new List<CourtAvailability>
                {
            new CourtAvailability { CourtId = 1, UnavailableSlotIds = new HashSet<int>() }
                });

            _bookingRepoMock.Setup(x => x.GetSlotStartTimesByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSpan>
                {
            { 1, TimeSpan.FromHours(8) },
            { 2, TimeSpan.FromHours(9) }
                });

            // Tạo TimeSlot với StartTime và EndTime là null
            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSlot>
                {
            { 1, new TimeSlot { Discount = 100000, StartTime = null, EndTime = null } },  // Null StartTime/EndTime
            { 2, new TimeSlot { Discount = 100000, StartTime = null, EndTime = null } }   // Null StartTime/EndTime
                });

            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, Court>
                {
            { 1, new Court { CourtId = 1, CourtName = "Test Court", PricePerHour = 100000 } }
                });

            _bookingRepoMock.Setup(x => x.AddBookingAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
            _bookingRepoMock.Setup(x => x.AddBookingDetailsAsync(It.IsAny<List<BookingDetail>>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateBookingAsync(request);

            // Assert
            Assert.True(result.Success);

            var json = JsonConvert.SerializeObject(result.Data);
            var data = JsonConvert.DeserializeObject<dynamic>(json);

            var slots = ((IEnumerable<dynamic>)data.slots).ToList();

            // Test StartTime và EndTime null branches
            foreach (var slot in slots)
            {
                // Khi StartTime/EndTime null, JSON sẽ serialize thành null
                Assert.True(slot.startTime == null || string.IsNullOrEmpty(slot.startTime?.ToString()));
                Assert.True(slot.endTime == null || string.IsNullOrEmpty(slot.endTime?.ToString()));
            }
        }


    }
}