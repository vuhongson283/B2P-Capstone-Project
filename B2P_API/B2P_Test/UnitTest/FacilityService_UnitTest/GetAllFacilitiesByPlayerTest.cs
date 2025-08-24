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
                            StatusId = 1, // Fix: court must be active
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
                        new TimeSlot { StatusId = 1, StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(22, 45), Discount = 10 }, // 10%
                        new TimeSlot { StatusId = 1, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(21, 0), Discount = 20 }   // 20%
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
                        new Court { CategoryId = 2, StatusId = 1, PricePerHour = 150000 }
                    },
                    TimeSlots = new List<TimeSlot>
                    {
                        new TimeSlot { StatusId = 1, StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(23, 30), Discount = 0 }
                    }
                }
            };
        }

        [Fact(DisplayName = "UTCID01 - Should return 400 when request is null")]
        public async Task UTCID01_NullRequest_Returns400()
        {
            var result = await _service.GetAllFacilitiesByPlayer(null!, 1, 10);

            Assert.Equal(400, result.Status);
            Assert.False(result.Success);
            Assert.Equal(MessagesCodes.MSG_80, result.Message);
            Assert.Null(result.Data);
        }

        [Theory(DisplayName = "UTCID02 - Should return 404 for no data scenarios")]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public async Task UTCID02_NoDataScenarios_Returns404(bool noActiveFacilities, bool noType)
        {
            SearchFormRequest request;

            if (noType)
            {
                request = new SearchFormRequest { Type = null };
                _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer()).ReturnsAsync(CreateTestFacilities());
            }
            else if (noActiveFacilities)
            {
                request = new SearchFormRequest { Type = new List<int> { 1 } };
                var inactiveFacilities = new List<Facility>
                {
                    new Facility { FacilityId = 1, StatusId = 2, FacilityName = "Inactive",
                        Courts = new List<Court> { new Court { StatusId = 1, CategoryId = 1, PricePerHour = 1 } } }
                };
                _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer()).ReturnsAsync(inactiveFacilities);
            }
            else
            {
                request = new SearchFormRequest
                {
                    Type = new List<int> { 3 },
                    Name = "NonExistent",
                    City = "NonExistent"
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
        [InlineData("Tennis", "HCM", "District 1", new int[] { 1 }, 1)]
        [InlineData("", null, null, new int[] { 1, 2 }, 2)]
        [InlineData("", null, null, new int[] { 1, 2 }, 3)]
        public async Task UTCID03_SuccessScenarios_Returns200(
            string name, string city, string ward, int[] types, int order)
        {
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

            Assert.Equal(200, result.Status);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Contains("Tìm thấy", result.Message);

            if (types != null && types.Contains(1))
            {
                var tennisClub = result.Data.Items.FirstOrDefault(x => x.FacilityId == 1);
                if (tennisClub != null)
                {
                    Assert.Equal("District 1, Ho Chi Minh City", tennisClub.Location);
                    Assert.Equal("08:30 - 22:45", tennisClub.OpenTime);
                    Assert.Equal("tennis1.jpg", tennisClub.FirstImage);
                    Assert.Equal(4.5, tennisClub.AverageRating);
                    Assert.Equal(100000, tennisClub.PricePerHour);

                    // MinPrice = Price * (1 - maxDiscount/100) = 100000 * (1 - 0.2) = 80000
                    // MaxPrice = Price * (1 - minDiscount/100) = 100000 * (1 - 0.1) = 90000
                    Assert.Equal(80000, tennisClub.MinPrice);
                    Assert.Equal(90000, tennisClub.MaxPrice);
                }
            }

            var itemsList = result.Data.Items.ToList();
            if (itemsList.Count > 1)
            {
                if (order == 1)
                {
                    Assert.True(itemsList[0].PricePerHour <= itemsList[1].PricePerHour);
                }
                else if (order == 2)
                {
                    Assert.True(itemsList[0].PricePerHour >= itemsList[1].PricePerHour);
                }
                else if (order == 3)
                {
                    Assert.True(itemsList[0].AverageRating >= itemsList[1].AverageRating);
                }
            }
        }

        [Theory(DisplayName = "UTCID04 - Should return 400 for invalid pagination")]
        [InlineData(0)]
        [InlineData(5)]
        public async Task UTCID04_InvalidPagination_Returns400(int pageNumber)
        {
            var request = new SearchFormRequest { Type = new List<int> { 1, 2 } };
            _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer()).ReturnsAsync(CreateTestFacilities());

            var result = await _service.GetAllFacilitiesByPlayer(request, pageNumber, 1);

            Assert.Equal(400, result.Status);
            Assert.False(result.Success);
            Assert.Equal(MessagesCodes.MSG_78, result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID07 - Should return 404 when facilities is null (activeFacilities == null)")]
        public async Task UTCID07_FacilitiesNull_Returns404()
        {
            var request = new SearchFormRequest { Type = new List<int> { 1 } };
            _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer()).ReturnsAsync((List<Facility>?)null);

            var result = await _service.GetAllFacilitiesByPlayer(request, 1, 10);

            Assert.Equal(404, result.Status);
            Assert.False(result.Success);
            Assert.Equal(MessagesCodes.MSG_72, result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Should handle pagination correctly")]
        public async Task UTCID05_ValidPagination_ReturnsCorrectPage()
        {
            var request = new SearchFormRequest { Type = new List<int> { 1, 2 } };
            _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer()).ReturnsAsync(CreateTestFacilities());

            var result = await _service.GetAllFacilitiesByPlayer(request, 2, 1);

            Assert.Equal(200, result.Status);
            Assert.True(result.Success);
            Assert.Equal(2, result.Data.CurrentPage);
            Assert.Equal(1, result.Data.ItemsPerPage);
            Assert.Equal(2, result.Data.TotalPages);
            Assert.Equal(2, result.Data.TotalItems);
            Assert.Single(result.Data.Items);
        }

        [Fact(DisplayName = "UTCID06 - Should return 500 when repository throws exception")]
        public async Task UTCID06_RepositoryException_Returns500()
        {
            var request = new SearchFormRequest { Type = new List<int> { 1 } };
            _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer())
                .ThrowsAsync(new Exception("Database connection failed"));

            var result = await _service.GetAllFacilitiesByPlayer(request, 1, 10);

            Assert.Equal(500, result.Status);
            Assert.False(result.Success);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("Database connection failed", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Should handle pagination correctly (Duplicate Fix)")]
        public async Task UTCID05_ValidPagination_ReturnsCorrectPage_Duplicate()
        {
            var request = new SearchFormRequest { Type = new List<int> { 1, 2 } };
            _facilityRepoForUserMock.Setup(x => x.GetAllFacilitiesByPlayer()).ReturnsAsync(CreateTestFacilities());

            var result = await _service.GetAllFacilitiesByPlayer(request, 2, 1);

            Assert.Equal(200, result.Status);
            Assert.True(result.Success);
            Assert.Equal(2, result.Data.CurrentPage);
            Assert.Equal(1, result.Data.ItemsPerPage);
            Assert.Equal(2, result.Data.TotalPages);
            Assert.Equal(2, result.Data.TotalItems);
            Assert.Single(result.Data.Items);
        }
    }
}