using B2P_API.Models;
using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Services;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.BookingService_UnitTest
{
    public class MarkBookingPaidAsyncTest
    {
        private readonly Mock<IBookingRepository> _bookingRepoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;
        private readonly Mock<IAccountRepository> _accRepo2Mock;
        private readonly Mock<IHubContext<B2P_API.Hubs.BookingHub>> _hubContextMock;

        public MarkBookingPaidAsyncTest()
        {
            _bookingRepoMock = new Mock<IBookingRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
            _accRepo2Mock = new Mock<IAccountRepository>();
            _hubContextMock = new Mock<IHubContext<B2P_API.Hubs.BookingHub>>();
        }

        [Fact(DisplayName = "UTCID01 - Booking not found returns 404")]
        public async Task UTCID01_BookingNotFound_Returns404()
        {
            var service = new BookingService(
                _bookingRepoMock.Object, _accRepoMock.Object, _hubContextMock.Object, _accRepo2Mock.Object);

            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsAsync(123)).ReturnsAsync((Booking)null);

            var result = await service.MarkBookingPaidAsync(123, "ABC");

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy booking.", result.Message);
        }

        [Fact(DisplayName = "UTCID02 - Booking already completed returns 400")]
        public async Task UTCID02_BookingAlreadyCompleted_Returns400()
        {
            var booking = new Booking
            {
                BookingId = 1,
                StatusId = 10,
                BookingDetails = new List<BookingDetail>
                {
                    new BookingDetail { CheckInDate = DateTime.Today }
                }
            };
            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsAsync(1)).ReturnsAsync(booking);

            var service = new BookingService(
                _bookingRepoMock.Object, _accRepoMock.Object, _hubContextMock.Object, _accRepo2Mock.Object);

            var result = await service.MarkBookingPaidAsync(1, "XYZ");

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Booking đã hoàn thành trước đó.", result.Message);
        }

        [Fact(DisplayName = "UTCID03 - Status not allow paid returns 400")]
        public async Task UTCID03_StatusNotAllowPaid_Returns400()
        {
            var booking = new Booking
            {
                BookingId = 2,
                StatusId = 99, // not in allowed
                BookingDetails = new List<BookingDetail>
                {
                    new BookingDetail { CheckInDate = DateTime.Today }
                }
            };
            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsAsync(2)).ReturnsAsync(booking);

            var service = new BookingService(
                _bookingRepoMock.Object, _accRepoMock.Object, _hubContextMock.Object, _accRepo2Mock.Object);

            var result = await service.MarkBookingPaidAsync(2, null);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Trạng thái hiện tại không cho phép hoàn thành thanh toan.", result.Message);
        }

        [Fact(DisplayName = "UTCID04 - SaveAsync fails returns 500")]
        public async Task UTCID04_SaveAsyncFails_Returns500()
        {
            var booking = new Booking
            {
                BookingId = 3,
                StatusId = 1,
                BookingDetails = new List<BookingDetail>
                {
                    new BookingDetail { CheckInDate = DateTime.Today, StatusId = 1 }
                }
            };
            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsAsync(3)).ReturnsAsync(booking);
            _bookingRepoMock.Setup(x => x.SaveAsync()).ReturnsAsync(false);

            var service = new BookingService(
                _bookingRepoMock.Object, _accRepoMock.Object, _hubContextMock.Object, _accRepo2Mock.Object);

            var result = await service.MarkBookingPaidAsync(3, "TTT123");

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Đã xảy ra lỗi khi lưu thay đổi.", result.Message);

            // booking.StatusId and all BookingDetails should be 7, TransactionCode set
            Assert.Equal(7, booking.StatusId);
            Assert.Equal("TTT123", booking.TransactionCode);
            Assert.All(booking.BookingDetails, d => Assert.Equal(7, d.StatusId));
        }

        [Fact(DisplayName = "UTCID05 - Success returns 200")]
        public async Task UTCID05_Success_Returns200()
        {
            var booking = new Booking
            {
                BookingId = 4,
                StatusId = 2,
                BookingDetails = new List<BookingDetail>
                {
                    new BookingDetail { CheckInDate = DateTime.Today, StatusId = 2 }
                }
            };
            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsAsync(4)).ReturnsAsync(booking);
            _bookingRepoMock.Setup(x => x.SaveAsync()).ReturnsAsync(true);

            // Setup SignalR Clients.All to return mock (no SendAsync setup needed)
            var clientProxyMock = new Mock<IClientProxy>();
            _hubContextMock.Setup(x => x.Clients.All).Returns(clientProxyMock.Object);

            var service = new BookingService(
                _bookingRepoMock.Object, _accRepoMock.Object, _hubContextMock.Object, _accRepo2Mock.Object);

            var result = await service.MarkBookingPaidAsync(4, "TRANS-CODE-001");

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Đã đánh dấu booking là paid thành công.", result.Message);

            Assert.Equal(7, booking.StatusId);
            Assert.Equal("TRANS-CODE-001", booking.TransactionCode);
            Assert.All(booking.BookingDetails, d => Assert.Equal(7, d.StatusId));
        }
    }
}