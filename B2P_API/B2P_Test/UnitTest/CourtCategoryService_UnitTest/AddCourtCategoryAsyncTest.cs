using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Services;
using B2P_API.Utils;
using Moq;

namespace B2P_Test.UnitTest.CourtCategoryService_UnitTest
{
    public class AddCourtCategoryAsyncTest
    {
        private readonly Mock<ICourtCategoryRepository> _categoryRepoMock;
        private readonly CourtCategoryService _service;

        public AddCourtCategoryAsyncTest()
        {
            _categoryRepoMock = new Mock<ICourtCategoryRepository>();
            _service = new CourtCategoryService(_categoryRepoMock.Object);
        }

        [Theory(DisplayName = "UTCID01 - Should return 400 for null/empty cateName")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n\r")]
        public async Task UTCID01_NullOrEmptyCateName_Returns400(string cateName)
        {
            // Covers: string.IsNullOrEmpty(cateName?.Trim()) branch
            var result = await _service.AddCourtCategoryAsync(cateName);

            Assert.Equal(400, result.Status);
            Assert.False(result.Success);
            Assert.Equal(MessagesCodes.MSG_74, result.Message);
            _categoryRepoMock.Verify(x => x.AddCourtCategoryAsync(It.IsAny<CourtCategory>()), Times.Never);
        }

        [Fact(DisplayName = "UTCID02 - Should return 400 when cateName exceeds 100 characters")]
        public async Task UTCID02_CateNameTooLong_Returns400()
        {
            // Covers: if (cateName.Trim().Length > 100) branch
            var longName = new string('A', 101);

            var result = await _service.AddCourtCategoryAsync(longName);

            Assert.Equal(400, result.Status);
            Assert.False(result.Success);
            Assert.Equal(MessagesCodes.MSG_76, result.Message);
            _categoryRepoMock.Verify(x => x.AddCourtCategoryAsync(It.IsAny<CourtCategory>()), Times.Never);
        }

        [Theory(DisplayName = "UTCID03 - Should return 200 and trim cateName")]
        [InlineData("Tennis Court", "Tennis Court")]
        [InlineData("  Tennis Court  ", "Tennis Court")]
        [InlineData("A", "A")]
        [InlineData("\tBasketball\n", "Basketball")]
        public async Task UTCID03_ValidCateName_Returns200(string input, string expectedTrimmed)
        {
            // Covers: Success path + Trim functionality + Object creation + Repository call
            _categoryRepoMock.Setup(x => x.AddCourtCategoryAsync(It.IsAny<CourtCategory>())).ReturnsAsync(true);

            var result = await _service.AddCourtCategoryAsync(input);

            Assert.Equal(200, result.Status);
            Assert.True(result.Success);
            Assert.Equal(MessagesCodes.MSG_75, result.Message);

            // Verify trimming works correctly
            _categoryRepoMock.Verify(x => x.AddCourtCategoryAsync(It.Is<CourtCategory>(c =>
                c.CategoryName == expectedTrimmed)), Times.Once);
        }

        [Fact(DisplayName = "UTCID04 - Should accept exactly 100 characters")]
        public async Task UTCID04_Exactly100Characters_Returns200()
        {
            // Covers: Boundary case - exactly at the 100 character limit
            var exactName = new string('A', 100);
            _categoryRepoMock.Setup(x => x.AddCourtCategoryAsync(It.IsAny<CourtCategory>())).ReturnsAsync(true);

            var result = await _service.AddCourtCategoryAsync(exactName);

            Assert.Equal(200, result.Status);
            Assert.True(result.Success);
            Assert.Equal(MessagesCodes.MSG_75, result.Message);

            // Verify exactly 100 characters
            _categoryRepoMock.Verify(x => x.AddCourtCategoryAsync(It.Is<CourtCategory>(c =>
                c.CategoryName.Length == 100 && c.CategoryName == exactName)), Times.Once);
        }

        [Fact(DisplayName = "UTCID05 - Should return 500 when repository throws exception")]
        public async Task UTCID05_RepositoryException_Returns500()
        {
            // Covers: catch (Exception ex) branch
            _categoryRepoMock.Setup(x => x.AddCourtCategoryAsync(It.IsAny<CourtCategory>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            var result = await _service.AddCourtCategoryAsync("Test Court");

            Assert.Equal(500, result.Status);
            Assert.False(result.Success);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("Database connection failed", result.Message);
        }
    }
}