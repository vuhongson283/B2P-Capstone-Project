using B2P_API.DTOs.CourtManagementDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2P_Test.UnitTest.CourtService_UnitTest
{
    public class GetAllCourtsTest
    {
        private readonly Mock<ICourtRepository> _courtRepoMock;
        private readonly CourtServices _service;
        public GetAllCourtsTest()
        {
            _courtRepoMock = new Mock<ICourtRepository>();
            _service = new CourtServices(_courtRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Should return success with valid pagination")]
        public async Task UTCID01_ValidPagination_ReturnsSuccess()
        {
            // Arrange
            var request = new CourtRequestDTO
            {
                PageNumber = 1,
                PageSize = 5
            };

            var mockResult = new PagedResponse<Court>
            {
                CurrentPage = 1,
                ItemsPerPage = 5,
                TotalItems = 10,
                TotalPages = 2,
                Items = new List<Court>
                {
                    new Court { CourtId = 1, CourtName = "Sân 1" },
                    new Court { CourtId = 2, CourtName = "Sân 2" }
                }
            };

            _courtRepoMock.Setup(x => x.GetAllCourts(request))
                .ReturnsAsync(mockResult);

            // Act
            var result = await _service.GetAllCourts(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Sân đã được lấy dữ liệu với phân trang thành công.", result.Message);
            Assert.Equal(2, result.Data.Items.Count());
            Assert.Equal(10, result.Data.TotalItems);
        }

        [Theory(DisplayName = "UTCID02 - Should correct invalid pagination parameters")]
        [InlineData(0, 5, 1, 5)]    // PageNumber <= 0
        [InlineData(1, 0, 1, 10)]   // PageSize <= 0
        [InlineData(1, 15, 1, 10)]  // PageSize > 10
        public async Task UTCID02_InvalidPagination_CorrectsParameters(
            int inputPage, int inputSize, int expectedPage, int expectedSize)
        {
            // Arrange
            var request = new CourtRequestDTO
            {
                PageNumber = inputPage,
                PageSize = inputSize
            };

            var mockResult = new PagedResponse<Court>
            {
                CurrentPage = expectedPage,
                ItemsPerPage = expectedSize,
                TotalItems = 10,
                TotalPages = 2,
                Items = new List<Court>
                {
                    new Court { CourtId = 1, CourtName = "Sân 1" }
                }
            };

            _courtRepoMock.Setup(x => x.GetAllCourts(It.Is<CourtRequestDTO>(r =>
                r.PageNumber == expectedPage && r.PageSize == expectedSize)))
                .ReturnsAsync(mockResult);

            // Act
            var result = await _service.GetAllCourts(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(expectedPage, result.Data.CurrentPage);
            Assert.Equal(expectedSize, result.Data.ItemsPerPage);
        }

        [Fact(DisplayName = "UTCID03 - Should return no results with search criteria")]
        public async Task UTCID03_SearchWithNoResults_ReturnsMessage()
        {
            // Arrange
            var request = new CourtRequestDTO
            {
                PageNumber = 1,
                PageSize = 10,
                Search = "Không tồn tại"
            };

            var mockResult = new PagedResponse<Court>
            {
                TotalItems = 0,
                Items = new List<Court>()
            };

            _courtRepoMock.Setup(x => x.GetAllCourts(request))
                .ReturnsAsync(mockResult);

            // Act
            var result = await _service.GetAllCourts(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Không có kết quả tìm kiếm phù hợp.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Should return no courts for facility")]
        public async Task UTCID04_NoCourtsForFacility_ReturnsMessage()
        {
            // Arrange
            var request = new CourtRequestDTO
            {
                PageNumber = 1,
                PageSize = 10
                // No search criteria
            };

            var mockResult = new PagedResponse<Court>
            {
                TotalItems = 0,
                Items = new List<Court>()
            };

            _courtRepoMock.Setup(x => x.GetAllCourts(request))
                .ReturnsAsync(mockResult);

            // Act
            var result = await _service.GetAllCourts(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cơ sở này không tồn tại sân nào trong hệ thống.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Should handle empty result with items")]
        public async Task UTCID05_EmptyItems_ReturnsAppropriateMessage()
        {
            // Arrange
            var request = new CourtRequestDTO
            {
                PageNumber = 1,
                PageSize = 10
            };

            var mockResult = new PagedResponse<Court>
            {
                CurrentPage = 1,
                ItemsPerPage = 10,
                TotalItems = 0,
                TotalPages = 0,
                Items = new List<Court>()
            };

            _courtRepoMock.Setup(x => x.GetAllCourts(request))
                .ReturnsAsync(mockResult);

            // Act
            var result = await _service.GetAllCourts(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cơ sở này không tồn tại sân nào trong hệ thống.", result.Message);
            Assert.Null(result.Data);
        }
    }
}
