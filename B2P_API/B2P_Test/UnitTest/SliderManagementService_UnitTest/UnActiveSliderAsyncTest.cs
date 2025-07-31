using System.Threading.Tasks;
using Moq;
using Xunit;
using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Services;

namespace B2P_Test.UnitTest.SliderManagementService_UnitTest
{
    public class UnActiveSliderAsyncTest
    {
        private readonly Mock<ISliderManagementRepository> _repoMock = new();

        private SliderManagementService CreateService()
            => new SliderManagementService(_repoMock.Object);

        [Fact(DisplayName = "UTCID01 - Return 400 when slider cannot be deactivated")]
        public async Task UTCID01_Return400WhenSliderCannotBeDeactivated()
        {
            // Arrange
            int slideId = 123;
            _repoMock.Setup(x => x.UnActiveSliderAsync(slideId)).ReturnsAsync(false);

            var service = CreateService();

            // Act
            var result = await service.UnActiveSliderAsync(slideId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Không thể tắt slider. Có thể slider không tồn tại hoặc đã được tắt trước đó.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Deactivate slider successfully returns 200 and slide id")]
        public async Task UTCID02_DeactivateSlider_Success()
        {
            // Arrange
            int slideId = 10;
            _repoMock.Setup(x => x.UnActiveSliderAsync(slideId)).ReturnsAsync(true);

            var service = CreateService();

            // Act
            var result = await service.UnActiveSliderAsync(slideId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Tắt slider thành công.", result.Message);
            Assert.Equal(slideId.ToString(), result.Data);
        }
    }
}