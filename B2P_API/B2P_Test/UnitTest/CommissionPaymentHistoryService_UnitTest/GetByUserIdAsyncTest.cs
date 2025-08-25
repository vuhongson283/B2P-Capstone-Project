using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using B2P_API.Services;
using B2P_API.DTOs.CommissionPaymentHistoryDTOs;
using B2P_API.Models;
using B2P_API.Interface;

namespace B2P_Test.UnitTest.CommissionPaymentHistoryService_UnitTest
{
    public class GetByUserIdAsyncTest
    {
        private readonly Mock<ICommissionPaymentHistoryRepository> _repoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;

        public GetByUserIdAsyncTest()
        {
            _repoMock = new Mock<ICommissionPaymentHistoryRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
        }

        [Fact(DisplayName = "UTCID01 - GetByUserIdAsync returns data when user exists")]
        public async Task GetByUserIdAsync_ReturnsData_WhenUserExists()
        {
            // Arrange
            int userId = 5;
            var user = new User { UserId = userId };

            _accRepoMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            var entities = new List<CommissionPaymentHistory>
            {
                new CommissionPaymentHistory
                {
                    Id = 1,
                    UserId = userId,
                    Month = 7,
                    Year = 2024,
                    Amount = 123000,
                    PaidAt = new DateTime(2024, 7, 10),
                    StatusId = 1,
                    Note = "Đã trả"
                },
                new CommissionPaymentHistory
                {
                    Id = 2,
                    UserId = userId,
                    Month = 8,
                    Year = 2024,
                    Amount = 456000,
                    PaidAt = null,
                    StatusId = 2,
                    Note = null
                }
            };

            _repoMock.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(entities);

            var service = new CommissionPaymentHistoryService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.GetByUserIdAsync(userId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy dữ liệu theo UserId thành công", result.Message);
            Assert.NotNull(result.Data);

            var list = result.Data.Cast<CommissionPaymentHistoryDto>().ToList();
            Assert.Equal(2, list.Count);

            Assert.Equal(1, list[0].Id);
            Assert.Equal(userId, list[0].UserId);
            Assert.Equal(7, list[0].Month);
            Assert.Equal(2024, list[0].Year);
            Assert.Equal(123000, list[0].Amount);
            Assert.Equal(new DateTime(2024, 7, 10), list[0].PaidAt);
            Assert.Equal(1, list[0].StatusId);
            Assert.Equal("Đã trả", list[0].Note);

            Assert.Equal(2, list[1].Id);
            Assert.Equal(userId, list[1].UserId);
            Assert.Equal(8, list[1].Month);
            Assert.Equal(2024, list[1].Year);
            Assert.Equal(456000, list[1].Amount);
            Assert.Null(list[1].PaidAt);
            Assert.Equal(2, list[1].StatusId);
            Assert.Null(list[1].Note);
        }

        [Fact(DisplayName = "UTCID02 - GetByUserIdAsync returns 404 if user not found")]
        public async Task GetByUserIdAsync_ReturnsNotFound_WhenUserNotFound()
        {
            // Arrange
            int userId = 999;
            _accRepoMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User)null);

            var service = new CommissionPaymentHistoryService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.GetByUserIdAsync(userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Người dùng không tồn tại", result.Message);
            Assert.Null(result.Data);
        }

    }
}