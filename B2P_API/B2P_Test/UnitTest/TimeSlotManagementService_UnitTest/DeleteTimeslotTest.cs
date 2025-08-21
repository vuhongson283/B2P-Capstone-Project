using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using Xunit;

namespace B2P_Test.UnitTest.TimeslotManagementService_UnitTest
{
    public class DeleteTimeSlotTest
    {
        private readonly Mock<ITimeSlotManagementRepository> _timeslotRepoMock;
        private readonly TimeslotManagementService _service;

        public DeleteTimeSlotTest()
        {
            _timeslotRepoMock = new Mock<ITimeSlotManagementRepository>();
            var contextMock = new Mock<SportBookingDbContext>();
            _service = new TimeslotManagementService(contextMock.Object, _timeslotRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Should delete timeslot successfully")]
        public async Task UTCID01_DeleteSuccess_ReturnsSuccessResponse()
        {
            // Arrange
            int timeslotId = 1;
            var mockTimeslot = new TimeSlot { TimeSlotId = timeslotId };

            _timeslotRepoMock.Setup(x => x.GetByIdAsync(timeslotId))
                .ReturnsAsync(mockTimeslot);

            _timeslotRepoMock.Setup(x => x.DeleteAsync(timeslotId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteTimeSlot(timeslotId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Xóa TimeSlot thành công", result.Message);
            Assert.Equal(timeslotId, result.Data.TimeSlotId);
        }

        [Fact(DisplayName = "UTCID02 - Should return not found when timeslot doesn't exist")]
        public async Task UTCID02_TimeslotNotFound_ReturnsNotFound()
        {
            // Arrange
            int timeslotId = 999;

            _timeslotRepoMock.Setup(x => x.GetByIdAsync(timeslotId))
                .ReturnsAsync((TimeSlot)null);

            // Act
            var result = await _service.DeleteTimeSlot(timeslotId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy TimeSlot", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Should return error when deletion fails")]
        public async Task UTCID03_DeletionFails_ReturnsServerError()
        {
            // Arrange
            int timeslotId = 1;
            var mockTimeslot = new TimeSlot { TimeSlotId = timeslotId };

            _timeslotRepoMock.Setup(x => x.GetByIdAsync(timeslotId))
                .ReturnsAsync(mockTimeslot);

            _timeslotRepoMock.Setup(x => x.DeleteAsync(timeslotId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.DeleteTimeSlot(timeslotId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Xóa TimeSlot thất bại", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Should return error response when database exception occurs")]
        public async Task UTCID04_DatabaseError_ReturnsErrorResponse()
        {
            // Arrange
            int timeslotId = 1;
            var mockTimeslot = new TimeSlot { TimeSlotId = timeslotId };

            _timeslotRepoMock.Setup(x => x.GetByIdAsync(timeslotId))
                .ReturnsAsync(mockTimeslot);

            _timeslotRepoMock.Setup(x => x.HasAnyActiveOrFutureBookingsAsync(timeslotId))
                .ReturnsAsync(false);

            _timeslotRepoMock.Setup(x => x.DeleteAsync(timeslotId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.DeleteTimeSlot(timeslotId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("Database error", result.Message);
            Assert.Null(result.Data);
        }
    }
}