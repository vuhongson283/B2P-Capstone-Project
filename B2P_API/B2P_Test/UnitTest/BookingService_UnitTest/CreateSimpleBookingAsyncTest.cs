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
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.BookingService_UnitTest
{
    public class CreateSimpleBookingAsyncTest
    {
        private readonly Mock<IBookingRepository> _bookingRepoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;
        private readonly Mock<IAccountRepository> _accRepo2Mock;
        private readonly Mock<IHubContext<B2P_API.Hubs.BookingHub>> _hubContextMock;

        public CreateSimpleBookingAsyncTest()
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
            var request = new SimpleBookingDto { UserId = 123, TimeSlotId = 1, CourtId = 10, CheckInDate = DateTime.Today };
            _accRepoMock.Setup(x => x.GetByIdAsync(123)).ReturnsAsync((User)null);

            var result = await service.CreateSimpleBookingAsync(request);

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
            var request = new SimpleBookingDto { Email = email, Phone = phone, TimeSlotId = 1, CourtId = 10, CheckInDate = DateTime.Today };

            var result = await service.CreateSimpleBookingAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Khách đặt sân phải cung cấp email và số điện thoại", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - Email is invalid returns 400")]
        public async Task UTCID03_EmailInvalid_Returns400()
        {
            var request = new SimpleBookingDto { Email = "invalid@", Phone = "0123456789", TimeSlotId = 1, CourtId = 10, CheckInDate = DateTime.Today };

            var serviceMock = new Mock<BookingService>(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            )
            { CallBase = true };
            serviceMock.Setup(x => x.IsRealEmailAsync(It.IsAny<string>())).ReturnsAsync(false);

            var result = await serviceMock.Object.CreateSimpleBookingAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - Phone is invalid returns 400")]
        public async Task UTCID04_PhoneInvalid_Returns400()
        {
            var request = new SimpleBookingDto { Email = "user@email.com", Phone = "123", TimeSlotId = 1, CourtId = 10, CheckInDate = DateTime.Today };

            var serviceMock = new Mock<BookingService>(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            )
            { CallBase = true };
            serviceMock.Setup(x => x.IsRealEmailAsync(It.IsAny<string>())).ReturnsAsync(true);

            var result = await serviceMock.Object.CreateSimpleBookingAsync(request);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số điện thoại không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "UTCID05 - Success returns 200")]
        public async Task UTCID05_Success_Returns200()
        {
            var request = new SimpleBookingDto
            {
                UserId = 1,
                Email = "user@email.com",
                Phone = "0123456789",
                TimeSlotId = 11,
                CourtId = 101,
                CheckInDate = DateTime.Today
            };
            var user = new User { UserId = 1, Email = request.Email, Phone = request.Phone };
            _accRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(user);

            var slotList = new Dictionary<int, TimeSlot>
            {
                { 11, new TimeSlot { TimeSlotId = 11, StartTime = new TimeOnly(8,0), EndTime = new TimeOnly(9,0), Discount = 100000 } }
            };

            var courtDict = new Dictionary<int, Court>
            {
                { 101, new Court { CourtId = 101, CourtName = "Sân A", PricePerHour = 200000 } }
            };

            _bookingRepoMock.Setup(x => x.GetTimeSlotsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(slotList);

            _bookingRepoMock.Setup(x => x.GetCourtsByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(courtDict);

            // Tạo booking và booking detail sẽ không throw
            _bookingRepoMock.Setup(x => x.AddBookingAsync(It.IsAny<Booking>()))
                .Callback<Booking>(b => b.BookingId = 555) // Giả lập BookingId sinh ra
                .Returns(Task.CompletedTask);
            _bookingRepoMock.Setup(x => x.AddBookingDetailsAsync(It.IsAny<List<BookingDetail>>()))
                .Returns(Task.CompletedTask);

            var service = new BookingService(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            );

            // Act
            var result = await service.CreateSimpleBookingAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Đặt sân thành công", result.Message);
            Assert.NotNull(result.Data);

            dynamic data = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(result.Data));
            Assert.Equal(555, (int)data.bookingId);
            Assert.Equal(request.CheckInDate.Date, ((DateTime)data.checkInDate).Date);
            Assert.Equal(request.UserId, (int)data.user.userId);
            Assert.Equal(request.Email, (string)data.user.email);
            Assert.Equal(request.Phone, (string)data.user.phone);
            Assert.Single(data.slots);
            Assert.Equal(request.TimeSlotId, (int)data.slots[0].timeSlotId);
            Assert.Equal(request.CourtId, (int)data.slots[0].courtId);
            Assert.Equal("Sân A", (string)data.slots[0].courtName);
        }
    }
}