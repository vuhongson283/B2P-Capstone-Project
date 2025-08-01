using B2P_API.DTOs.CourtCategoryDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Services;
using B2P_API.Utils;
using Moq;

namespace B2P_Test.UnitTest.CourtCategoryService_UnitTest
{
    public class GetCourtCategoryByIdAsyncTest
    {
        private readonly Mock<ICourtCategoryRepository> _categoryRepoMock;
        private readonly CourtCategoryService _service;

        public GetCourtCategoryByIdAsyncTest()
        {
            _categoryRepoMock = new Mock<ICourtCategoryRepository>();
            _service = new CourtCategoryService(_categoryRepoMock.Object);
        }

        [Theory(DisplayName = "UTCID01 - Should return 404 for not found or 200 for success")]
        [InlineData(1, false, 404, MessagesCodes.MSG_73)]        // Category not found
        [InlineData(2, true, 200, MessagesCodes.MSG_86)]         // Category found - Success
        public async Task UTCID01_GetCategoryScenarios_ReturnsCorrectResponse(
            int id, bool categoryExists, int expectedStatus, string expectedMessage)
        {
            // Setup
            CourtCategory? mockCategory = null;
            if (categoryExists)
            {
                mockCategory = new CourtCategory
                {
                    CategoryId = id,
                    CategoryName = "Test Category Name"
                };
            }

            _categoryRepoMock.Setup(x => x.GetCourtCategoryByIdAsync(id))
                .ReturnsAsync(mockCategory);

            // Act
            var result = await _service.GetCourtCategoryByIdAsync(id);

            // Assert
            Assert.Equal(categoryExists, result.Success);
            Assert.Equal(expectedStatus, result.Status);
            Assert.Equal(expectedMessage, result.Message);

            if (categoryExists)
            {
                // Verify successful response with correct data mapping
                Assert.NotNull(result.Data);
                Assert.Equal(id, result.Data.CategoryId);
                Assert.Equal("Test Category Name", result.Data.CategoryName);
            }
            else
            {
                // Verify not found response
                Assert.Null(result.Data);
            }

            // Verify repository was called
            _categoryRepoMock.Verify(x => x.GetCourtCategoryByIdAsync(id), Times.Once);
        }

        [Fact(DisplayName = "UTCID02 - Should return 500 when repository throws exception")]
        public async Task UTCID02_RepositoryException_Returns500()
        {
            // Setup
            var validId = 1;
            _categoryRepoMock.Setup(x => x.GetCourtCategoryByIdAsync(validId))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _service.GetCourtCategoryByIdAsync(validId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("Database connection failed", result.Message);
            Assert.Null(result.Data);

            // Verify repository was called
            _categoryRepoMock.Verify(x => x.GetCourtCategoryByIdAsync(validId), Times.Once);
        }
    }
}