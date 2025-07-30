using B2P_API.DTOs.CourtCategoryDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Services;
using B2P_API.Utils;
using Moq;

namespace B2P_Test.UnitTest.CourtCategoryService_UnitTest
{
    public class UpdateCourtCategoryAsyncTest
    {
        private readonly Mock<ICourtCategoryRepository> _categoryRepoMock;
        private readonly CourtCategoryService _service;

        public UpdateCourtCategoryAsyncTest()
        {
            _categoryRepoMock = new Mock<ICourtCategoryRepository>();
            _service = new CourtCategoryService(_categoryRepoMock.Object);
        }

        [Theory(DisplayName = "UTCID01 - Should return 400/404 for invalid requests")]
        [InlineData(null, "Valid Name", 400, MessagesCodes.MSG_81)]
        [InlineData(0, "Valid Name", 400, MessagesCodes.MSG_81)]
        [InlineData(-1, "Valid Name", 400, MessagesCodes.MSG_81)]
        [InlineData(1, null, 400, MessagesCodes.MSG_82)]
        [InlineData(1, "", 400, MessagesCodes.MSG_82)]
        [InlineData(1, "   ", 400, MessagesCodes.MSG_82)]
        [InlineData(1, "Valid Name", 404, MessagesCodes.MSG_83)]
        public async Task UTCID01_InvalidRequests_ReturnsErrorResponse(
            int? categoryId, string? categoryName, int expectedStatus, string expectedMessage)
        {
            // Setup
            var request = new CourtCategoryUpdateRequest
            {
                CategoryId = categoryId,
                CategoryName = categoryName
            };

            if (expectedStatus == 404)
            {
                _categoryRepoMock.Setup(x => x.GetCourtCategoryByIdAsync(categoryId!.Value))
                    .ReturnsAsync((CourtCategory?)null);
            }

            // Act
            var result = await _service.UpdateCourtCategoryAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(expectedStatus, result.Status);
            Assert.Equal(expectedMessage, result.Message);

            if (expectedStatus == 400 && (categoryId == null || categoryId <= 0))
            {
                Assert.Equal(false, result.Data);
            }
            else
            {
                Assert.Null(result.Data);
            }

            // Verify repository calls
            if (expectedStatus == 404)
            {
                _categoryRepoMock.Verify(x => x.GetCourtCategoryByIdAsync(categoryId!.Value), Times.Once);
                _categoryRepoMock.Verify(x => x.UpdateCourtCategoryAsync(It.IsAny<CourtCategory>()), Times.Never);
            }
            else
            {
                _categoryRepoMock.Verify(x => x.GetCourtCategoryByIdAsync(It.IsAny<int>()), Times.Never);
                _categoryRepoMock.Verify(x => x.UpdateCourtCategoryAsync(It.IsAny<CourtCategory>()), Times.Never);
            }
        }

        [Fact(DisplayName = "UTCID02 - Should successfully update category")]
        public async Task UTCID02_ValidRequest_ReturnsSuccess()
        {
            // Setup
            var request = new CourtCategoryUpdateRequest
            {
                CategoryId = 1,
                CategoryName = "  Updated Category Name  "
            };

            var existingCategory = new CourtCategory
            {
                CategoryId = 1,
                CategoryName = "Old Category Name"
            };

            _categoryRepoMock.Setup(x => x.GetCourtCategoryByIdAsync(1))
                .ReturnsAsync(existingCategory);

            // ✅ FIXED: Return Task<bool> with true
            _categoryRepoMock.Setup(x => x.UpdateCourtCategoryAsync(It.IsAny<CourtCategory>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.UpdateCourtCategoryAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal(MessagesCodes.MSG_84, result.Message);
            Assert.Null(result.Data);

            // Verify trimming
            Assert.Equal("Updated Category Name", existingCategory.CategoryName);

            // Verify repository calls
            _categoryRepoMock.Verify(x => x.GetCourtCategoryByIdAsync(1), Times.Once);
            _categoryRepoMock.Verify(x => x.UpdateCourtCategoryAsync(
                It.Is<CourtCategory>(c => c.CategoryName == "Updated Category Name")), Times.Once);
        }

        [Fact(DisplayName = "UTCID03 - Should return 500 when repository throws exception")]
        public async Task UTCID03_RepositoryException_Returns500()
        {
            // Setup
            var request = new CourtCategoryUpdateRequest
            {
                CategoryId = 1,
                CategoryName = "Valid Name"
            };

            _categoryRepoMock.Setup(x => x.GetCourtCategoryByIdAsync(1))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _service.UpdateCourtCategoryAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("Database connection failed", result.Message);
            Assert.Null(result.Data);

            // Verify repository calls
            _categoryRepoMock.Verify(x => x.GetCourtCategoryByIdAsync(1), Times.Once);
            _categoryRepoMock.Verify(x => x.UpdateCourtCategoryAsync(It.IsAny<CourtCategory>()), Times.Never);
        }
    }
}