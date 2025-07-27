using B2P_API.DTOs.CourtCategoryDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Services;
using B2P_API.Utils;
using Moq;

namespace B2P_Test.UnitTest.CourtCategoryService_UnitTest
{
    public class GetAllCourtCategoriesAsyncTest
    {
        private readonly Mock<ICourtCategoryRepository> _categoryRepoMock;
        private readonly CourtCategoryService _service;

        public GetAllCourtCategoriesAsyncTest()
        {
            _categoryRepoMock = new Mock<ICourtCategoryRepository>();
            _service = new CourtCategoryService(_categoryRepoMock.Object);
        }

        [Theory(DisplayName = "UTCID01 - Should return 404 for empty data or no search results")]
        [InlineData(true)]   // Empty database
        [InlineData(false)]  // Search finds nothing
        public async Task UTCID01_EmptyDataOrNoSearchResults_Returns404(bool isEmptyDatabase)
        {
            // Covers: Empty data + Search filtering with no results
            if (isEmptyDatabase)
            {
                _categoryRepoMock.Setup(x => x.GetAllCourtCategoriesAsync()).ReturnsAsync((List<CourtCategory>)null);

                var result = await _service.GetAllCourtCategoriesAsync("", 1, 10);

                Assert.Equal(404, result.Status);
                Assert.False(result.Success);
                Assert.Equal(MessagesCodes.MSG_71, result.Message);
                Assert.Null(result.Data);
            }
            else
            {
                var categories = new List<CourtCategory>
                {
                    new CourtCategory { CategoryId = 1, CategoryName = "Tennis" }
                };
                _categoryRepoMock.Setup(x => x.GetAllCourtCategoriesAsync()).ReturnsAsync(categories);

                var result = await _service.GetAllCourtCategoriesAsync("Football", 1, 10);

                Assert.Equal(404, result.Status);
                Assert.False(result.Success);
                Assert.Equal(MessagesCodes.MSG_77, result.Message);
                Assert.Null(result.Data);
            }
        }

        [Theory(DisplayName = "UTCID02 - Should handle pagination and search correctly")]
        [InlineData("", 1, 10, 3, true)]        // No search, valid pagination
        [InlineData("ball", 1, 10, 2, true)]    // Case insensitive search
        [InlineData("", 1, 2, 3, true)]         // Pagination with pageSize=2
        [InlineData("", 0, 10, 3, false)]       // Invalid pageNumber < 1
        [InlineData("", 5, 10, 3, false)]       // Invalid pageNumber > totalPages
        public async Task UTCID02_PaginationAndSearch_ReturnsCorrectResult(string search, int pageNumber,
            int pageSize, int totalCategories, bool shouldSucceed)
        {
            // Setup test data
            var categories = new List<CourtCategory>
            {
                new CourtCategory { CategoryId = 1, CategoryName = "Tennis" },
                new CourtCategory { CategoryId = 2, CategoryName = "Basketball" },
                new CourtCategory { CategoryId = 3, CategoryName = "Football" }
            };
            _categoryRepoMock.Setup(x => x.GetAllCourtCategoriesAsync()).ReturnsAsync(categories);

            var result = await _service.GetAllCourtCategoriesAsync(search, pageNumber, pageSize);

            if (shouldSucceed)
            {
                // Covers: Success scenarios + Search + Pagination
                Assert.Equal(200, result.Status);
                Assert.True(result.Success);
                Assert.NotNull(result.Data);
                Assert.Contains(MessagesCodes.MSG_79, result.Message);

                // Verify search filtering
                if (search == "ball")
                {
                    Assert.Equal(2, result.Data.TotalItems); // Basketball + Football
                }
                else
                {
                    Assert.Equal(3, result.Data.TotalItems); // All items
                }
            }
            else
            {
                // Covers: Invalid pagination
                Assert.Equal(400, result.Status);
                Assert.False(result.Success);
                Assert.Equal(MessagesCodes.MSG_78, result.Message);
                Assert.Null(result.Data);
            }
        }

        [Fact(DisplayName = "UTCID03 - Should return 500 when repository throws exception")]
        public async Task UTCID03_RepositoryException_Returns500()
        {
            // Covers: catch (Exception ex) branch
            _categoryRepoMock.Setup(x => x.GetAllCourtCategoriesAsync())
                .ThrowsAsync(new Exception("Database connection failed"));

            var result = await _service.GetAllCourtCategoriesAsync("", 1, 10);

            Assert.Equal(500, result.Status);
            Assert.False(result.Success);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("Database connection failed", result.Message);
            Assert.Null(result.Data);
        }
    }
}