using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using Xunit;

namespace B2P_Test.UnitTest.TimeslotManagementService_UnitTest
{
    public class GetTimeslotByFacilityIdAsyncTest
    {
        private readonly Mock<ITimeSlotManagementRepository> _timeslotRepoMock;
        private readonly TimeslotManagementService _service;

        public GetTimeslotByFacilityIdAsyncTest()
        {
            _timeslotRepoMock = new Mock<ITimeSlotManagementRepository>();
            var contextMock = new Mock<SportBookingDbContext>();
            _service = new TimeslotManagementService(contextMock.Object, _timeslotRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Should return timeslots with valid facility ID")]
        public async Task UTCID01_ValidFacilityId_ReturnsTimeslots()
        {
            // Arrange
            int facilityId = 1;
            var mockTimeslots = new List<TimeSlot>
            {
                new TimeSlot
                {
                    TimeSlotId = 1,
                    FacilityId = facilityId,
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(12, 0),
                    StatusId = 1,
                    Discount = 0.1m,
                    Status = new Status { StatusId = 1, StatusName = "Active" }
                }
            };

            _timeslotRepoMock.Setup(x => x.GetByFacilityIdAsync(facilityId))
                .ReturnsAsync(mockTimeslots);

            // Act
            var result = await _service.GetTimeslotByFacilityIdAsync(facilityId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy TimeSlot thành công", result.Message);
            Assert.Single(result.Data.Items);
            Assert.Equal("10:00:00", result.Data.Items.ToList()[0].StartTime);
        }

        [Fact(DisplayName = "UTCID02 - Should validate invalid facility ID")]
        public async Task UTCID02_InvalidFacilityId_ReturnsBadRequest()
        {
            // Arrange
            int invalidFacilityId = 0;

            // Act
            var result = await _service.GetTimeslotByFacilityIdAsync(invalidFacilityId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Facility ID phải lớn hơn 0", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Should validate invalid pagination parameters")]
        public async Task UTCID03_InvalidPagination_ReturnsBadRequest()
        {
            // Arrange
            int facilityId = 1;
            int invalidPageNumber = 0;
            int invalidPageSize = -1;

            // Act
            var result = await _service.GetTimeslotByFacilityIdAsync(facilityId, null, invalidPageNumber, invalidPageSize);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số trang và kích thước trang phải lớn hơn 0", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Should filter by status ID")]
        public async Task UTCID04_FilterByStatus_ReturnsFilteredResults()
        {
            // Arrange
            int facilityId = 1;
            int statusId = 2;
            var mockTimeslots = new List<TimeSlot>
            {
                new TimeSlot
                {
                    TimeSlotId = 1,
                    FacilityId = facilityId,
                    StatusId = 1,
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(12, 0),
                    Status = new Status { StatusId = 1, StatusName = "Available" }
                },
                new TimeSlot
                {
                    TimeSlotId = 2,
                    FacilityId = facilityId,
                    StatusId = 2,
                    StartTime = new TimeOnly(14, 0),
                    EndTime = new TimeOnly(16, 0),
                    Status = new Status { StatusId = 2, StatusName = "Booked" }
                }
            };

            _timeslotRepoMock.Setup(x => x.GetByFacilityIdAsync(facilityId))
                .ReturnsAsync(mockTimeslots);

            // Act
            var result = await _service.GetTimeslotByFacilityIdAsync(facilityId, statusId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy TimeSlot thành công", result.Message);

            var itemsList = result.Data.Items.ToList();
            Assert.Single(itemsList);

            var filteredItem = itemsList.First();
            Assert.Equal(2, filteredItem.TimeSlotId);
            Assert.Equal(statusId, filteredItem.StatusId);
            Assert.Equal("Booked", filteredItem.Status.StatusName);
            Assert.Equal("14:00:00", filteredItem.StartTime);
            Assert.Equal("16:00:00", filteredItem.EndTime);
        }

        [Fact(DisplayName = "UTCID05 - Should return empty result for no timeslots")]
        public async Task UTCID05_NoTimeslots_ReturnsEmptyResult()
        {
            // Arrange
            int facilityId = 1;
            _timeslotRepoMock.Setup(x => x.GetByFacilityIdAsync(facilityId))
                .ReturnsAsync(new List<TimeSlot>());

            // Act
            var result = await _service.GetTimeslotByFacilityIdAsync(facilityId);

            // Assert
            Assert.True(result.Success);
            Assert.Empty(result.Data.Items);
            Assert.Equal("Không có TimeSlot nào cho facility này", result.Message);
        }

        [Fact(DisplayName = "UTCID06 - Should handle pagination correctly")]
        public async Task UTCID06_Pagination_ReturnsCorrectPage()
        {
            // Arrange
            int facilityId = 1;
            var mockTimeslots = Enumerable.Range(1, 15)
                .Select(i => new TimeSlot
                {
                    TimeSlotId = i,
                    FacilityId = facilityId,
                    StatusId = 1, // Required field
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(12, 0),
                    Status = new Status // Required for mapping
                    {
                        StatusId = 1,
                        StatusName = "Available",
                        StatusDescription = "Available for booking"
                    }
                })
                .ToList();

            _timeslotRepoMock.Setup(x => x.GetByFacilityIdAsync(facilityId))
                .ReturnsAsync(mockTimeslots);

            // Act
            var result = await _service.GetTimeslotByFacilityIdAsync(facilityId, null, 2, 5);

            // Assert
            Assert.True(result.Success); // First check if successful
            Assert.Equal(2, result.Data.CurrentPage);
            Assert.Equal(5, result.Data.ItemsPerPage);
            Assert.Equal(3, result.Data.TotalPages);
            Assert.Equal(15, result.Data.TotalItems);
            Assert.Equal(5, result.Data.Items.Count());

            // Verify items are correctly ordered and paged
            var expectedIds = new List<int> { 6, 7, 8, 9, 10 };
            var actualIds = result.Data.Items.Select(x => x.TimeSlotId).ToList();
            Assert.Equal(expectedIds, actualIds);
        }

        [Fact(DisplayName = "UTCID07 - Should handle database error")]
        public async Task UTCID07_DatabaseError_ReturnsServerError()
        {
            // Arrange
            int facilityId = 1;
            _timeslotRepoMock.Setup(x => x.GetByFacilityIdAsync(facilityId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.GetTimeslotByFacilityIdAsync(facilityId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("Đã có lỗi xảy ra khi lấy TimeSlot", result.Message);
            Assert.Null(result.Data);
        }
    }
}