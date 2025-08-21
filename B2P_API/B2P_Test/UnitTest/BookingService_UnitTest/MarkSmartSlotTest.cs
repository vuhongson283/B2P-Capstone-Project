using B2P_API.DTOs;
using B2P_API.DTOs.BookingDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.BookingService_UnitTest
{
    public class MarkSmartSlotTest
    {
        private readonly Mock<IBookingRepository> _bookingRepoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;
        private readonly Mock<IAccountRepository> _accRepo2Mock;
        private readonly Mock<IHubContext<B2P_API.Hubs.BookingHub>> _hubContextMock;

        public MarkSmartSlotTest()
        {
            _bookingRepoMock = new Mock<IBookingRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
            _accRepo2Mock = new Mock<IAccountRepository>();
            _hubContextMock = new Mock<IHubContext<B2P_API.Hubs.BookingHub>>();
        }

        [Fact(DisplayName = "UTCID01 - UserId does not exist returns 404")]
        public async Task UTCID01_UserIdNotExists_Returns404()
        {
            var service = new BookingService(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            );
            var request = new BookingRequestDto { UserId = 123 };
            _accRepoMock.Setup(x => x.GetByIdAsync(123)).ReturnsAsync((User)null);

            var result = await service.MarkSmartSlot(request);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Người dùng không tồn tại", result.Message);
        }

        [Theory(DisplayName = "UTCID02 - Missing email or phone returns 400")]
        [InlineData(null, "0123456789")]
        [InlineData("user@email.com", null)]
        [InlineData("", "0123456789")]
        [InlineData("user@email.com", "")]
        public async Task UTCID02_MissingEmailOrPhone_Returns400(string email, string phone)
        {
            var service = new BookingService(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            );
            var request = new BookingRequestDto { Email = email, Phone = phone, UserId = null };

            var result = await service.MarkSmartSlot(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Khách đặt sân phải cung cấp email và số điện thoại", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - Email is invalid returns 400")]
        public async Task UTCID03_EmailInvalid_Returns400()
        {
            var request = new BookingRequestDto { Email = "invalid@", Phone = "0123456789", UserId = null };

            // BookingService với IsRealEmailAsync được override (virtual) để trả về false
            var serviceMock = new Mock<BookingService>(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            )
            { CallBase = true };
            serviceMock.Setup(x => x.IsRealEmailAsync(It.IsAny<string>())).ReturnsAsync(false);

            var result = await serviceMock.Object.MarkSmartSlot(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - Phone is invalid returns 400")]
        public async Task UTCID04_PhoneInvalid_Returns400()
        {
            var request = new BookingRequestDto { Email = "user@email.com", Phone = "123", UserId = null };

            var serviceMock = new Mock<BookingService>(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            )
            { CallBase = true };
            serviceMock.Setup(x => x.IsRealEmailAsync(It.IsAny<string>())).ReturnsAsync(true);

            var result = await serviceMock.Object.MarkSmartSlot(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số điện thoại không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID05 - Not enough courts returns 409")]
        public async Task UTCID05_NotEnoughCourts_Returns409()
        {
            var request = new BookingRequestDto
            {
                UserId = 1,
                Email = "user@email.com",
                Phone = "0123456789",
                TimeSlotIds = new List<int> { 1, 2 },
                CheckInDate = DateTime.Today
            };
            _accRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new User { UserId = 1 });
            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, TimeSlot>());
            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new Dictionary<int, Court>());

            var serviceMock = new Mock<BookingService>(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            )
            { CallBase = true };
            serviceMock.Setup(x => x.AssignCourtsAsync(It.IsAny<BookingRequestDto>()))
                .ReturnsAsync((Dictionary<int, int>)null);

            var result = await serviceMock.Object.MarkSmartSlot(request);

            Assert.False(result.Success);
            Assert.Equal(409, result.Status);
            Assert.Equal("Không đủ sân trống để đặt các slot đã chọn", result.Message);
        }

        [Fact(DisplayName = "UTCID06 - Success returns 200")]
        public async Task UTCID06_Success_Returns200()
        {
            var request = new BookingRequestDto
            {
                UserId = 1,
                Email = "user@email.com",
                Phone = "0123456789",
                TimeSlotIds = new List<int> { 11, 12 },
                CheckInDate = DateTime.Today
            };
            var user = new User { UserId = 1, Email = request.Email, Phone = request.Phone };
            _accRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);

            // Giả lập AssignCourtsAsync trả về mapping
            var slotToCourt = new Dictionary<int, int>
            {
                { 11, 101 },
                { 12, 101 }
            };

            var slotList = new Dictionary<int, TimeSlot>
            {
                { 11, new TimeSlot { TimeSlotId = 11, StartTime = new TimeOnly(8,0), EndTime = new TimeOnly(9,0), Discount = 100000 } },
                { 12, new TimeSlot { TimeSlotId = 12, StartTime = new TimeOnly(9,0), EndTime = new TimeOnly(10,0), Discount = 120000 } }
            };

            var courtDict = new Dictionary<int, Court>
            {
                { 101, new Court { CourtId = 101, CourtName = "Sân A", PricePerHour = 200000 } }
            };

            var serviceMock = new Mock<BookingService>(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            )
            { CallBase = true };

            serviceMock.Setup(x => x.AssignCourtsAsync(It.IsAny<BookingRequestDto>()))
                .ReturnsAsync(slotToCourt);

            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(slotList);

            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(courtDict);

            // Tạo booking và booking detail sẽ không throw
            _bookingRepoMock.Setup(x => x.AddBookingAsync(It.IsAny<Booking>()))
                .Returns(Task.CompletedTask);
            _bookingRepoMock.Setup(x => x.AddBookingDetailsAsync(It.IsAny<List<BookingDetail>>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await serviceMock.Object.MarkSmartSlot(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Đặt sân thành công", result.Message);
            Assert.NotNull(result.Data);

            // Sử dụng JsonConvert để truy cập property động
            dynamic data = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(result.Data));
            Assert.Equal(request.UserId, (int)data.user.userId);
            Assert.Equal(request.Email, (string)data.user.email);
            Assert.Equal(request.Phone, (string)data.user.phone);
            Assert.Equal(request.CheckInDate.Date, ((DateTime)data.checkInDate).Date);
            Assert.Equal(2, data.slots.Count);
        }

        [Fact(DisplayName = "UTCID07 - Create new user and booking succeeds")]
        public async Task UTCID07_CreateNewUser_Success()
        {
            // Arrange
            var request = new BookingRequestDto
            {
                UserId = null, // Không có UserId, sẽ vào nhánh tạo user mới
                Email = "newuser@email.com",
                Phone = "0999888777",
                TimeSlotIds = new List<int> { 21, 22 },
                CheckInDate = DateTime.Today
            };

            // Mock IsRealEmailAsync trả về true
            var serviceMock = new Mock<BookingService>(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            )
            { CallBase = true };
            serviceMock.Setup(x => x.IsRealEmailAsync(It.IsAny<string>())).ReturnsAsync(true);

            // Không cần mock _accRepoMock.GetByIdAsync vì không có UserId

            // Đảm bảo phone hợp lệ (IsValidPhone)
            // Không cần mock, nếu IsValidPhone là private static logic đơn giản (hoặc dùng reflection nếu muốn test riêng)

            // Mock RegisterAccountAsync: giả lập gán UserId cho user mới
            _accRepo2Mock.Setup(x => x.RegisterAccountAsync(It.IsAny<User>()))
                .Callback<User>(u => u.UserId = 55)
                .ReturnsAsync((User u) => u);

            // Mock AssignCourtsAsync trả về slotToCourt
            var slotToCourt = new Dictionary<int, int> { { 21, 201 }, { 22, 201 } };
            serviceMock.Setup(x => x.AssignCourtsAsync(It.IsAny<BookingRequestDto>())).ReturnsAsync(slotToCourt);

            // Mock slotList
            var slotList = new Dictionary<int, TimeSlot>
    {
        { 21, new TimeSlot { TimeSlotId = 21, StartTime = new TimeOnly(10,0), EndTime = new TimeOnly(11,0), Discount = 111000 } },
        { 22, new TimeSlot { TimeSlotId = 22, StartTime = new TimeOnly(11,0), EndTime = new TimeOnly(12,0), Discount = 222000 } }
    };
            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(slotList);

            // Mock courtDict
            var courtDict = new Dictionary<int, Court>
    {
        { 201, new Court { CourtId = 201, CourtName = "Sân B", PricePerHour = 200000 } }
    };
            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(courtDict);

            _bookingRepoMock.Setup(x => x.AddBookingAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
            _bookingRepoMock.Setup(x => x.AddBookingDetailsAsync(It.IsAny<List<BookingDetail>>())).Returns(Task.CompletedTask);

            // Act
            var result = await serviceMock.Object.MarkSmartSlot(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Đặt sân thành công", result.Message);

            // Kiểm tra RegisterAccountAsync đã được gọi
            _accRepo2Mock.Verify(x => x.RegisterAccountAsync(It.Is<User>(u =>
                u.Email == request.Email && u.Phone == request.Phone)), Times.Once);

            // Kiểm tra thông tin trả về là user mới
            dynamic data = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(result.Data));
            Assert.Equal(55, (int)data.user.userId); // UserId gán trong callback trên
            Assert.Equal(request.Email, (string)data.user.email);
            Assert.Equal(request.Phone, (string)data.user.phone);
            Assert.Equal(request.CheckInDate.Date, ((DateTime)data.checkInDate).Date);
            Assert.Equal(2, data.slots.Count);
        }
    }
}