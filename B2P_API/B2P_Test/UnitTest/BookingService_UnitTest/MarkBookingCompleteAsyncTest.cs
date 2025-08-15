using B2P_API.Hubs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.BookingService_UnitTest
{
    public class MarkBookingCompleteAsyncTest
    {
        private readonly Mock<IBookingRepository> _bookingRepoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;
        private readonly Mock<IAccountRepository> _accRepo2Mock;
        private readonly IHubContext<BookingHub> _hubContext;
        private readonly BookingService _service;

        public MarkBookingCompleteAsyncTest()
        {
            _bookingRepoMock = new Mock<IBookingRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
            _accRepo2Mock = new Mock<IAccountRepository>();
            _hubContext = Mock.Of<IHubContext<BookingHub>>();
            _service = new BookingService(_bookingRepoMock.Object, _accRepoMock.Object, _hubContext, _accRepo2Mock.Object);
        }

        [Fact(DisplayName = "MarkBookingCompleteAsync - Booking không tồn tại")]
        public async Task MarkBookingCompleteAsync_BookingNotFound_Returns404()
        {
            // Arrange
            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync((Booking)null);

            // Act
            var result = await _service.MarkBookingCompleteAsync(1);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy booking.", result.Message);
        }

        [Fact(DisplayName = "MarkBookingCompleteAsync - Booking đã hoàn thành trước đó")]
        public async Task MarkBookingCompleteAsync_AlreadyCompleted_Returns400()
        {
            var booking = new Booking
            {
                BookingId = 2,
                StatusId = 10,
                BookingDetails = new List<BookingDetail>
                {
                    new BookingDetail { StatusId = 10, CheckInDate = DateTime.Today.AddDays(-2) }
                }
            };
            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsAsync(2)).ReturnsAsync(booking);

            // Act
            var result = await _service.MarkBookingCompleteAsync(2);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Booking đã hoàn thành trước đó.", result.Message);
        }

        [Fact(DisplayName = "MarkBookingCompleteAsync - Chưa tới ngày check-in")]
        public async Task MarkBookingCompleteAsync_NotYetCheckInDate_Returns400()
        {
            var futureDate = DateTime.Today.AddDays(2);
            var booking = new Booking
            {
                BookingId = 3,
                StatusId = 1,
                BookingDetails = new List<BookingDetail>
                {
                    new BookingDetail { StatusId = 1, CheckInDate = futureDate }
                }
            };
            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsAsync(3)).ReturnsAsync(booking);

            // Act
            var result = await _service.MarkBookingCompleteAsync(3);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal($"Không thể hoàn thành booking trước ngày {futureDate:dd/MM/yyyy}.", result.Message);
        }

        [Fact(DisplayName = "MarkBookingCompleteAsync - Trạng thái không cho phép hoàn thành")]
        public async Task MarkBookingCompleteAsync_NotAllowedStatus_Returns400()
        {
            var booking = new Booking
            {
                BookingId = 4,
                StatusId = 99, // Not in allowedStatusToComplete
                BookingDetails = new List<BookingDetail>
                {
                    new BookingDetail { StatusId = 99, CheckInDate = DateTime.Today.AddDays(-1) }
                }
            };
            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsAsync(4)).ReturnsAsync(booking);

            // Act
            var result = await _service.MarkBookingCompleteAsync(4);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Trạng thái hiện tại không cho phép hoàn thành booking.", result.Message);
        }

        [Fact(DisplayName = "MarkBookingCompleteAsync - Lưu thay đổi thất bại")]
        public async Task MarkBookingCompleteAsync_SaveFailed_Returns500()
        {
            var booking = new Booking
            {
                BookingId = 5,
                StatusId = 2,
                BookingDetails = new List<BookingDetail>
                {
                    new BookingDetail { StatusId = 2, CheckInDate = DateTime.Today.AddDays(-1) }
                }
            };
            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsAsync(5)).ReturnsAsync(booking);
            _bookingRepoMock.Setup(x => x.SaveAsync()).ReturnsAsync(false);

            // Act
            var result = await _service.MarkBookingCompleteAsync(5);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Đã xảy ra lỗi khi lưu thay đổi.", result.Message);
        }

        [Fact(DisplayName = "MarkBookingCompleteAsync - Thành công")]
        public async Task MarkBookingCompleteAsync_Success_Returns200()
        {
            var booking = new Booking
            {
                BookingId = 6,
                StatusId = 1,
                BookingDetails = new List<BookingDetail>
                {
                    new BookingDetail { StatusId = 1, CheckInDate = DateTime.Today.AddDays(-1) }
                }
            };
            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsAsync(6)).ReturnsAsync(booking);
            _bookingRepoMock.Setup(x => x.SaveAsync()).ReturnsAsync(true);

            // Act
            var result = await _service.MarkBookingCompleteAsync(6);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Đã đánh dấu booking là hoàn thành thành công.", result.Message);
            Assert.Equal(10, booking.StatusId);
            Assert.All(booking.BookingDetails, d => Assert.Equal(10, d.StatusId));
            _bookingRepoMock.Verify(x => x.SaveAsync(), Times.Once);
        }
    }
}