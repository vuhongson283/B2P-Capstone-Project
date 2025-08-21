using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Repository;
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
    public class GetTimeSlotAvailabilityAsyncTest
    {
        private readonly Mock<IBookingRepository> _bookingRepoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;
        private readonly Mock<IAccountRepository> _accRepo2Mock;
        private readonly Mock<IHubContext<B2P_API.Hubs.BookingHub>> _hubContextMock;

        public GetTimeSlotAvailabilityAsyncTest()
        {
            _bookingRepoMock = new Mock<IBookingRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
            _accRepo2Mock = new Mock<IAccountRepository>();
            _hubContextMock = new Mock<IHubContext<B2P_API.Hubs.BookingHub>>();
        }

        [Fact(DisplayName = "UTCID01 - Return available slots successfully")]
        public async Task UTCID01_ReturnAvailableSlotsSuccessfully()
        {
            // Arrange
            int facilityId = 10;
            int categoryId = 2;
            DateTime checkInDate = new DateTime(2025, 8, 20);

            var expectedList = new List<TimeSlotAvailability>
            {
                new TimeSlotAvailability { TimeSlotId = 1, AvailableCourtCount = 3 },
                new TimeSlotAvailability { TimeSlotId = 2, AvailableCourtCount = 1 }
            };

            _bookingRepoMock.Setup(x => x.GetAvailableCourtCountPerSlotAsync(
                facilityId, categoryId, checkInDate))
                .ReturnsAsync(expectedList);

            var service = new BookingService(
                _bookingRepoMock.Object,
                _accRepoMock.Object,
                _hubContextMock.Object,
                _accRepo2Mock.Object
            );

            // Act
            var result = await service.GetTimeSlotAvailabilityAsync(facilityId, categoryId, checkInDate);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy danh sách slot trống thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(expectedList.Count, result.Data.Count);
            for (int i = 0; i < expectedList.Count; i++)
            {
                Assert.Equal(expectedList[i].TimeSlotId, result.Data[i].TimeSlotId);
                Assert.Equal(expectedList[i].AvailableCourtCount, result.Data[i].AvailableCourtCount);
            }
        }
    }
}