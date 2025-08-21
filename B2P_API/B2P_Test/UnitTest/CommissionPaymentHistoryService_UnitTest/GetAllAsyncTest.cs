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
    public class GetAllAsyncTest
    {
        private readonly Mock<ICommissionPaymentHistoryRepository> _repoMock;

        public GetAllAsyncTest()
        {
            _repoMock = new Mock<ICommissionPaymentHistoryRepository>();
        }

        [Fact(DisplayName = "UTCID01 - GetAllAsync returns CommissionPaymentHistoryDto list")]
        public async Task GetAllAsync_ReturnsListOfCommissionPaymentHistoryDto()
        {
            // Arrange
            var entities = new List<CommissionPaymentHistory>
            {
                new CommissionPaymentHistory
                {
                    Id = 1,
                    UserId = 10,
                    Month = 7,
                    Year = 2024,
                    Amount = 500000,
                    PaidAt = new DateTime(2024, 7, 31),
                    StatusId = 1,
                    Note = "Đã thanh toán"
                },
                new CommissionPaymentHistory
                {
                    Id = 2,
                    UserId = 11,
                    Month = 8,
                    Year = 2025,
                    Amount = 750000,
                    PaidAt = null,
                    StatusId = 2,
                    Note = "Chưa thanh toán"
                }
            };

            _repoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(entities);

            var service = new CommissionPaymentHistoryService(_repoMock.Object, null);

            // Act
            var result = await service.GetAllAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy danh sách thành công", result.Message);
            Assert.NotNull(result.Data);

            var list = result.Data.ToList();
            Assert.Equal(2, list.Count);

            Assert.Equal(1, list[0].Id);
            Assert.Equal(10, list[0].UserId);
            Assert.Equal(7, list[0].Month);
            Assert.Equal(2024, list[0].Year);
            Assert.Equal(500000, list[0].Amount);
            Assert.Equal(new DateTime(2024, 7, 31), list[0].PaidAt);
            Assert.Equal(1, list[0].StatusId);
            Assert.Equal("Đã thanh toán", list[0].Note);

            Assert.Equal(2, list[1].Id);
            Assert.Equal(11, list[1].UserId);
            Assert.Equal(8, list[1].Month);
            Assert.Equal(2025, list[1].Year);
            Assert.Equal(750000, list[1].Amount);
            Assert.Null(list[1].PaidAt);
            Assert.Equal(2, list[1].StatusId);
            Assert.Equal("Chưa thanh toán", list[1].Note);
        }

        [Fact(DisplayName = "UTCID02 - GetAllAsync returns empty list if no data")]
        public async Task GetAllAsync_ReturnsEmptyList_IfNoData()
        {
            // Arrange
            _repoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<CommissionPaymentHistory>());

            var service = new CommissionPaymentHistoryService(_repoMock.Object, null);

            // Act
            var result = await service.GetAllAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Lấy danh sách thành công", result.Message);
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
        }
    }
}