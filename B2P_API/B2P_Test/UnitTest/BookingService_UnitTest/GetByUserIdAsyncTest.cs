using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using B2P_API.Models;
using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Services;
using B2P_API.DTOs.BookingDTOs;

namespace B2P_Test.UnitTest.BookingService_UnitTest
{
    public class GetByUserIdAsyncTest
    {
        private readonly Mock<IBookingRepository> _bookingRepoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;
        private readonly Mock<IAccountRepository> _accRepo2Mock;
        private readonly BookingService _service;

        public GetByUserIdAsyncTest()
        {
            _bookingRepoMock = new Mock<IBookingRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
            _accRepo2Mock = new Mock<IAccountRepository>();
            _service = new BookingService(_bookingRepoMock.Object, _accRepoMock.Object, _accRepo2Mock.Object);
        }

        [Fact(DisplayName = "GetByUserIdAsync - Page và PageSize <= 0")]
        public async Task GetByUserIdAsync_InvalidPaging_Returns400()
        {
            var queryParams = new BookingQueryParameters { Page = 0, PageSize = 0, SortBy = "checkindate", SortDirection = "asc" };

            var result = await _service.GetByUserIdAsync(1, queryParams);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Page và PageSize phải lớn hơn 0.", result.Message);
        }

        [Fact(DisplayName = "GetByUserIdAsync - SortBy không hợp lệ")]
        public async Task GetByUserIdAsync_InvalidSortBy_Returns400()
        {
            var queryParams = new BookingQueryParameters { Page = 1, PageSize = 10, SortBy = "invalid", SortDirection = "asc" };

            var result = await _service.GetByUserIdAsync(1, queryParams);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Contains("SortBy không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "GetByUserIdAsync - SortDirection không hợp lệ")]
        public async Task GetByUserIdAsync_InvalidSortDirection_Returns400()
        {
            var queryParams = new BookingQueryParameters { Page = 1, PageSize = 10, SortBy = "checkindate", SortDirection = "wrongdir" };

            var result = await _service.GetByUserIdAsync(1, queryParams);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Contains("SortDirection không hợp lệ", result.Message);
        }

        [Fact(DisplayName = "GetByUserIdAsync - Page vượt quá số trang tối đa")]
        public async Task GetByUserIdAsync_PageOverTotalPages_Returns400()
        {
            var queryParams = new BookingQueryParameters { Page = 3, PageSize = 10, SortBy = "checkindate", SortDirection = "asc" };
            _bookingRepoMock.Setup(x => x.CountByUserIdAsync(1, null)).ReturnsAsync(15); // 2 pages, page 3 vượt quá

            var result = await _service.GetByUserIdAsync(1, queryParams);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Contains("Page vượt quá số trang tối đa (2)", result.Message);
        }

        [Fact(DisplayName = "GetByUserIdAsync - Lấy danh sách booking thành công")]
        public async Task GetByUserIdAsync_ReturnsBookingListSuccessfully()
        {
            // Arrange
            var userId = 1;
            var queryParams = new BookingQueryParameters { Page = 1, PageSize = 2, SortBy = "checkindate", SortDirection = "desc" };
            _bookingRepoMock.Setup(x => x.CountByUserIdAsync(userId, null)).ReturnsAsync(2);

            // Fake bookings
            var bookings = new List<Booking>
            {
                new Booking
                {
                    BookingId = 10,
                    UserId = userId,
                    TotalPrice = 100,
                    Status = new Status { StatusName = "Đã đặt" },
                    BookingDetails = new List<BookingDetail>
                    {
                        new BookingDetail { CourtId = 1, TimeSlotId = 2, CheckInDate = new DateTime(2025, 8, 1) }
                    }
                },
                new Booking
                {
                    BookingId = 20,
                    UserId = userId,
                    TotalPrice = 200,
                    Status = new Status { StatusName = "Đã xác nhận" },
                    BookingDetails = new List<BookingDetail>
                    {
                        new BookingDetail { CourtId = 2, TimeSlotId = 3, CheckInDate = new DateTime(2025, 8, 2) }
                    }
                }
            };
            _bookingRepoMock.Setup(x => x.GetByUserIdAsync(userId, queryParams)).ReturnsAsync(bookings);

            // Fake court and slot dictionary
            var courtDict = new Dictionary<int, Court>
            {
                { 1, new Court { CourtId = 1, CourtName = "Sân A", Category = new CourtCategory { CategoryName = "VIP" } } },
                { 2, new Court { CourtId = 2, CourtName = "Sân B", Category = new CourtCategory { CategoryName = "Thường" } } }
            };
            var slotDict = new Dictionary<int, TimeSlot>
            {
                { 2, new TimeSlot { TimeSlotId = 2, StartTime = new TimeOnly(8,0), EndTime = new TimeOnly(9,0) } },
                { 3, new TimeSlot { TimeSlotId = 3, StartTime = new TimeOnly(9,0), EndTime = new TimeOnly(10,0) } }
            };
            _bookingRepoMock.Setup(x => x.GetCourtsWithCategoryAsync()).ReturnsAsync(courtDict);
            _bookingRepoMock.Setup(x => x.GetTimeSlotsAsync()).ReturnsAsync(slotDict);

            // Act
            var result = await _service.GetByUserIdAsync(userId, queryParams);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy danh sách booking thành công", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.TotalItems);
            Assert.Equal(1, result.Data.CurrentPage);
            Assert.Equal(2, result.Data.Items.Count());

            var first = result.Data.Items.First();
            Assert.Equal(10, first.BookingId);
            Assert.Equal(1, first.UserId);
            Assert.Equal(100, first.TotalPrice);
            Assert.Equal("Đã đặt", first.Status);
            Assert.Single(first.Slots);
            Assert.Equal("Sân A", first.Slots[0].CourtName);
            Assert.Equal("VIP", first.Slots[0].CategoryName);

            var second = result.Data.Items.Skip(1).First();
            Assert.Equal(20, second.BookingId);
            Assert.Equal(1, second.UserId);
            Assert.Equal(200, second.TotalPrice);
            Assert.Equal("Đã xác nhận", second.Status);
            Assert.Single(second.Slots);
            Assert.Equal("Sân B", second.Slots[0].CourtName);
            Assert.Equal("Thường", second.Slots[0].CategoryName);
        }
    }
}