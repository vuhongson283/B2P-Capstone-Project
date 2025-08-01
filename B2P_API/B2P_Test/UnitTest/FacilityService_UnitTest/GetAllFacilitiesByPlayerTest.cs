using B2P_API.DTOs.FacilityDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Services;
using B2P_API.Utils;
using Moq;

namespace B2P_Test.UnitTest.FacilityService_UnitTest
{
    public class GetAllFacilitiesByPlayerTest
    {
        private readonly Mock<IFacilityRepositoryForUser> _facilityRepoForUserMock;
        private readonly Mock<IFacilityManageRepository> _facilityRepoMock;
        private readonly FacilityService _service;

        public GetAllFacilitiesByPlayerTest()
        {
            _facilityRepoForUserMock = new Mock<IFacilityRepositoryForUser>();
            _facilityRepoMock = new Mock<IFacilityManageRepository>();
            _service = new FacilityService(_facilityRepoMock.Object, _facilityRepoForUserMock.Object);
        }

        private List<Facility> CreateTestFacilities()
        {
            return new List<Facility>
            {
                new Facility
                {
                    FacilityId = 1,
                    StatusId = 1,
                    FacilityName = "Tennis Club",
                    Location = "HCM$$District 1, Ho Chi Minh City",
                    Courts = new List<Court>
                    {
                        new Court
                        {
                            CategoryId = 1,
                            PricePerHour = 100000,
                            BookingDetails = new List<BookingDetail>
                            {
                                new BookingDetail
                                {
                                    Booking = new Booking
                                    {
                                        Ratings = new List<Rating>
                                        {
                                            new Rating { Stars = 4 },
                                            new Rating { Stars = 5 }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    TimeSlots = new List<TimeSlot>
                    {
                        new TimeSlot { StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(22, 45), Discount = 0.1m },
                        new TimeSlot { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(21, 0), Discount = 0.2m }
                    },
                    Images = new List<Image>
                    {
                        new Image { ImageUrl = "tennis1.jpg", Order = 1 }
                    }
                },
                new Facility
                {
                    FacilityId = 2,
                    StatusId = 1,
                    FacilityName = "Basketball Arena",
                    Location = "HN$$District 2, Hanoi",
                    Courts = new List<Court>
                    {
                        new Court { CategoryId = 2, PricePerHour = 150000 }
                    },
                    TimeSlots = new List<TimeSlot>
                    {
                        new TimeSlot { StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(23, 30) }
                    }
                }
            };
        }

        [Fact(DisplayName = "UTCID01 - Should return 400 when request is null")]
        public async Task UTCID01_NullRequest_Returns400()
        {
            // Test: request == null branch
            var result = await _service.GetAllFacilitiesByPlayer(null!, 1, 10);

            Assert.Equal(400, result.Status);
            Assert.False(result.Success);
            Assert.Equal(MessagesCodes.MSG_80, result.Message);
            Assert.Null(result.Data);
        }

        [Theory(DisplayName = "UTCID02 - Should return 404 for no data scenarios")]
        [InlineData(true, false)]    // No active facilities
        [InlineData(false, true)]    // No Type provided  
        [InlineData(false, false)]   // No results after filtering
        public async Task UTCID02_NoDataScenarios_Returns404(bool noActiveFacilities, bool noType)
        {
            // Setup
            SearchFormRequest request;

            if (noType)
            {
                // Test: request.Type == null || !request.Type.Any()
                request = new SearchFormRequest { Type = null };
                _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer()).ReturnsAsync(CreateTestFacilities());
            }
            else if (noActiveFacilities)
            {
                // Test: activeFacilities == null || activeFacilities.Count == 0
                request = new SearchFormRequest { Type = new List<int> { 1 } };
                var inactiveFacilities = new List<Facility>
                {
                    new Facility { FacilityId = 1, StatusId = 2, FacilityName = "Inactive" }
                };
                _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer()).ReturnsAsync(inactiveFacilities);
            }
            else
            {
                // Test: filteredList.Count == 0 (no results after all filtering)
                request = new SearchFormRequest
                {
                    Type = new List<int> { 3 },  // No matching type
                    Name = "NonExistent",        // No matching name  
                    City = "NonExistent"         // No matching city
                };
                _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer()).ReturnsAsync(CreateTestFacilities());
            }

            var result = await _service.GetAllFacilitiesByPlayer(request, 1, 10);

            Assert.Equal(404, result.Status);
            Assert.False(result.Success);
            Assert.Equal(MessagesCodes.MSG_72, result.Message);
            Assert.Null(result.Data);
        }

        [Theory(DisplayName = "UTCID03 - Should handle success scenarios with all logic branches")]
        [InlineData("Tennis", "HCM", "District 1", new int[] { 1 }, 1)]  // All filters + price asc sort + all mapping logic
        [InlineData("", null, null, new int[] { 1, 2 }, 2)]              // Multiple types + price desc sort
        [InlineData("", null, null, new int[] { 1, 2 }, 3)]              // Rating desc sort + no sorting (default)
        public async Task UTCID03_SuccessScenarios_Returns200(
            string name, string city, string ward, int[] types, int order)
        {
            // Setup
            var request = new SearchFormRequest
            {
                Name = name,
                City = city,
                Ward = ward,
                Type = types?.ToList(),
                Order = order
            };

            _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer()).ReturnsAsync(CreateTestFacilities());

            var result = await _service.GetAllFacilitiesByPlayer(request, 1, 10);

            // Test success response
            Assert.Equal(200, result.Status);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Contains("Tìm thấy", result.Message);

            // Test all response mapping logic with Tennis Club (when included)
            if (types.Contains(1))
            {
                var tennisClub = result.Data.Items.FirstOrDefault(x => x.FacilityId == 1);
                if (tennisClub != null)
                {
                    // Test ALL complex mapping logic in one assertion
                    Assert.Equal("District 1, Ho Chi Minh City", tennisClub.Location); // Location parsing with $$
                    Assert.Equal("08:30 - 22:45", tennisClub.OpenTime); // Time rounding + formatting
                    Assert.Equal("tennis1.jpg", tennisClub.FirstImage); // Image ordering
                    Assert.Equal(4.5, tennisClub.AverageRating); // Rating calculation
                    Assert.Equal(100000, tennisClub.PricePerHour); // Min price calculation
                    Assert.Equal(10000, tennisClub.MinPrice); // Price * minDiscount (0.1)
                    Assert.Equal(20000, tennisClub.MaxPrice); // Price * maxDiscount (0.2)
                }
            }

            // Test sorting logic (when multiple items exist)
            var itemsList = result.Data.Items.ToList();
            if (itemsList.Count > 1)
            {
                if (order == 1) // Price ascending
                {
                    Assert.True(itemsList[0].PricePerHour <= itemsList[1].PricePerHour);
                }
                else if (order == 2) // Price descending
                {
                    Assert.True(itemsList[0].PricePerHour >= itemsList[1].PricePerHour);
                }
                else if (order == 3) // Rating descending
                {
                    Assert.True(itemsList[0].AverageRating >= itemsList[1].AverageRating);
                }
            }
        }

        [Theory(DisplayName = "UTCID04 - Should return 400 for invalid pagination")]
        [InlineData(0)]   // pageNumber < 1
        [InlineData(5)]   // pageNumber > totalPages
        public async Task UTCID04_InvalidPagination_Returns400(int pageNumber)
        {
            // Setup
            var request = new SearchFormRequest { Type = new List<int> { 1, 2 } };
            _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer()).ReturnsAsync(CreateTestFacilities());

            // Test: pageNumber < 1 || pageNumber > totalPages
            var result = await _service.GetAllFacilitiesByPlayer(request, pageNumber, 10);

            Assert.Equal(400, result.Status);
            Assert.False(result.Success);
            Assert.Equal(MessagesCodes.MSG_78, result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Should handle pagination correctly")]
        public async Task UTCID05_ValidPagination_ReturnsCorrectPage()
        {
            // Setup
            var request = new SearchFormRequest { Type = new List<int> { 1, 2 } };
            _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer()).ReturnsAsync(CreateTestFacilities());

            // Test: Valid pagination with pageSize=1 (2 items = 2 pages)
            var result = await _service.GetAllFacilitiesByPlayer(request, 2, 1);

            Assert.Equal(200, result.Status);
            Assert.True(result.Success);
            Assert.Equal(2, result.Data.CurrentPage);
            Assert.Equal(1, result.Data.ItemsPerPage);
            Assert.Equal(2, result.Data.TotalPages);
            Assert.Equal(2, result.Data.TotalItems);
            Assert.Equal(1, result.Data.Items.Count()); // Only 1 item on page 2
        }

        [Fact(DisplayName = "UTCID06 - Should return 500 when repository throws exception")]
        public async Task UTCID06_RepositoryException_Returns500()
        {
            // Setup
            var request = new SearchFormRequest { Type = new List<int> { 1 } };
            _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer())
                .ThrowsAsync(new Exception("Database connection failed"));

            // Test: catch (Exception ex) branch
            var result = await _service.GetAllFacilitiesByPlayer(request, 1, 10);

            Assert.Equal(500, result.Status);
            Assert.False(result.Success);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("Database connection failed", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID07 - Should return 404 when facilities is null (activeFacilities == null)")]
        public async Task UTCID07_FacilitiesNull_Returns404()
        {
            // Arrange
            var request = new SearchFormRequest { Type = new List<int> { 1 } };
            _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer()).ReturnsAsync((List<Facility>)null);

            // Act
            var result = await _service.GetAllFacilitiesByPlayer(request, 1, 10);

            // Assert
            Assert.Equal(404, result.Status);
            Assert.False(result.Success);
            Assert.Equal(MessagesCodes.MSG_72, result.Message);
            Assert.Null(result.Data);
        }
    }
}