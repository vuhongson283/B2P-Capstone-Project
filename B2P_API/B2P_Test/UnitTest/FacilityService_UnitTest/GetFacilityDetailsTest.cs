using B2P_API.DTOs.FacilityDTO;
using B2P_API.DTOs.FacilityDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.FacilityService_UnitTest
{
    public class GetFacilityDetailsTest
    {
        private readonly Mock<IFacilityManageRepository> _manageRepoMock = new();
        private readonly Mock<IFacilityRepositoryForUser> _userRepoMock = new();

        private FacilityService CreateService()
        {
            return new FacilityService(_manageRepoMock.Object, _userRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - FacilityDetailsDto is null returns 404")]
        public async Task UTCID01_FacilityDetailsDtoNull_Returns404()
        {
            _userRepoMock.Setup(x => x.GetFacilityDetails(It.IsAny<int>())).ReturnsAsync((FacilityDetailsDto)null);

            var service = CreateService();

            var result = await service.GetFacilityDetails(1);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy cơ sở.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Success returns 200")]
        public async Task UTCID02_Success_Returns200()
        {
            var dto = new FacilityDetailsDto
            {
                FacilityId = 123,
                FacilityName = "Facility X"
                // Thêm các property khác nếu cần
            };
            _userRepoMock.Setup(x => x.GetFacilityDetails(It.IsAny<int>())).ReturnsAsync(dto);

            var service = CreateService();

            var result = await service.GetFacilityDetails(123);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy thông tin cơ sở thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(123, result.Data.FacilityId);
            Assert.Equal("Facility X", result.Data.FacilityName);
        }
    }
}