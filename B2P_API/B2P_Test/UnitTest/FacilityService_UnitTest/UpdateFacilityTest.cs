using B2P_API.DTOs.FacilityDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.FacilityService_UnitTest
{
    public class UpdateFacilityTest
    {
        private readonly Mock<IFacilityManageRepository> _manageRepoMock = new();
        private readonly Mock<IFacilityRepositoryForUser> _userRepoMock = new();

        private FacilityService CreateService()
        {
            return new FacilityService(_manageRepoMock.Object, _userRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Facility not found returns 404")]
        public async Task UTCID01_FacilityNotFound_Returns404()
        {
            var req = new UpdateFacilityRequest
            {
                FacilityName = "Name",
                StatusId = 1
            };
            _manageRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Facility)null);

            var service = CreateService();

            var result = await service.UpdateFacility(req, 123);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy cơ sở hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - FacilityName is null or whitespace returns 400")]
        public async Task UTCID02_FacilityNameIsNullOrWhitespace_Returns400()
        {
            var req = new UpdateFacilityRequest
            {
                FacilityName = "   ",
                StatusId = 1
            };
            _manageRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Facility());

            var service = CreateService();

            var result = await service.UpdateFacility(req, 123);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tên cơ sở không được để trống hoặc chỉ chứa khoảng trắng", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - StatusId <= 0 returns 400")]
        public async Task UTCID03_StatusIdInvalid_Returns400()
        {
            var req = new UpdateFacilityRequest
            {
                FacilityName = "Facility",
                StatusId = 0
            };
            _manageRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Facility());

            var service = CreateService();

            var result = await service.UpdateFacility(req, 123);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Trạng thái không hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - UpdateAsync returns null returns 500")]
        public async Task UTCID04_UpdateAsyncReturnsNull_Returns500()
        {
            var req = new UpdateFacilityRequest
            {
                FacilityName = "Facility",
                StatusId = 1,
                Location = "HN",
                Contact = "0909"
            };
            var facility = new Facility();
            _manageRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(facility);
            _manageRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Facility>())).ReturnsAsync((Facility)null);

            var service = CreateService();

            var result = await service.UpdateFacility(req, 123);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Cập nhật thất bại", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Exception in repository returns 500")]
        public async Task UTCID05_ExceptionInRepository_Returns500()
        {
            var req = new UpdateFacilityRequest
            {
                FacilityName = "Facility",
                StatusId = 1,
                Location = "HN"
            };
            var facility = new Facility();
            _manageRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(facility);
            _manageRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Facility>()))
                .ThrowsAsync(new Exception("db error"));

            var service = CreateService();

            var result = await service.UpdateFacility(req, 123);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.StartsWith("Cập nhật cơ sở thất bại:", result.Message);
            Assert.Contains("db error", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID06 - Success returns 200")]
        public async Task UTCID06_Success_Returns200()
        {
            var req = new UpdateFacilityRequest
            {
                FacilityName = "  Facility X  ",
                StatusId = 2,
                Location = "  Đà Nẵng ",
                Contact = "  01234 "
            };
            var facility = new Facility
            {
                FacilityId = 222,
                FacilityName = "Old Name",
                StatusId = 1,
                Location = "Old",
                Contact = "Old"
            };
            var updated = new Facility
            {
                FacilityId = 222,
                FacilityName = "Facility X",
                StatusId = 2,
                Location = "Đà Nẵng",
                Contact = "01234"
            };
            _manageRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(facility);
            _manageRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Facility>())).ReturnsAsync(updated);

            var service = CreateService();

            var result = await service.UpdateFacility(req, 222);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật cơ sở thành công", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(222, result.Data.FacilityId);
            Assert.Equal("Facility X", result.Data.FacilityName);
            Assert.Equal("Đà Nẵng", result.Data.Location);
            Assert.Equal("01234", result.Data.Contact);
            Assert.Equal(2, result.Data.StatusId);
        }

        [Fact(DisplayName = "UTCID07 - Location and Contact are null or whitespace should set to null")]
        public async Task UTCID07_LocationAndContactNullOrWhitespace_SetToNull()
        {
            var req = new UpdateFacilityRequest
            {
                FacilityName = "Facility",
                StatusId = 1,
                Location = "    ",
                Contact = null
            };
            var facility = new Facility
            {
                FacilityId = 333,
                FacilityName = "Old Name",
                StatusId = 1,
                Location = "Old Location",
                Contact = "Old Contact"
            };
            var updated = new Facility
            {
                FacilityId = 333,
                FacilityName = "Facility",
                StatusId = 1,
                Location = null,
                Contact = null
            };
            _manageRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(facility);
            // Capture input to verify values are null
            _manageRepoMock.Setup(x => x.UpdateAsync(It.Is<Facility>(
                f => f.Location == null && f.Contact == null
            ))).ReturnsAsync(updated);

            var service = CreateService();

            var result = await service.UpdateFacility(req, 333);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.NotNull(result.Data);
            Assert.Null(result.Data.Location);
            Assert.Null(result.Data.Contact);
        }
    }
}