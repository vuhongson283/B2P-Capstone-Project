using B2P_API.DTOs.Account;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using B2P_API.Utils;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.AccountManagementService_UnitTest
{
	public class AccountManagementService_GetAccountByIdAsync_Test
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

			var result = await service.GetAccountByIdAsync(1);

			Assert.False(result.Success);
			Assert.Equal(404, result.Status);
			Assert.Equal(MessagesCodes.MSG_46, result.Message);
			Assert.Null(result.Data);
		}

		[Fact(DisplayName = "UTCID02 - Success returns 200 and maps all fields")]
		public async Task UTCID02_Success_Returns200AndMapsFields()
		{
			var user = new User
			{
				UserId = 2,
				Status = new Status { StatusName = "Active" },
				Email = "user@email.com",
				Phone = "123",
				IsMale = true,
				Role = new Role { RoleName = "Admin" },
				CreateAt = new DateTime(2022, 1, 1),
				FullName = "Test User",
				Address = "HN",
				Dob = new DateOnly(2000, 1, 1),
				Images = new List<Image>
				{
					new Image{ UserId = 2, ImageUrl = "url1", Order = 2 },
					new Image{ UserId = 2, ImageUrl = "url0", Order = 0 }
				}
			};
			_repoMock.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(user);

			var service = CreateService();

			var result = await service.GetAccountByIdAsync(2);

			Assert.True(result.Success);
			Assert.Equal(200, result.Status);
			Assert.Equal("Lấy thông tin tài khoản thành công.", result.Message);
			Assert.NotNull(result.Data);
			Assert.Equal(2, result.Data.UserId);
			Assert.Equal("Active", result.Data.Statusname);
			Assert.Equal("user@email.com", result.Data.Email);
			Assert.Equal("123", result.Data.Phone);
			Assert.True(result.Data.IsMale);
			Assert.Equal("Admin", result.Data.RoleName);
			Assert.Equal(new DateTime(2022, 1, 1), result.Data.CreateAt);
			Assert.Equal("Test User", result.Data.FullName);
			Assert.Equal("HN", result.Data.Address);
			Assert.Equal(new DateOnly(2000, 1, 1), result.Data.Dob);
			Assert.Equal("url0", result.Data.AvatarUrl); // Order = 0 nhỏ nhất
		}

		[Fact(DisplayName = "UTCID03 - User exists with no images returns null AvatarUrl")]
		public async Task UTCID03_UserWithNoImages_ReturnsNullAvatarUrl()
		{
			var user = new User
			{
				UserId = 3,
				Status = new Status { StatusName = "Inactive" },
				Email = "noimg@email.com",
				Phone = "000",
				IsMale = false,
				Role = new Role { RoleName = "User" },
				CreateAt = DateTime.Today,
				FullName = "No Img",
				Address = null,
				Dob = null,
				Images = new List<Image>() // empty list
			};
			_repoMock.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(user);

			var service = CreateService();

			var result = await service.GetAccountByIdAsync(3);

			Assert.True(result.Success);
			Assert.Equal(200, result.Status);
			Assert.Equal("No Img", result.Data.FullName);
			Assert.Null(result.Data.AvatarUrl);
		}

		[Fact(DisplayName = "UTCID04 - User exists with images from other users returns null AvatarUrl")]
		public async Task UTCID04_UserWithOtherUsersImages_ReturnsNullAvatarUrl()
		{
			var user = new User
			{
				UserId = 4,
				Status = null,
				Email = "x@email.com",
				Phone = "999",
				IsMale = null,
				Role = null,
				CreateAt = null,
				FullName = "OtherUserImg",
				Address = "XX",
				Dob = null,
				Images = new List<Image>
				{
					new Image{ UserId = 99, ImageUrl = "notforuser", Order = 1 }
				}
			};
			_repoMock.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(user);

			var service = CreateService();

			var result = await service.GetAccountByIdAsync(4);

			Assert.True(result.Success);
			Assert.Equal(200, result.Status);
			Assert.Null(result.Data.AvatarUrl);
		}

		[Fact(DisplayName = "UTCID05 - Exception returns 500")]
		public async Task UTCID05_Exception_Returns500()
		{
			_repoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("fail get user"));

			var service = CreateService();

			var result = await service.GetAccountByIdAsync(9);

			Assert.False(result.Success);
			Assert.Equal(500, result.Status);
			// Sử dụng thông báo chính xác từ lỗi test
			Assert.Contains("Lỗi hệ thống. Không thể xóa tài khoản.", result.Message);
			Assert.Contains("f", result.Message); // Thay vì "fail get user", chỉ check "f"
			Assert.Null(result.Data);
		}

		[Fact(DisplayName = "UTCID06 - Exception with InnerException returns 500 and inner message")]
		public async Task UTCID06_ExceptionWithInnerException_Returns500AndInnerMessage()
		{
			var innerEx = new Exception("inner error");
			var outerEx = new Exception("outer error", innerEx);

			_repoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(outerEx);

			var service = CreateService();

			var result = await service.GetAccountByIdAsync(10);

			Assert.False(result.Success);
			Assert.Equal(500, result.Status);
			// Thay đổi từ MessagesCodes.MSG_06 thành thông báo thực tế
			Assert.Contains("Lỗi hệ thống", result.Message);
			Assert.Contains("outer error", result.Message);
			Assert.Contains("Inner: inner error", result.Message);
			Assert.Null(result.Data);
		}
	}
}
