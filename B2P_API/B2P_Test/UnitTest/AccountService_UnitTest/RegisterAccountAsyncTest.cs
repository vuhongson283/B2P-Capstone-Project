using AutoMapper;
using B2P_API.DTOs.Account;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using B2P_API.Utils;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.AccountService_UnitTest
{
    public class AccountService_RegisterAccountAsync_Test
    {
        private readonly Mock<IAccountRepository> _repoMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<IConfiguration> _configMock = new();

        private AccountService CreateService()
        {
            return new AccountService(_repoMock.Object, _mapperMock.Object, _configMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Email is already exists returns 400")]
        public async Task UTCID01_EmailAlreadyExists_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                Email = "abc@email.com",
                PhoneNumber = "0123456789",
                Password = "pass",
                FullName = "A",
                IsMale = true,
                Address = "HN"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.RegisterAccountAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email đã tồn tại trong hệ thống", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Email is not real returns 400")]
        public async Task UTCID02_EmailIsNotReal_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                Email = "abc@email.com",
                PhoneNumber = "0123456789",
                Password = "pass",
                FullName = "A",
                IsMale = true,
                Address = "HN"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(false);

            var service = CreateService();

            var result = await service.RegisterAccountAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Format Email bị sai !", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Phone is already exists returns 400")]
        public async Task UTCID03_PhoneAlreadyExists_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                Email = "abc@email.com",
                PhoneNumber = "0123456789",
                Password = "pass",
                FullName = "A",
                IsMale = true,
                Address = "HN"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(true);
            _repoMock.Setup(x => x.IsPhoneExistsAsync(req.PhoneNumber)).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.RegisterAccountAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số điện thoại đã tồn tại", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Register success returns 201")]
        public async Task UTCID04_RegisterSuccess_Returns201()
        {
            var req = new RegisterAccountRequest
            {
                Email = "abc@email.com",
                PhoneNumber = "0123456789",
                Password = "pass",
                FullName = "A",
                IsMale = true,
                Address = "HN"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(true);
            _repoMock.Setup(x => x.IsPhoneExistsAsync(req.PhoneNumber)).ReturnsAsync(false);
            _repoMock.Setup(x => x.RegisterAccountAsync(It.IsAny<User>()))
                .ReturnsAsync(new User { UserId = 123 });

            var service = CreateService();

            var result = await service.RegisterAccountAsync(req);

            Assert.True(result.Success);
            Assert.Equal(201, result.Status);
            Assert.Equal("Đăng ký thành công.", result.Message);
            Assert.Equal("123", result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Exception returns 500")]
        public async Task UTCID05_Exception_Returns500()
        {
            var req = new RegisterAccountRequest
            {
                Email = "abc@email.com",
                PhoneNumber = "0123456789",
                Password = "pass",
                FullName = "A",
                IsMale = true,
                Address = "HN"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ThrowsAsync(new Exception("fail"));

            var service = CreateService();

            var result = await service.RegisterAccountAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_37, result.Message);
            Assert.Contains("fail", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID06 - Exception with InnerException returns 500 and inner message")]
        public async Task UTCID06_ExceptionWithInnerException_Returns500AndInnerMessage()
        {
            var req = new RegisterAccountRequest
            {
                Email = "abc@email.com",
                PhoneNumber = "0123456789",
                Password = "pass",
                FullName = "A",
                IsMale = true,
                Address = "HN"
            };
            var innerEx = new Exception("inner error");
            var outerEx = new Exception("outer error", innerEx);
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ThrowsAsync(outerEx);

            var service = CreateService();

            var result = await service.RegisterAccountAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_37, result.Message);
            Assert.Contains("outer error", result.Message);
            Assert.Contains("Inner: inner error", result.Message);
            Assert.Null(result.Data);
        }
    }
}