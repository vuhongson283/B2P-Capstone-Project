using B2P_API.DTOs.SliderDTOs;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using B2P_API.Interface;

namespace B2P_Test.UnitTest.SliderManagementService_UnitTest
{
    public class GetAllSlidersAsyncTest
    {
        private readonly Mock<ISliderManagementRepository> _repoMock = new();

        private SliderManagementService CreateService()
        {
            return new SliderManagementService(_repoMock.Object);
        }

        private List<Slider> CreateSliders()
        {
            return new List<Slider>
            {
                new Slider
                {
                    SlideId = 1,
                    SlideDescription = "Desc1",
                    SlideUrl = "url1",
                    Status = new Status { StatusName = "Active" },
                    Images = new List<Image>
                    {
                        new Image { ImageUrl = "img1.jpg", Order = 2 },
                        new Image { ImageUrl = "img0.jpg", Order = 1 }
                    }
                },
                new Slider
                {
                    SlideId = 2,
                    SlideDescription = "Desc2",
                    SlideUrl = "url2",
                    Status = null,
                    Images = null
                }
            };
        }

        [Fact(DisplayName = "UTCID01 - Success with items (all mapping branches)")]
        public async Task UTCID01_Success_AllBranches()
        {
            var req = new GetListSliderRequest
            {
                PageNumber = 1,
                PageSize = 2,
                Search = null,
                StatusId = null
            };

            var sliders = CreateSliders();
            _repoMock.Setup(x => x.GetAllSlidersAsync(1, 2, null, null)).ReturnsAsync(sliders);
            _repoMock.Setup(x => x.GetTotalSlidersAsync(null, null)).ReturnsAsync(2);

            var service = CreateService();
            var result = await service.GetAllSlidersAsync(req);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy danh sách slider thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.TotalItems);
            Assert.Equal(1, result.Data.CurrentPage);
            Assert.Equal(2, result.Data.ItemsPerPage);
            Assert.Equal(1, result.Data.TotalPages);
            Assert.Equal(2, result.Data.Items.Count());

            // Check mapping
            var first = result.Data.Items.First();
            Assert.Equal(1, first.SlideId);
            Assert.Equal("Desc1", first.SlideDescription);
            Assert.Equal("url1", first.SlideUrl);
            Assert.Equal("Active", first.StatusName);
            Assert.Equal("img0.jpg", first.ImageUrl); // Ordered image

            var second = result.Data.Items.Skip(1).First();
            Assert.Equal(2, second.SlideId);
            Assert.Null(second.StatusName); // Status null branch
            Assert.Null(second.ImageUrl); // Images null branch
        }

        [Fact(DisplayName = "UTCID02 - Empty list")]
        public async Task UTCID02_EmptyList()
        {
            var req = new GetListSliderRequest
            {
                PageNumber = 1,
                PageSize = 10,
                Search = "no",
                StatusId = 2
            };
            _repoMock.Setup(x => x.GetAllSlidersAsync(1, 10, "no", 2)).ReturnsAsync(new List<Slider>());
            _repoMock.Setup(x => x.GetTotalSlidersAsync("no", 2)).ReturnsAsync(0);

            var service = CreateService();
            var result = await service.GetAllSlidersAsync(req);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy danh sách slider thành công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(0, result.Data.TotalItems);
            Assert.Equal(0, result.Data.TotalPages);
            Assert.Empty(result.Data.Items);
        }

        [Fact(DisplayName = "UTCID03 - Exception handling")]
        public async Task UTCID03_ExceptionHandling()
        {
            var req = new GetListSliderRequest
            {
                PageNumber = 1,
                PageSize = 5,
                Search = null,
                StatusId = null
            };
            _repoMock.Setup(x => x.GetAllSlidersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
                .ThrowsAsync(new Exception("DB error!"));

            var service = CreateService();
            var result = await service.GetAllSlidersAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("Lỗi hệ thống: DB error!", result.Message);
            Assert.Null(result.Data);
        }
    }
}