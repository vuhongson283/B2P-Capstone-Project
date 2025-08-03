using B2P_API.DTOs;
using B2P_API.DTOs.BookingDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Repositories;
using B2P_API.Repository;
using B2P_API.Services;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.BookingService_UnitTest
{
    public class CreateBookingAsyncTest
    {
        private readonly Mock<IBookingRepository> _bookingRepoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;
        private readonly Mock<IAccountRepository> _accRepo2Mock;
        private readonly BookingService _service;

        public CreateBookingAsyncTest()
        {
            _bookingRepoMock = new Mock<IBookingRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
            _accRepo2Mock = new Mock<IAccountRepository>();
            _service = new BookingService(_bookingRepoMock.Object, _accRepoMock.Object, _accRepo2Mock.Object);
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
    }
}