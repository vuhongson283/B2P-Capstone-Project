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
using Microsoft.AspNetCore.SignalR;
using B2P_API.Hubs;

namespace B2P_Test.UnitTest.BookingService_UnitTest
{
    public class GetByIdAsyncTest
    {
        private readonly Mock<IBookingRepository> _bookingRepoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;
        private readonly Mock<IAccountRepository> _accRepo2Mock;
        private readonly Mock<IHubContext<BookingHub>> _hubContextMock;
        private readonly BookingService _service;

        public GetByIdAsyncTest()
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

        [Fact(DisplayName = "GetByIdAsync - Không tìm thấy booking")]
        public async Task GetByIdAsync_BookingNotFound_Returns404()
        {
            // Arrange
            int bookingId = 1;
            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsByIdAsync(bookingId))
                .ReturnsAsync((Booking)null);

            // Act
            var result = await _service.GetByIdAsync(bookingId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal($"Không tìm thấy booking với ID = {bookingId}", result.Message);
        }

        [Fact(DisplayName = "GetByIdAsync - Lấy thông tin booking thành công")]
        public async Task GetByIdAsync_ReturnsBookingSuccessfully()
        {
            // Arrange
            int bookingId = 1;
            var booking = new Booking
            {
                BookingId = bookingId,
                TotalPrice = 300000,
                Status = new Status { StatusName = "Đã xác nhận" },
                BookingDetails = new List<BookingDetail>
                {
                    new BookingDetail
                    {
                        CourtId = 1,
                        TimeSlotId = 2,
                        CheckInDate = new DateTime(2025, 8, 1),
                        Court = new Court
                        {
                            CourtId = 1,
                            CourtName = "Sân A",
                            Category = new CourtCategory { CategoryName = "VIP" }
                        },
                        TimeSlot = new TimeSlot
                        {
                            TimeSlotId = 2,
                            StartTime = new TimeOnly(8, 0),
                            EndTime = new TimeOnly(9, 0)
                        }
                    },
                    new BookingDetail
                    {
                        CourtId = 1,
                        TimeSlotId = 3,
                        CheckInDate = new DateTime(2025, 8, 1),
                        Court = new Court
                        {
                            CourtId = 1,
                            CourtName = "Sân A",
                            Category = new CourtCategory { CategoryName = "VIP" }
                        },
                        TimeSlot = new TimeSlot
                        {
                            TimeSlotId = 3,
                            StartTime = new TimeOnly(9, 0),
                            EndTime = new TimeOnly(10, 0)
                        }
                    }
                }
            };

            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsByIdAsync(bookingId))
                .ReturnsAsync(booking);

            // Act
            var result = await _service.GetByIdAsync(bookingId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy chi tiết booking thành công", result.Message);
            Assert.NotNull(result.Data);

            var dto = result.Data;
            Assert.Equal(bookingId, dto.BookingId);
            Assert.Equal(300000, dto.TotalPrice);
            Assert.Equal(new DateTime(2025, 8, 1), dto.CheckInDate);
            Assert.Equal("Đã xác nhận", dto.Status);
            Assert.Equal(2, dto.Slots.Count);

            var firstSlot = dto.Slots[0];
            Assert.Equal(1, firstSlot.CourtId);
            Assert.Equal(2, firstSlot.TimeSlotId);
            Assert.Equal(TimeSpan.FromHours(8), firstSlot.StartTime);
            Assert.Equal(TimeSpan.FromHours(9), firstSlot.EndTime);
            Assert.Equal("Sân A", firstSlot.CourtName);
            Assert.Equal("VIP", firstSlot.CategoryName);

            var secondSlot = dto.Slots[1];
            Assert.Equal(1, secondSlot.CourtId);
            Assert.Equal(3, secondSlot.TimeSlotId);
            Assert.Equal(TimeSpan.FromHours(9), secondSlot.StartTime);
            Assert.Equal(TimeSpan.FromHours(10), secondSlot.EndTime);
            Assert.Equal("Sân A", secondSlot.CourtName);
            Assert.Equal("VIP", secondSlot.CategoryName);
        }

        [Fact(DisplayName = "GetByIdAsync - Xử lý khi Status null (chỉ kiểm tra các field an toàn)")]
        public async Task GetByIdAsync_WhenStatusIsNull_ReturnsValidResponse()
        {
            // Arrange
            int bookingId = 1;
            var booking = new Booking
            {
                BookingId = bookingId,
                TotalPrice = 300000,
                Status = null,
                BookingDetails = new List<BookingDetail>
        {
            new BookingDetail
            {
                CourtId = 1,
                TimeSlotId = 2,
                CheckInDate = new DateTime(2025, 8, 1),
                Court = new Court {
                    CourtId = 1,
                    CourtName = "Sân A",
                    Category = new CourtCategory { CategoryName = "VIP" }
                },
                TimeSlot = new TimeSlot {
                    TimeSlotId = 2,
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(9, 0)
                }
            }
        }
            };

            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsByIdAsync(bookingId))
                .ReturnsAsync(booking);

            // Act
            var result = await _service.GetByIdAsync(bookingId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy chi tiết booking thành công", result.Message);

            // Kiểm tra các field không liên quan đến Status
            var dto = result.Data;
            Assert.Equal(bookingId, dto.BookingId);
            Assert.Equal(300000, dto.TotalPrice);
            Assert.Equal(new DateTime(2025, 8, 1), dto.CheckInDate);

            // Kiểm tra slots
            Assert.Single(dto.Slots);
            var slot = dto.Slots[0];
            Assert.Equal(1, slot.CourtId);
            Assert.Equal(2, slot.TimeSlotId);
            Assert.Equal("Sân A", slot.CourtName);
            Assert.Equal("VIP", slot.CategoryName);
            Assert.Equal(TimeSpan.FromHours(8), slot.StartTime);
            Assert.Equal(TimeSpan.FromHours(9), slot.EndTime);
        }

        [Fact(DisplayName = "GetByIdAsync - Xử lý khi không có BookingDetails")]
        public async Task GetByIdAsync_WhenNoBookingDetails_ThrowsException()
        {
            // Arrange
            int bookingId = 1;
            var booking = new Booking
            {
                BookingId = bookingId,
                TotalPrice = 300000,
                Status = new Status { StatusName = "Đã xác nhận" },
                BookingDetails = new List<BookingDetail>() // Không có details
            };

            _bookingRepoMock.Setup(x => x.GetBookingWithDetailsByIdAsync(bookingId))
                .ReturnsAsync(booking);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetByIdAsync(bookingId));
        }
    }
}