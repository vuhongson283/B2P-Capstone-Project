using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using B2P_API.Services;
using B2P_API.DTOs.CommissionPaymentHistoryDTOs;
using B2P_API.Models;
using B2P_API.Interface;

namespace B2P_Test.UnitTest.CommissionPaymentHistoryService_UnitTest
{
    public class UpdateAsyncTest
    {
        private readonly Mock<ICommissionPaymentHistoryRepository> _repoMock;

        public UpdateAsyncTest()
        {
            _repoMock = new Mock<ICommissionPaymentHistoryRepository>();
        }

        [Fact(DisplayName = "UTCID01 - UpdateAsync returns success when entity exists")]
        public async Task UpdateAsync_ReturnsSuccess_WhenEntityExists()
        {
            // Arrange
            int id = 5;
            var entity = new CommissionPaymentHistory
            {
                Id = id,
                StatusId = 1,
                Note = "Old note"
            };
            var dto = new CommissionPaymentHistoryUpdateDto
            {
                StatusId = 2,
                Note = "  Updated note  "
            };

            _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);
            _repoMock.Setup(x => x.UpdateAsync(entity)).Returns(Task.CompletedTask);
            _repoMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var service = new CommissionPaymentHistoryService(_repoMock.Object, null);

            // Act
            var result = await service.UpdateAsync(id, dto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Cập nhật thành công", result.Message);
            Assert.NotNull(result.Data);

            // Kiểm tra object trả về đúng entity đã update
            var updated = Assert.IsType<CommissionPaymentHistory>(result.Data);
            Assert.Equal(id, updated.Id);
            Assert.Equal(dto.StatusId, updated.StatusId);
            Assert.Equal("Updated note", updated.Note); // phải trim
        }

        [Fact(DisplayName = "UTCID02 - UpdateAsync returns 404 when entity not found")]
        public async Task UpdateAsync_ReturnsNotFound_WhenEntityDoesNotExist()
        {
            // Arrange
            int id = 999;
            var dto = new CommissionPaymentHistoryUpdateDto
            {
                StatusId = 2,
                Note = "Some note"
            };

            _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((CommissionPaymentHistory)null);

            var service = new CommissionPaymentHistoryService(_repoMock.Object, null);

            // Act
            var result = await service.UpdateAsync(id, dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy bản ghi", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - UpdateAsync trims note or sets null if empty")]
        public async Task UpdateAsync_TrimNote_OrNullIfEmpty()
        {
            // Arrange
            int id = 10;
            var entity = new CommissionPaymentHistory
            {
                Id = id,
                StatusId = 1,
                Note = "Old note"
            };

            // Case 1: Dto with only spaces for Note => Note phải null
            var dtoEmpty = new CommissionPaymentHistoryUpdateDto
            {
                StatusId = 3,
                Note = "   "
            };

            _repoMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(entity);
            _repoMock.Setup(x => x.UpdateAsync(entity)).Returns(Task.CompletedTask);
            _repoMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var service = new CommissionPaymentHistoryService(_repoMock.Object, null);

            // Act
            var result = await service.UpdateAsync(id, dtoEmpty);

            // Assert
            Assert.True(result.Success);
            Assert.Null(((CommissionPaymentHistory)result.Data).Note);
        }
    }
}