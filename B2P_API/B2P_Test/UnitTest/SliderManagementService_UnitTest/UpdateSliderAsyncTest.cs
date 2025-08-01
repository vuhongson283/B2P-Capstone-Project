using System.Threading.Tasks;
using Moq;
using Xunit;
using B2P_API.DTOs.SliderDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;

namespace B2P_Test.UnitTest.SliderManagementService_UnitTest
{
    public class UpdateSliderAsyncTest
    {
        private readonly Mock<ISliderManagementRepository> _repoMock = new();

        private SliderManagementService CreateService()
            => new SliderManagementService(_repoMock.Object);

        [Fact(DisplayName = "UTCID01 - Return 404 when slider to update does not exist")]
        public async Task UTCID01_Return404WhenSliderNotFound()
        {
            // Arrange
            int slideId = 123;
            var request = new UpdateSliderRequest
            {
                SlideUrl = "updated-url",
                SlideDescription = "updated description"
            };
            _repoMock.Setup(x => x.UpdateSliderAsync(slideId, It.IsAny<Slider>())).ReturnsAsync((Slider)null);

            var service = CreateService();

            // Act
            var result = await service.UpdateSliderAsync(slideId, request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy slider.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Update slider successfully returns 200 and updated slider id")]
        public async Task UTCID02_UpdateSlider_Success()
        {
            // Arrange
            int slideId = 10;
            var request = new UpdateSliderRequest
            {
                SlideUrl = "new-url",
                SlideDescription = "new description"
            };
            var updatedSlider = new Slider
            {
                SlideId = slideId,
                SlideUrl = "new-url",
                SlideDescription = "new description"
            };
            _repoMock.Setup(x => x.UpdateSliderAsync(slideId, It.IsAny<Slider>())).ReturnsAsync(updatedSlider);

            var service = CreateService();

            // Act
            var result = await service.UpdateSliderAsync(slideId, request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật slider thành công.", result.Message);
            Assert.Equal(slideId.ToString(), result.Data);
            _repoMock.Verify(x => x.UpdateSliderAsync(slideId, It.Is<Slider>(
                s => s.SlideUrl == request.SlideUrl
                  && s.SlideDescription == request.SlideDescription
            )), Times.Once);
        }
    }
}