using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using B2P_API.Utils;
using AutoMapper;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

// Fake repo để pass constructor

namespace B2P_Test.UnitTest.AccountManagementService_UnitTest
{
    public class AccountManagementService_UnBanUserAsync_Test
    {
        private readonly Mock<IAccountManagementRepository> _repoMock = new();
        private readonly Mock<IMapper> _mapperMock = new();

        private AccountManagementService CreateService(FacilityService facilityService = null)
        {
            return new AccountManagementService(_repoMock.Object, facilityService, _mapperMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - User not found returns 404")]
        public async Task UTCID01_UserNotFound_Returns404()
        {
            _repoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((User)null);

            var service = CreateService(null);

            var result = await service.UnBanUserAsync(1);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal(MessagesCodes.MSG_46, result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - User already active returns 400")]
        public async Task UTCID02_UserAlreadyActive_Returns400()
        {
            var user = new User
            {
                UserId = 2,
                StatusId = 1 // Đã hoạt động
            };
            _repoMock.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(user);

            var service = CreateService(null);

            var result = await service.UnBanUserAsync(2);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tài khoản này đã được hoạt động rồi, không thể gỡ cấm.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - UnBan user success returns 200")]
        public async Task UTCID03_UnBanUserSuccess_Returns200()
        {
            var user = new User
            {
                UserId = 3,
                StatusId = 4 // Đang bị cấm
            };
            _repoMock.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(user);
            _repoMock.Setup(x => x.UpdateStatusAsync(user, 1)).ReturnsAsync(true);

            var facilityService = new FacilityServiceFake(true);

            var service = CreateService(facilityService);

            var result = await service.UnBanUserAsync(3);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Mở khoá tài khoản thành công.", result.Message);
            Assert.Equal("3", result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Exception returns 500")]
        public async Task UTCID04_Exception_Returns500()
        {
            _repoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("fail"));

            var service = CreateService(null);

            var result = await service.UnBanUserAsync(99);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_37, result.Message);
            Assert.Contains("fail", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Exception with InnerException returns 500 and inner message")]
        public async Task UTCID05_ExceptionWithInnerException_Returns500AndInnerMessage()
        {
            var innerEx = new Exception("inner error");
            var outerEx = new Exception("outer error", innerEx);

            _repoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(outerEx);

            var service = CreateService(null);

            var result = await service.UnBanUserAsync(888);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_37, result.Message);
            Assert.Contains("outer error", result.Message);
            Assert.Contains("Inner: inner error", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID06 - Exception with InnerException")]
        public async Task UTCID06_ExceptionWithInnerException_Returns500AndInnerMessage()
        {
            var innerEx = new Exception("inner error");
            var outerEx = new Exception("outer error", innerEx);

            _repoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(outerEx);

            var service = CreateService(null);

            var result = await service.UnBanUserAsync(888);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_37, result.Message);
            Assert.Contains("outer error", result.Message);
            Assert.Contains("Inner: inner error", result.Message);
            Assert.Null(result.Data);
        }
    }
}