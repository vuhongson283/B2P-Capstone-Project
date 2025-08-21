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
    public class CreateAsyncTest
    {
        private readonly Mock<ICommissionPaymentHistoryRepository> _repoMock;
        private readonly Mock<IAccountManagementRepository> _accRepoMock;

        public CreateAsyncTest()
        {
            _repoMock = new Mock<ICommissionPaymentHistoryRepository>();
            _accRepoMock = new Mock<IAccountManagementRepository>();
        }

        [Fact(DisplayName = "UTCID01 - CreateAsync returns success with valid data")]
        public async Task CreateAsync_ReturnsSuccess_WithValidData()
        {
            // Arrange
            var userId = 10;
            var dto = new CommissionPaymentHistoryCreateDto
            {
                UserId = userId,
                Month = 8,
                Year = 2025,
                Amount = 1000000,
                StatusId = 1,
                Note = "Ghi chú"
            };

            // SỬA: sử dụng User thay vì AccountManagement, property là UserId
            _accRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(new User { UserId = userId });
            _repoMock.Setup(x => x.AddAsync(It.IsAny<CommissionPaymentHistory>())).Callback<CommissionPaymentHistory>(e => e.Id = 111).Returns(Task.CompletedTask);
            _repoMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var service = new CommissionPaymentHistoryService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(201, result.Status);
            Assert.Equal("Tạo mới thành công", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(111, result.Data.Id);
            Assert.Equal(userId, result.Data.UserId);
            Assert.Equal(dto.Month, result.Data.Month);
            Assert.Equal(dto.Year, result.Data.Year);
            Assert.Equal(dto.Amount, result.Data.Amount);
            Assert.Equal(dto.StatusId, result.Data.StatusId);
            Assert.Equal(dto.Note, result.Data.Note);
            Assert.Null(result.Data.PaidAt);
        }

        [Fact(DisplayName = "UTCID02 - CreateAsync returns error if user not found")]
        public async Task CreateAsync_ReturnsError_WhenUserNotFound()
        {
            // Arrange
            var dto = new CommissionPaymentHistoryCreateDto
            {
                UserId = 999,
                Month = 8,
                Year = 2025,
                Amount = 1000000,
                StatusId = 1,
                Note = null
            };

            // SỬA: trả về null User
            _accRepoMock.Setup(x => x.GetByIdAsync(dto.UserId)).ReturnsAsync((User)null);

            var service = new CommissionPaymentHistoryService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("UserId không hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        [Theory(DisplayName = "UTCID03 - CreateAsync returns error if month is invalid")]
        [InlineData(0)]
        [InlineData(13)]
        public async Task CreateAsync_ReturnsError_WhenMonthIsInvalid(int month)
        {
            // Arrange
            var dto = new CommissionPaymentHistoryCreateDto
            {
                UserId = 1,
                Month = month,
                Year = 2025,
                Amount = 1000000,
                StatusId = 1,
                Note = null
            };

            // SỬA: trả về User với UserId
            _accRepoMock.Setup(x => x.GetByIdAsync(dto.UserId)).ReturnsAsync(new User { UserId = dto.UserId });

            var service = new CommissionPaymentHistoryService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tháng phải từ 1 đến 12", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - CreateAsync returns error if year < 2020")]
        public async Task CreateAsync_ReturnsError_WhenYearInvalid()
        {
            // Arrange
            var dto = new CommissionPaymentHistoryCreateDto
            {
                UserId = 1,
                Month = 8,
                Year = 2019,
                Amount = 1000000,
                StatusId = 1,
                Note = null
            };

            _accRepoMock.Setup(x => x.GetByIdAsync(dto.UserId)).ReturnsAsync(new User { UserId = dto.UserId });

            var service = new CommissionPaymentHistoryService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Năm không hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        [Theory(DisplayName = "UTCID05 - CreateAsync returns error if amount <= 0")]
        [InlineData(0)]
        [InlineData(-500)]
        public async Task CreateAsync_ReturnsError_WhenAmountNonPositive(decimal amount)
        {
            // Arrange
            var dto = new CommissionPaymentHistoryCreateDto
            {
                UserId = 1,
                Month = 8,
                Year = 2025,
                Amount = amount,
                StatusId = 1,
                Note = null
            };

            _accRepoMock.Setup(x => x.GetByIdAsync(dto.UserId)).ReturnsAsync(new User { UserId = dto.UserId });

            var service = new CommissionPaymentHistoryService(_repoMock.Object, _accRepoMock.Object);

            // Act
            var result = await service.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số tiền phải lớn hơn 0", result.Message);
            Assert.Null(result.Data);
        }
    }
}