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
    public class DeleteFacilityTest
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
            _manageRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Facility)null);

            var service = CreateService();

            var result = await service.DeleteFacility(11);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy cơ sở hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Facility has active bookings returns 400")]
        public async Task UTCID02_HasActiveBookings_Returns400()
        {
            var facility = new Facility { FacilityId = 12, FacilityName = "F" };
            _manageRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(facility);
            _manageRepoMock.Setup(x => x.HasActiveBookingsAsync(It.IsAny<int>())).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.DeleteFacility(12);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Không thể xóa cơ sở này vì đang có booking hoạt động", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - DeleteCascadeAsync returns false returns 500")]
        public async Task UTCID03_DeleteCascadeReturnsFalse_Returns500()
        {
            var facility = new Facility { FacilityId = 13, FacilityName = "F" };
            _manageRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(facility);
            _manageRepoMock.Setup(x => x.HasActiveBookingsAsync(It.IsAny<int>())).ReturnsAsync(false);
            _manageRepoMock.Setup(x => x.DeleteCascadeAsync(It.IsAny<int>())).ReturnsAsync(false);

            var service = CreateService();

            var result = await service.DeleteFacility(13);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Xóa cơ sở thất bại", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Exception thrown returns 500")]
        public async Task UTCID04_ExceptionThrown_Returns500()
        {
            var facility = new Facility { FacilityId = 14, FacilityName = "F" };
            _manageRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(facility);
            _manageRepoMock.Setup(x => x.HasActiveBookingsAsync(It.IsAny<int>())).ThrowsAsync(new Exception("db error!"));

            var service = CreateService();

            var result = await service.DeleteFacility(14);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.StartsWith("Đã xảy ra lỗi khi xóa:", result.Message);
            Assert.Contains("db error", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Success returns 200")]
        public async Task UTCID05_Success_Returns200()
        {
            var facility = new Facility { FacilityId = 15, FacilityName = "F" };
            _manageRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(facility);
            _manageRepoMock.Setup(x => x.HasActiveBookingsAsync(It.IsAny<int>())).ReturnsAsync(false);
            _manageRepoMock.Setup(x => x.DeleteCascadeAsync(It.IsAny<int>())).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.DeleteFacility(15);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Xóa cơ sở và tất cả dữ liệu liên quan thành công", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(15, result.Data.FacilityId);
        }
    }
}