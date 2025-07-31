using System.Threading.Tasks;
using Moq;
using Xunit;
using B2P_API.Interface;
using B2P_API.Response;
using B2P_API.Services;

namespace B2P_Test.UnitTest.SliderManagementService_UnitTest
{
    public class DeleteSliderAsyncTest
    {
        private readonly Mock<ISliderManagementRepository> _repoMock = new();

        private SliderManagementService CreateService()
            => new SliderManagementService(_repoMock.Object);

        [Fact(DisplayName = "UTCID01 - Return 404 when slider to delete does not exist")]
        public async Task UTCID01_Return404WhenSliderNotFound()
        {
            // Arrange
            int slideId = 123;
            _repoMock.Setup(x => x.DeleteSliderAsync(slideId)).ReturnsAsync(false);

            var service = CreateService();

            // Act
            var result = await service.DeleteSliderAsync(slideId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Xóa slider thất bại. Slider không tồn tại.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Delete slider successfully returns 200 and slide id")]
        public async Task UTCID02_DeleteSlider_Success()
        {
            // Arrange
            int slideId = 10;
            _repoMock.Setup(x => x.DeleteSliderAsync(slideId)).ReturnsAsync(true);

            var service = CreateService();

            // Act
            var result = await service.DeleteSliderAsync(slideId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Xóa slider thành công.", result.Message);
            Assert.Equal(slideId.ToString(), result.Data);
        }
    }
}