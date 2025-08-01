using System.Collections.Generic;
using System.Linq;
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
    public class GetSliderByIdAsyncTest
    {
        private readonly Mock<ISliderManagementRepository> _repoMock = new();

        private SliderManagementService CreateService()
            => new SliderManagementService(_repoMock.Object);

        [Fact(DisplayName = "UTCID01 - Return 404 when slider does not exist")]
        public async Task UTCID01_Return404WhenSliderNotFound()
        {
            // Arrange
            int slideId = 123;
            _repoMock.Setup(x => x.GetSliderByIdAsync(slideId)).ReturnsAsync((Slider)null);

            var service = CreateService();

            // Act
            var result = await service.GetSliderByIdAsync(slideId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy slider.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Return slider details with all fields")]
        public async Task UTCID02_ReturnSliderDetails_AllFields()
        {
            // Arrange
            int slideId = 10;
            var slider = new Slider
            {
                SlideId = slideId,
                SlideUrl = "slider-url",
                SlideDescription = "desc",
                StatusId = 2,
                Status = new Status { StatusName = "Active" },
                Images = new List<Image>
                {
                    new Image { ImageId = 88, ImageUrl = "imgB.jpg", Order = 2 },
                    new Image { ImageId = 77, ImageUrl = "imgA.jpg", Order = 1 }
                }
            };
            _repoMock.Setup(x => x.GetSliderByIdAsync(slideId)).ReturnsAsync(slider);

            var service = CreateService();

            // Act
            var result = await service.GetSliderByIdAsync(slideId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy chi tiết slider thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(slideId, result.Data.SlideId);
            Assert.Equal("slider-url", result.Data.SlideUrl);
            Assert.Equal("desc", result.Data.SlideDescription);
            Assert.Equal(2, result.Data.StatusId);
            Assert.Equal("Active", result.Data.StatusName);
            Assert.Equal("imgA.jpg", result.Data.ImageUrl); // Smallest order image
            Assert.Equal(77, result.Data.ImageId); // ImageId of smallest order image
        }

        [Fact(DisplayName = "UTCID03 - Return slider details when Status and Images are null")]
        public async Task UTCID03_ReturnSliderDetails_StatusAndImagesNull()
        {
            // Arrange
            int slideId = 11;
            var slider = new Slider
            {
                SlideId = slideId,
                SlideUrl = "url",
                SlideDescription = "desc",
                StatusId = 3,
                Status = null,
                Images = null
            };
            _repoMock.Setup(x => x.GetSliderByIdAsync(slideId)).ReturnsAsync(slider);

            var service = CreateService();

            // Act
            var result = await service.GetSliderByIdAsync(slideId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy chi tiết slider thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(slideId, result.Data.SlideId);
            Assert.Equal("url", result.Data.SlideUrl);
            Assert.Equal("desc", result.Data.SlideDescription);
            Assert.Equal(3, result.Data.StatusId);
            Assert.Null(result.Data.StatusName);
            Assert.Null(result.Data.ImageUrl);
            Assert.Null(result.Data.ImageId);
        }

        [Fact(DisplayName = "UTCID04 - Return slider details when Images is empty")]
        public async Task UTCID04_ReturnSliderDetails_ImagesEmpty()
        {
            // Arrange
            int slideId = 12;
            var slider = new Slider
            {
                SlideId = slideId,
                SlideUrl = "url-empty",
                SlideDescription = "desc-empty",
                StatusId = 4,
                Status = new Status { StatusName = "Deactive" },
                Images = new List<Image>() // Empty list
            };
            _repoMock.Setup(x => x.GetSliderByIdAsync(slideId)).ReturnsAsync(slider);

            var service = CreateService();

            // Act
            var result = await service.GetSliderByIdAsync(slideId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy chi tiết slider thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(slideId, result.Data.SlideId);
            Assert.Equal("url-empty", result.Data.SlideUrl);
            Assert.Equal("desc-empty", result.Data.SlideDescription);
            Assert.Equal(4, result.Data.StatusId);
            Assert.Equal("Deactive", result.Data.StatusName);
            Assert.Null(result.Data.ImageUrl); // Images is empty, so null
            Assert.Null(result.Data.ImageId);  // Images is empty, so null
        }
    }
}