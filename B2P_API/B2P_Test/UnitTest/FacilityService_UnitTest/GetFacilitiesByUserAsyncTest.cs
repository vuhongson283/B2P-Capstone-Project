using B2P_API.DTOs.FacilityDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
namespace B2P_Test.UnitTest.FacilityService_UnitTest
{
    public class GetFacilitiesByUserAsyncTest
    {
        private readonly Mock<IFacilityManageRepository> _facilityRepoMock = new();
        private readonly Mock<IFacilityRepositoryForUser> _facilityRepoForUserMock = new();

        private FacilityService CreateService()
        {
            return new FacilityService(_facilityRepoMock.Object, _facilityRepoForUserMock.Object);
        }

        private List<Facility> CreateFacilities()
        {
            return new List<Facility>
            {
                new Facility
                {
                    FacilityId = 1,
                    FacilityName = "Tennis Club",
                    Location = "HCM",
                    Courts = new List<Court> { new Court(), new Court() },
                    Status = new Status { StatusId = 1, StatusName = "Active", StatusDescription = "Hoạt động" },
                    Images = new List<Image> { new Image { ImageId = 100, ImageUrl = "img1.jpg", Order = 1, Caption = "cap1" } }
                },
                new Facility
                {
                    FacilityId = 2,
                    FacilityName = "Basketball Arena",
                    Location = "HN",
                    Courts = new List<Court> { new Court() },
                    Status = new Status { StatusId = 2, StatusName = "Inactive", StatusDescription = "Ngừng hoạt động" },
                    Images = new List<Image> { new Image { ImageId = 200, ImageUrl = "img2.jpg", Order = 1, Caption = "cap2" } }
                }
            };
        }

        [Fact(DisplayName = "UTCID01 - userId <= 0 returns 400")]
        public async Task UTCID01_UserIdInvalid_Returns400()
        {
            var service = CreateService();

            var result = await service.GetFacilitiesByUserAsync(0);

            Assert.Equal(400, result.Status);
            Assert.False(result.Success);
            Assert.Equal("UserId không hợp lệ", result.Message);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.TotalItems);
            Assert.Equal(0, result.Data.TotalPages);
        }

        [Fact(DisplayName = "UTCID02 - Exception in repo returns 500")]
        public async Task UTCID02_ExceptionInRepo_Returns500()
        {
            _facilityRepoMock.Setup(x => x.GetByUserIdAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("database error"));

            var service = CreateService();

            var result = await service.GetFacilitiesByUserAsync(1);

            Assert.Equal(500, result.Status);
            Assert.False(result.Success);
            Assert.Contains("database error", result.Message);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.TotalItems);
            Assert.Equal(0, result.Data.TotalPages);
        }

        [Fact(DisplayName = "UTCID03 - Filter by facilityName")]
        public async Task UTCID03_FilterByFacilityName()
        {
            var facilities = CreateFacilities();
            _facilityRepoMock.Setup(x => x.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync(facilities);

            var service = CreateService();

            var result = await service.GetFacilitiesByUserAsync(1, facilityName: "Tennis");

            Assert.Equal(200, result.Status);
            Assert.True(result.Success);
            Assert.Single(result.Data.Items);
            Assert.Contains("Tennis", result.Data.Items.First().FacilityName);
        }

        [Fact(DisplayName = "UTCID04 - Filter by statusId")]
        public async Task UTCID04_FilterByStatusId()
        {
            var facilities = CreateFacilities();
            _facilityRepoMock.Setup(x => x.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync(facilities);

            var service = CreateService();

            var result = await service.GetFacilitiesByUserAsync(1, statusId: 2);

            Assert.Equal(200, result.Status);
            Assert.True(result.Success);
            Assert.Single(result.Data.Items);
            Assert.Equal(2, result.Data.Items.First().Status.StatusId);
        }

        [Fact(DisplayName = "UTCID05 - Filter by facilityName and statusId (no match)")]
        public async Task UTCID05_FilterByNameAndStatus_NoMatch()
        {
            var facilities = CreateFacilities();
            _facilityRepoMock.Setup(x => x.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync(facilities);

            var service = CreateService();

            var result = await service.GetFacilitiesByUserAsync(1, facilityName: "Nonexistent", statusId: 1);

            Assert.Equal(200, result.Status);
            Assert.True(result.Success);
            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.TotalItems);
        }

        [Fact(DisplayName = "UTCID06 - Mapping logic and pagination (many items)")]
        public async Task UTCID06_MappingPagination()
        {
            var facilities = new List<Facility>();
            for (int i = 1; i <= 7; i++)
            {
                facilities.Add(new Facility
                {
                    FacilityId = i,
                    FacilityName = "Facility " + i,
                    Courts = new List<Court> { new Court(), new Court() },
                    Status = new Status { StatusId = i % 2 + 1, StatusName = "Status" + i, StatusDescription = "Desc" + i },
                    Images = new List<Image>
                    {
                        new Image { ImageId = i, ImageUrl = $"img{i}.jpg", Order = 1, Caption = $"cap{i}" }
                    }
                });
            }
            _facilityRepoMock.Setup(x => x.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync(facilities);

            var service = CreateService();

            // Page 2, 3 items per page
            var result = await service.GetFacilitiesByUserAsync(1, currentPage: 2, itemsPerPage: 3);

            Assert.Equal(200, result.Status);
            Assert.True(result.Success);
            Assert.Equal(2, result.Data.CurrentPage);
            Assert.Equal(3, result.Data.ItemsPerPage);
            Assert.Equal(7, result.Data.TotalItems);
            Assert.Equal(3, result.Data.TotalPages);
            Assert.Equal(3, result.Data.Items.Count());
            // Verify mapping: CourtCount, Images, StatusDto
            var dto = result.Data.Items.First();
            Assert.NotNull(dto.Images);
            Assert.Equal(2, dto.CourtCount);
            Assert.NotNull(dto.Status);
        }

        [Fact(DisplayName = "UTCID07 - Mapping when courts, images, status are null")]
        public async Task UTCID07_MappingWithNullCollections()
        {
            var facilities = new List<Facility>
            {
                new Facility
                {
                    FacilityId = 10,
                    FacilityName = "NullTest",
                    Courts = null,
                    Status = null,
                    Images = null,
                    Location = null
                }
            };
            _facilityRepoMock.Setup(x => x.GetByUserIdAsync(It.IsAny<int>())).ReturnsAsync(facilities);

            var service = CreateService();

            var result = await service.GetFacilitiesByUserAsync(1);

            Assert.Equal(200, result.Status);
            Assert.True(result.Success);
            Assert.Single(result.Data.Items);
            var dto = result.Data.Items.First();
            Assert.Equal(0, dto.CourtCount);
            Assert.Null(dto.Status);
            Assert.Null(dto.Images);
        }
    }
}