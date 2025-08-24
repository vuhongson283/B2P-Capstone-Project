using AutoMapper;
using B2P_API.DTOs.Account;
using B2P_API.DTOs.AuthDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using B2P_API.Utils;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.AccountService_UnitTest
{
    public class AccountService_RegisterCourtOwnerAsync_Test
    {
        private readonly Mock<IAccountRepository> _repoMock = new();
        private readonly Mock<IImageRepository> _imageMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<IAuthRepository> _authRepoMock = new();

        // Helper để tạo JWTHelper thật với config giả
        private JWTHelper CreateRealJwtHelper()
        {
            var inMemorySettings = new Dictionary<string, string> {
                {"JWT:AccessSecret", "supersecretkeyyoushouldchange1234"},
                {"JWT:Issuer", "issuer"},
                {"JWT:Audience", "audience"}
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            return new JWTHelper(configuration);
        }

        private AccountService CreateService()
        {
            return new AccountService(
                _repoMock.Object,
                _imageMock.Object,
                _mapperMock.Object,
                _configMock.Object,
                CreateRealJwtHelper(),
                _authRepoMock.Object
            );
        }

        [Fact(DisplayName = "UTC001 - Request null returns 400")]
        public async Task UTC001_RequestNull_Returns400()
        {
            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(null);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Request không hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTC002 - FullName null or empty returns 400")]
        public async Task UTC002_FullNameNullOrEmpty_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "",
                Email = "test@email.com",
                PhoneNumber = "0123456789",
                Password = "Password1",
                ConfirmPassword = "Password1",
                IsMale = true,
                Address = "Hanoi"
            };

            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tên không được để trống", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTC003 - Address null or empty returns 400")]
        public async Task UTC003_AddressNullOrEmpty_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "Test User",
                Email = "test@email.com",
                PhoneNumber = "0123456789",
                Password = "Password1",
                ConfirmPassword = "Password1",
                IsMale = true,
                Address = ""
            };

            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Địa chỉ không được để trống", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTC004 - Email already exists returns 400")]
        public async Task UTC004_EmailAlreadyExists_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "Test User",
                Email = "existing@email.com",
                PhoneNumber = "0123456789",
                Password = "Password1",
                ConfirmPassword = "Password1",
                IsMale = true,
                Address = "Hanoi"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Email đã tồn tại trong hệ thống", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTC005 - Email format invalid returns 400")]
        public async Task UTC005_EmailFormatInvalid_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "Test User",
                Email = "invalid@email.com",
                PhoneNumber = "0123456789",
                Password = "Password1",
                ConfirmPassword = "Password1",
                IsMale = true,
                Address = "Hanoi"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(false);

            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Format Email bị sai !", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTC006 - Phone has letters returns 400")]
        public async Task UTC006_PhoneHasLetters_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "Test User",
                Email = "test@email.com",
                PhoneNumber = "abc123def0",
                Password = "Password1",
                ConfirmPassword = "Password1",
                IsMale = true,
                Address = "Hanoi"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Format số điện thoại không phù hợp", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTC007 - Phone length less than 10 returns 400")]
        public async Task UTC007_PhoneLengthLessThan10_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "Test User",
                Email = "test@email.com",
                PhoneNumber = "987654321",
                Password = "Password1",
                ConfirmPassword = "Password1",
                IsMale = true,
                Address = "Hanoi"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Format số điện thoại không phù hợp", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTC008 - Phone length greater than 10 returns 400")]
        public async Task UTC008_PhoneLengthGreaterThan10_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "Test User",
                Email = "test@email.com",
                PhoneNumber = "98765432100",
                Password = "Password1",
                ConfirmPassword = "Password1",
                IsMale = true,
                Address = "Hanoi"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Format số điện thoại không phù hợp", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTC009 - Phone already exists returns 400")]
        public async Task UTC009_PhoneAlreadyExists_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "Test User",
                Email = "test@email.com",
                PhoneNumber = "0123456789",
                Password = "Password1",
                ConfirmPassword = "Password1",
                IsMale = true,
                Address = "Hanoi"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(true);
            _repoMock.Setup(x => x.IsPhoneExistsAsync(req.PhoneNumber)).ReturnsAsync(true);

            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số điện thoại đã tồn tại", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTC010 - Password missing uppercase returns 400")]
        public async Task UTC010_PasswordMissingUppercase_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "Test User",
                Email = "test@email.com",
                PhoneNumber = "0123456789",
                Password = "abc123",
                ConfirmPassword = "abc123",
                IsMale = true,
                Address = "Hanoi"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(true);
            _repoMock.Setup(x => x.IsPhoneExistsAsync(req.PhoneNumber)).ReturnsAsync(false);

            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số, tối thiểu 6 ký tự", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTC011 - Password missing lowercase returns 400")]
        public async Task UTC011_PasswordMissingLowercase_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "Test User",
                Email = "test@email.com",
                PhoneNumber = "0123456789",
                Password = "ABC123",
                ConfirmPassword = "ABC123",
                IsMale = true,
                Address = "Hanoi"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(true);
            _repoMock.Setup(x => x.IsPhoneExistsAsync(req.PhoneNumber)).ReturnsAsync(false);

            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số, tối thiểu 6 ký tự", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTC012 - Password missing digit returns 400")]
        public async Task UTC012_PasswordMissingDigit_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "Test User",
                Email = "test@email.com",
                PhoneNumber = "0123456789",
                Password = "Abcdef",
                ConfirmPassword = "Abcdef",
                IsMale = true,
                Address = "Hanoi"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(true);
            _repoMock.Setup(x => x.IsPhoneExistsAsync(req.PhoneNumber)).ReturnsAsync(false);

            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số, tối thiểu 6 ký tự", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTC013 - Password too short returns 400")]
        public async Task UTC013_PasswordTooShort_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "Test User",
                Email = "test@email.com",
                PhoneNumber = "0123456789",
                Password = "Ab1",
                ConfirmPassword = "Ab1",
                IsMale = true,
                Address = "Hanoi"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(true);
            _repoMock.Setup(x => x.IsPhoneExistsAsync(req.PhoneNumber)).ReturnsAsync(false);

            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số, tối thiểu 6 ký tự", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTC014 - Password and ConfirmPassword not match returns 400")]
        public async Task UTC014_PasswordConfirmPasswordNotMatch_Returns400()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "Test User",
                Email = "test@email.com",
                PhoneNumber = "0123456789",
                Password = "Password1",
                ConfirmPassword = "Password2",
                IsMale = true,
                Address = "Hanoi"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(true);
            _repoMock.Setup(x => x.IsPhoneExistsAsync(req.PhoneNumber)).ReturnsAsync(false);

            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Password và ConfirmPassword không khớp", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTC015 - All valid data returns 200")]
        public async Task UTC015_AllValidData_Returns200()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "Test User",
                Email = "test@email.com",
                PhoneNumber = "0123456789",
                Password = "Password1",
                ConfirmPassword = "Password1",
                IsMale = true,
                Address = "Hanoi"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
            _repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(true);
            _repoMock.Setup(x => x.IsPhoneExistsAsync(req.PhoneNumber)).ReturnsAsync(false);
            _repoMock.Setup(x => x.RegisterAccountAsync(It.IsAny<User>()))
                .ReturnsAsync(new User
                {
                    UserId = 123,
                    RoleId = 3,
                    Role = new Role { RoleName = "CourtOwner" },
                    Phone = req.PhoneNumber,
                    Email = req.Email,
                    FullName = req.FullName,
                    IsMale = req.IsMale,
                    CreateAt = DateTime.UtcNow,
                    Address = req.Address
                });

            _imageMock.Setup(x => x.CreateUserDefaultImageAsync(It.IsAny<int>()))
                .ReturnsAsync(new Image { ImageId = 1, UserId = 123 });
            _authRepoMock.Setup(x => x.SaveUserTokenAsync(It.IsAny<UserToken>())).Returns(Task.CompletedTask);

            var inMemorySettings = new Dictionary<string, string> {
        {"JWT:AccessSecret", "supersecretkeyyoushouldchangemeplz1234"},
        {"JWT:Issuer", "issuer"},
        {"JWT:Audience", "audience"}
    };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            var jwtHelper = new JWTHelper(configuration);

            var service = new AccountService(
                _repoMock.Object,
                _imageMock.Object,
                _mapperMock.Object,
                _configMock.Object,
                jwtHelper,
                _authRepoMock.Object
            );

            var result = await service.RegisterCourtOwnerAsync(req);

            if (!result.Success)
                Console.WriteLine($"Fail message: {result.Message}");

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Đăng ký thành công và đã đăng nhập.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(123, result.Data.User.UserId);
        }

        [Fact(DisplayName = "UTC016 - Exception returns 500")]
        public async Task UTC016_Exception_Returns500()
        {
            var req = new RegisterAccountRequest
            {
                FullName = "Test User",
                Email = "test@email.com",
                PhoneNumber = "0123456789",
                Password = "Password1",
                ConfirmPassword = "Password1",
                IsMale = true,
                Address = "Hanoi"
            };
            _repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ThrowsAsync(new Exception("Database error"));

            var service = CreateService();

            var result = await service.RegisterCourtOwnerAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_37, result.Message);
            Assert.Contains("Database error", result.Message);
            Assert.Null(result.Data);
        }
    }
}