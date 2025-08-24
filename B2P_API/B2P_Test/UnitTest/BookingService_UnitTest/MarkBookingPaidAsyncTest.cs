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

            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsAsync(123)).ReturnsAsync((Booking?)null);

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
            Assert.Equal("Trạng thái hiện tại không cho phép hoàn thành thanh toán.", result.Message);
        }
    }
}