using B2P_API.DTOs.TimeslotDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using Xunit;

namespace B2P_Test.UnitTest.TimeslotManagementService_UnitTest
{
    public class CreateNewTimeSlotTest
    {
        private readonly Mock<ITimeSlotManagementRepository> _timeslotRepoMock;
        private readonly TimeslotManagementService _service;

        public CreateNewTimeSlotTest()
        {
            _timeslotRepoMock = new Mock<ITimeSlotManagementRepository>();
            var contextMock = new Mock<SportBookingDbContext>();
            _service = new TimeslotManagementService(contextMock.Object, _timeslotRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Should create timeslot successfully with valid data")]
        public async Task UTCID01_ValidData_ReturnsSuccess()
        {
            // Arrange
            var validRequest = new CreateTimeslotRequestDTO
            {
                FacilityId = 1,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(12, 0),
                StatusId = 1,
                Discount = 0.1m
            };

            var mockTimeslot = new TimeSlot
            {
                TimeSlotId = 1,
                FacilityId = validRequest.FacilityId,
                StartTime = validRequest.StartTime,
                EndTime = validRequest.EndTime,
                StatusId = validRequest.StatusId,
                Discount = validRequest.Discount
            };

            _timeslotRepoMock.Setup(x => x.GetByFacilityIdAsync(validRequest.FacilityId))
                .ReturnsAsync(new List<TimeSlot>());

            _timeslotRepoMock.Setup(x => x.CreateAsync(It.IsAny<TimeSlot>()))
                .ReturnsAsync(mockTimeslot);

            // Act
            var result = await _service.CreateNewTimeSlot(validRequest);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Tạo TimeSlot thành công", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(mockTimeslot.TimeSlotId, result.Data.TimeSlotId);
            Assert.Equal(validRequest.FacilityId, result.Data.FacilityId);
            Assert.Equal(validRequest.StartTime, result.Data.StartTime);
            Assert.Equal(validRequest.EndTime, result.Data.EndTime);
            Assert.Equal(validRequest.StatusId, result.Data.StatusId);
            Assert.Equal(validRequest.Discount, result.Data.Discount);
        }

        [Theory(DisplayName = "UTCID02 - Should validate time slot constraints")]
        [InlineData("12:00", "10:00", "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc")]
        [InlineData("10:00", "10:00", "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc")]
        public async Task UTCID02_InvalidTimeRange_ReturnsValidationError(string startTimeStr, string endTimeStr, string expectedMessage)
        {
            // Arrange
            var invalidRequest = new CreateTimeslotRequestDTO
            {
                FacilityId = 1,
                StartTime = TimeOnly.Parse(startTimeStr),
                EndTime = TimeOnly.Parse(endTimeStr),
                StatusId = 1,
                Discount = 0.1m
            };

            // Act
            var result = await _service.CreateNewTimeSlot(invalidRequest);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal(expectedMessage, result.Message);
            Assert.Null(result.Data);
            _timeslotRepoMock.Verify(x => x.CreateAsync(It.IsAny<TimeSlot>()), Times.Never());
        }

        [Fact(DisplayName = "UTCID03 - Should detect overlapping time slots")]
        public async Task UTCID03_OverlappingTimeslot_ReturnsConflict()
        {
            // Arrange
            var request = new CreateTimeslotRequestDTO
            {
                FacilityId = 1,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(12, 0),
                StatusId = 1,
                Discount = 0.1m
            };

            var existingSlots = new List<TimeSlot>
            {
                new TimeSlot
                {
                    TimeSlotId = 2,
                    FacilityId = 1,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(11, 0),
                    StatusId = 1,
                    Discount = 0m
                }
            };

            _timeslotRepoMock.Setup(x => x.GetByFacilityIdAsync(request.FacilityId))
                .ReturnsAsync(existingSlots);

            // Act
            var result = await _service.CreateNewTimeSlot(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(409, result.Status);
            Assert.Equal("Khung giờ bị trùng với TimeSlot đã tồn tại", result.Message);
            Assert.Null(result.Data);
            _timeslotRepoMock.Verify(x => x.CreateAsync(It.IsAny<TimeSlot>()), Times.Never());
        }

        [Fact(DisplayName = "UTCID04 - Should use returned timeslot from repository")]
        public async Task UTCID05_UsesRepositoryReturnedTimeslot()
        {
            // Arrange
            var request = new CreateTimeslotRequestDTO
            {
                FacilityId = 1,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(12, 0),
                StatusId = 1,
                Discount = 0.1m
            };

            var returnedTimeslot = new TimeSlot
            {
                TimeSlotId = 5,
                FacilityId = request.FacilityId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                StatusId = request.StatusId,
                Discount = request.Discount
            };

            _timeslotRepoMock.Setup(x => x.GetByFacilityIdAsync(request.FacilityId))
                .ReturnsAsync(new List<TimeSlot>());

            _timeslotRepoMock.Setup(x => x.CreateAsync(It.IsAny<TimeSlot>()))
                .ReturnsAsync(returnedTimeslot);

            // Act
            var result = await _service.CreateNewTimeSlot(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(returnedTimeslot.TimeSlotId, result.Data.TimeSlotId);
        }
    }
}