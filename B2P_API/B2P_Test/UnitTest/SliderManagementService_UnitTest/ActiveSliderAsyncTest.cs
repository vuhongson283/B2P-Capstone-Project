using System.Threading.Tasks;
using Moq;
using Xunit;
using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Services;

namespace B2P_Test.UnitTest.SliderManagementService_UnitTest
{
    public class ActiveSliderAsyncTest
    {
        private readonly Mock<ISliderManagementRepository> _repoMock = new();

        private SliderManagementService CreateService()
            => new SliderManagementService(_repoMock.Object);

        [Fact(DisplayName = "UTCID01 - Return 400 when slider cannot be activated")]
        public async Task UTCID01_Return400WhenSliderCannotBeActivated()
        {
            // Arrange
            int slideId = 123;
            _repoMock.Setup(x => x.ActiveSliderAsync(slideId)).ReturnsAsync(false);

            var service = CreateService();

            // Act
            var result = await service.ActiveSliderAsync(slideId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Không thể bật slider. Có thể slider không tồn tại hoặc đã được bật trước đó.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Activate slider successfully returns 200 and slide id")]
        public async Task UTCID02_ActivateSlider_Success()
        {
            // Arrange
            int slideId = 10;
            _repoMock.Setup(x => x.ActiveSliderAsync(slideId)).ReturnsAsync(true);

            var service = CreateService();

            // Act
            var result = await service.ActiveSliderAsync(slideId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Bật slider thành công.", result.Message);
            Assert.Equal(slideId.ToString(), result.Data);
        }
    }
}