using B2P_API.DTOs.TimeslotDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using Xunit;

namespace B2P_Test.UnitTest.TimeslotManagementService_UnitTest
{
    public class UpdateTimeSlotTest
    {
        private readonly Mock<ITimeSlotManagementRepository> _timeslotRepoMock;
        private readonly TimeslotManagementService _service;

        public UpdateTimeSlotTest()
        {
            _timeslotRepoMock = new Mock<ITimeSlotManagementRepository>();
            var contextMock = new Mock<SportBookingDbContext>();
            _service = new TimeslotManagementService(contextMock.Object, _timeslotRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Should update timeslot successfully")]
        public async Task UTCID01_ValidUpdate_ReturnsSuccess()
        {
            // Arrange
            int timeslotId = 1;
            var request = new CreateTimeslotRequestDTO
            {
                FacilityId = 1,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(12, 0),
                StatusId = 1,
                Discount = 0.1m
            };

            var existingTimeslot = new TimeSlot
            {
                TimeSlotId = timeslotId,
                FacilityId = 1,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(11, 0),
                StatusId = 2,
                Discount = 0m
            };

            _timeslotRepoMock.Setup(x => x.GetByIdAsync(timeslotId))
                .ReturnsAsync(existingTimeslot);

            _timeslotRepoMock.Setup(x => x.GetByFacilityIdAsync(request.FacilityId))
                .ReturnsAsync(new List<TimeSlot> { existingTimeslot });

            _timeslotRepoMock.Setup(x => x.UpdateAsync(It.IsAny<TimeSlot>()))
                .ReturnsAsync(existingTimeslot);

            // Act
            var result = await _service.UpdateTimeSlot(request, timeslotId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật TimeSlot thành công", result.Message);

            // Verify response data
            Assert.Equal(request.StatusId, result.Data.StatusId);
            Assert.Equal(request.StartTime, result.Data.StartTime);
            Assert.Equal(request.EndTime, result.Data.EndTime);
            Assert.Equal(request.Discount, result.Data.Discount);
        }

        [Fact(DisplayName = "UTCID02 - Should return not found when timeslot doesn't exist")]
        public async Task UTCID02_TimeslotNotFound_ReturnsNotFound()
        {
            // Arrange
            int timeslotId = 999;
            var request = new CreateTimeslotRequestDTO();

            _timeslotRepoMock.Setup(x => x.GetByIdAsync(timeslotId))
                .ReturnsAsync((TimeSlot)null);

            // Act
            var result = await _service.UpdateTimeSlot(request, timeslotId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy TimeSlot cần cập nhật", result.Message);
            Assert.Null(result.Data);
        }

        [Theory(DisplayName = "UTCID03 - Should validate time range")]
        [InlineData("12:00", "10:00", "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc")]
        [InlineData("10:00", "10:00", "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc")]
        public async Task UTCID03_InvalidTimeRange_ReturnsBadRequest(string startTimeStr, string endTimeStr, string expectedMessage)
        {
            // Arrange
            int timeslotId = 1;
            var request = new CreateTimeslotRequestDTO
            {
                StartTime = TimeOnly.Parse(startTimeStr),
                EndTime = TimeOnly.Parse(endTimeStr)
            };

            _timeslotRepoMock.Setup(x => x.GetByIdAsync(timeslotId))
                .ReturnsAsync(new TimeSlot());

            // Act
            var result = await _service.UpdateTimeSlot(request, timeslotId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(expectedMessage, result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Should detect overlapping time slots")]
        public async Task UTCID04_OverlappingTimeslot_ReturnsConflict()
        {
            // Arrange
            int timeslotId = 1;
            var request = new CreateTimeslotRequestDTO
            {
                FacilityId = 1,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(12, 0),
                StatusId = 1
            };

            var existingTimeslot = new TimeSlot
            {
                TimeSlotId = timeslotId,
                FacilityId = 1
            };

            var conflictingSlot = new TimeSlot
            {
                TimeSlotId = 2,
                FacilityId = 1,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(11, 0)
            };

            _timeslotRepoMock.Setup(x => x.GetByIdAsync(timeslotId))
                .ReturnsAsync(existingTimeslot);

            _timeslotRepoMock.Setup(x => x.GetByFacilityIdAsync(request.FacilityId))
                .ReturnsAsync(new List<TimeSlot> { existingTimeslot, conflictingSlot });

            // Act
            var result = await _service.UpdateTimeSlot(request, timeslotId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(409, result.Status);
            Assert.Equal("Khung giờ cập nhật bị trùng với TimeSlot khác", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Should handle update failure")]
        public async Task UTCID05_UpdateFails_ReturnsServerError()
        {
            // Arrange
            int timeslotId = 1;
            var request = new CreateTimeslotRequestDTO
            {
                FacilityId = 1,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(12, 0),
                StatusId = 1
            };

            var existingTimeslot = new TimeSlot
            {
                TimeSlotId = timeslotId,
                FacilityId = 1
            };

            _timeslotRepoMock.Setup(x => x.GetByIdAsync(timeslotId))
                .ReturnsAsync(existingTimeslot);

            _timeslotRepoMock.Setup(x => x.GetByFacilityIdAsync(request.FacilityId))
                .ReturnsAsync(new List<TimeSlot> { existingTimeslot });

            _timeslotRepoMock.Setup(x => x.UpdateAsync(It.IsAny<TimeSlot>()))
                .ReturnsAsync((TimeSlot)null);

            // Act
            var result = await _service.UpdateTimeSlot(request, timeslotId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Cập nhật TimeSlot thất bại", result.Message);
            Assert.Null(result.Data);
        }
    }
}