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
	public class AccountService_RegisterCourtOwnerAsync_Test
	{
		private readonly Mock<IAccountRepository> _repoMock = new();
		private readonly Mock<IImageRepository> _imageMock = new();
		private readonly Mock<IMapper> _mapperMock = new();
		private readonly Mock<IConfiguration> _configMock = new();

		private AccountService CreateService()
		{
			return new AccountService(_repoMock.Object, _imageMock.Object, _mapperMock.Object, _configMock.Object);
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
				FullName = "", // Empty
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
				Address = "" // Empty
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
				PhoneNumber = "abc123def0", // Contains letters
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
				PhoneNumber = "987654321", // 9 digits
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
				PhoneNumber = "98765432100", // 11 digits
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
				Password = "abc123", // No uppercase
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
				Password = "ABC123", // No lowercase
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
				Password = "Abcdef", // No digit
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
				Password = "Ab1", // Too short
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
				ConfirmPassword = "Password2", // Different
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

		[Fact(DisplayName = "UTC015 - All valid data returns 201")]
		public async Task UTC015_AllValidData_Returns201()
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
				.ReturnsAsync(new User { UserId = 123 });

			var service = CreateService();

			var result = await service.RegisterCourtOwnerAsync(req);

			Assert.True(result.Success);
			Assert.Equal(201, result.Status);
			Assert.Equal("Đăng ký thành công.", result.Message);
			Assert.Equal("123", result.Data);
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

		[Theory(DisplayName = "UTC017 - FullName and Address validation")]
		[InlineData(null, "Hanoi", "Tên không được để trống", false)]
		[InlineData("", "Hanoi", "Tên không được để trống", false)]
		[InlineData("   ", "Hanoi", "Tên không được để trống", false)] // Whitespace only
		[InlineData("Test User", null, "Địa chỉ không được để trống", false)]
		[InlineData("Test User", "", "Địa chỉ không được để trống", false)]
		[InlineData("Test User", "   ", "Địa chỉ không được để trống", false)] // Whitespace only
		[InlineData("Test User", "Hanoi", "Đăng ký thành công.", true)]
		public async Task UTC017_FullNameAndAddressValidation(string fullName, string address, string expectedMessage, bool shouldPass)
		{
			var req = new RegisterAccountRequest
			{
				FullName = fullName,
				Email = "test@email.com",
				PhoneNumber = "0123456789",
				Password = "Password1",
				ConfirmPassword = "Password1",
				IsMale = true,
				Address = address
			};
			_repoMock.Setup(x => x.IsEmailExistsAsync(req.Email)).ReturnsAsync(false);
			_repoMock.Setup(x => x.IsRealEmailAsync(req.Email)).ReturnsAsync(true);
			_repoMock.Setup(x => x.IsPhoneExistsAsync(req.PhoneNumber)).ReturnsAsync(false);
			_repoMock.Setup(x => x.RegisterAccountAsync(It.IsAny<User>()))
				.ReturnsAsync(new User { UserId = 123 });

			var service = CreateService();

			var result = await service.RegisterCourtOwnerAsync(req);

			if (shouldPass)
			{
				Assert.True(result.Success);
				Assert.Equal(201, result.Status);
			}
			else
			{
				Assert.False(result.Success);
				Assert.Equal(400, result.Status);
			}

			Assert.Contains(expectedMessage, result.Message);
		}
	}
}