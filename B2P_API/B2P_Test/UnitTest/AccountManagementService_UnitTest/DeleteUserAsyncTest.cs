using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using B2P_API.Utils;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.AccountManagementService_UnitTest
{
    public class AccountManagementService_DeleteUserAsync_Test
    {
        private readonly Mock<IAccountManagementRepository> _repoMock = new();

        private AccountManagementService CreateService()
        {
            return new AccountManagementService(_repoMock.Object, null);
        }

        [Fact(DisplayName = "UTCID01 - User not found returns 404")]
        public async Task UTCID01_UserNotFound_Returns404()
        {
            _repoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((User)null);

            var service = CreateService();

            var result = await service.DeleteUserAsync(1);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal(MessagesCodes.MSG_46, result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - User not banned returns 400")]
        public async Task UTCID02_UserNotBanned_Returns400()
        {
            var user = new User
            {
                UserId = 2,
                StatusId = 1 // Không bị banned
            };
            _repoMock.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(user);

            var service = CreateService();

            var result = await service.DeleteUserAsync(2);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Chỉ được xóa tài khoản đang ở trạng thái banned.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Delete failed returns 500")]
        public async Task UTCID03_DeleteFailed_Returns500()
        {
            var user = new User
            {
                UserId = 3,
                StatusId = 4 // Banned
            };
            _repoMock.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(user);
            _repoMock.Setup(x => x.DeleteUserAsync(user)).ReturnsAsync(false);

            var service = CreateService();

            var result = await service.DeleteUserAsync(3);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal(MessagesCodes.MSG_50, result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Delete success returns 200")]
        public async Task UTCID04_DeleteSuccess_Returns200()
        {
            var user = new User
            {
                UserId = 4,
                StatusId = 4 // Banned
            };
            _repoMock.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(user);
            _repoMock.Setup(x => x.DeleteUserAsync(user)).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.DeleteUserAsync(4);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal(MessagesCodes.MSG_48, result.Message);
            Assert.Equal("4", result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Exception returns 500")]
        public async Task UTCID05_Exception_Returns500()
        {
            _repoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("fail"));

            var service = CreateService();

            var result = await service.DeleteUserAsync(99);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_50, result.Message);
            Assert.Contains("fail", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID06 - Exception with InnerException returns 500 and inner message")]
        public async Task UTCID06_ExceptionWithInnerException_Returns500AndInnerMessage()
        {
            var innerEx = new Exception("inner error");
            var outerEx = new Exception("outer error", innerEx);

            _repoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(outerEx);

            var service = CreateService();

            var result = await service.DeleteUserAsync(888);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_50, result.Message);
            Assert.Contains("outer error", result.Message);
            Assert.Contains("Inner: inner error", result.Message);
            Assert.Null(result.Data);
        }
    }
}