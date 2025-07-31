using System;
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
    public class GetAllSlidersByStatusAsyncTest
    {
        private readonly Mock<ISliderManagementRepository> _repoMock = new();

        private SliderManagementService CreateService()
            => new SliderManagementService(_repoMock.Object);

        [Fact(DisplayName = "UTCID01 - Return paged active sliders with images")]
        public async Task UTCID01_ReturnPagedActiveSliders_WithImages()
        {
            // Arrange
            int pageNumber = 1;
            int pageSize = 2;
            var sliders = new List<Slider>
            {
                new Slider
                {
                    SlideUrl = "url1",
                    Images = new List<Image>
                    {
                        new Image { ImageUrl = "imgB.jpg", Order = 2 },
                        new Image { ImageUrl = "imgA.jpg", Order = 1 }
                    }
                },
                new Slider
                {
                    SlideUrl = "url2",
                    Images = null
                },
                new Slider
                {
                    SlideUrl = "url3",
                    Images = new List<Image>
                    {
                        new Image { ImageUrl = "imgC.jpg", Order = 1 }
                    }
                }
            };
            _repoMock.Setup(x => x.GetAllSlidersByStatusAsync(pageNumber, pageSize, 1)).ReturnsAsync(sliders);

            var service = CreateService();

            // Act
            var result = await service.GetAllSlidersByStatusAsync(pageNumber, pageSize);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy danh sách slider đang bật thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(pageNumber, result.Data.CurrentPage);
            Assert.Equal(pageSize, result.Data.ItemsPerPage);
            Assert.Equal(3, result.Data.TotalItems);
            Assert.Equal(2, result.Data.TotalPages); // 3/2 = 1.5 -> 2

            var items = result.Data.Items.ToList();
            Assert.Equal(3, items.Count);
            Assert.Equal("url1", items[0].SlideUrl);
            Assert.Equal("imgA.jpg", items[0].ImageUrl); // Order 1
            Assert.Equal("url2", items[1].SlideUrl);
            Assert.Null(items[1].ImageUrl); // No images
            Assert.Equal("url3", items[2].SlideUrl);
            Assert.Equal("imgC.jpg", items[2].ImageUrl);
        }

        [Fact(DisplayName = "UTCID02 - Return empty paged result when no active sliders")]
        public async Task UTCID02_ReturnEmptyPagedResult_WhenNoActiveSliders()
        {
            // Arrange
            int pageNumber = 1;
            int pageSize = 10;
            var sliders = new List<Slider>();
            _repoMock.Setup(x => x.GetAllSlidersByStatusAsync(pageNumber, pageSize, 1)).ReturnsAsync(sliders);

            var service = CreateService();

            // Act
            var result = await service.GetAllSlidersByStatusAsync(pageNumber, pageSize);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy danh sách slider đang bật thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(pageNumber, result.Data.CurrentPage);
            Assert.Equal(pageSize, result.Data.ItemsPerPage);
            Assert.Equal(0, result.Data.TotalItems);
            Assert.Equal(0, result.Data.TotalPages);
            Assert.Empty(result.Data.Items);
        }
    }
}