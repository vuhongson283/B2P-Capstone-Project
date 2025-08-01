using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.FacilityService_UnitTest
{
    public class GetFacilityByIdTest
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

            var result = await service.GetFacilityById(1);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy cơ sở hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Success returns 200")]
        public async Task UTCID02_Success_Returns200()
        {
            var facility = new Facility
            {
                FacilityId = 2,
                FacilityName = "Facility Z"
            };
            _manageRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(facility);

            var service = CreateService();

            var result = await service.GetFacilityById(2);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy thông tin cơ sở thành công", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.FacilityId);
            Assert.Equal("Facility Z", result.Data.FacilityName);
        }
    }
}