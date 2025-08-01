using AutoMapper;
using B2P_API.DTOs.Account;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using B2P_API.Utils;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.AccountManagementService_UnitTest
{
    public class AccountManagementService_GetAllAccountsAsync_Test
    {
        private readonly Mock<IAccountManagementRepository> _repoMock = new();
        private readonly Mock<IMapper> _mapperMock = new();

        private AccountManagementService CreateService()
        {
            return new AccountManagementService(_repoMock.Object, _mapperMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Null request returns 400")]
        public async Task UTCID01_NullRequest_Returns400()
        {
            var service = CreateService();

            var result = await service.GetAllAccountsAsync(null);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Request bị null.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Repository returns null returns 500")]
        public async Task UTCID02_RepositoryReturnsNull_Returns500()
        {
            var req = new GetListAccountRequest { PageNumber = 1, PageSize = 10 };
            _repoMock.Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync((List<User>)null);

            var service = CreateService();

            var result = await service.GetAllAccountsAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Không thể tải danh sách tài khoản. Repository trả về null.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Users list contains null element returns 500")]
        public async Task UTCID03_UsersListContainsNull_Returns500()
        {
            var req = new GetListAccountRequest { PageNumber = 1, PageSize = 10 };
            var users = new List<User> { new User(), null };
            _repoMock.Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(users);

            var service = CreateService();

            var result = await service.GetAllAccountsAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Equal("Có phần tử null trong danh sách users.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Mapped result is empty returns 404")]
        public async Task UTCID04_MappedResultEmpty_Returns404()
        {
            var req = new GetListAccountRequest { PageNumber = 1, PageSize = 10 };
            var users = new List<User>();
            _repoMock.Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(users);
            _repoMock.Setup(x => x.GetTotalAccountsAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(0);

            var service = CreateService();

            var result = await service.GetAllAccountsAsync(req);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không có tài khoản nào khớp với tiêu chí tìm kiếm của bạn.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Success returns 200")]
        public async Task UTCID05_Success_Returns200()
        {
            var req = new GetListAccountRequest
            {
                PageNumber = 2,
                PageSize = 5,
                Search = "abc",
                RoleId = 1,
                StatusId = 2
            };
            var users = new List<User>
            {
                new User
                {
                    UserId = 10,
                    FullName = "Test User",
                    Email = "test@email.com",
                    Phone = "123",
                    Role = new Role { RoleName = "Admin" },
                    Status = new Status { StatusName = "Active" }
                }
            };
            _repoMock.Setup(x => x.GetAllAsync(2, 5, "abc", 1, 2)).ReturnsAsync(users);
            _repoMock.Setup(x => x.GetTotalAccountsAsync("abc", 1, 2)).ReturnsAsync(16);

            var service = CreateService();

            var result = await service.GetAllAccountsAsync(req);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Tải Lên Tài Khoản Thành Công.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.CurrentPage);
            Assert.Equal(5, result.Data.ItemsPerPage);
            Assert.Equal(16, result.Data.TotalItems);
            Assert.Equal(4, result.Data.TotalPages);
            var item = result.Data.Items.First();
            Assert.Equal(10, item.UserId);
            Assert.Equal("Test User", item.FullName);
            Assert.Equal("test@email.com", item.Email);
            Assert.Equal("123", item.Phone);
            Assert.Equal("Admin", item.RoleName);
            Assert.Equal("Active", item.StatusName);
        }

        [Fact(DisplayName = "UTCID06 - PageSize <= 0 should default to 10")]
        public async Task UTCID06_PageSizeZeroOrNegative_DefaultsTo10()
        {
            var req = new GetListAccountRequest { PageNumber = 1, PageSize = 0 };
            var users = new List<User>
            {
                new User
                {
                    UserId = 1,
                    FullName = "A",
                    Email = "a@email.com",
                    Phone = "123",
                    Role = new Role { RoleName = "R" },
                    Status = new Status { StatusName = "S" }
                }
            };
            _repoMock.Setup(x => x.GetAllAsync(1, 10, null, null, null)).ReturnsAsync(users);
            _repoMock.Setup(x => x.GetTotalAccountsAsync(null, null, null)).ReturnsAsync(1);

            var service = CreateService();

            var result = await service.GetAllAccountsAsync(req);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.NotNull(result.Data);
            Assert.Equal(10, result.Data.ItemsPerPage);
        }

        [Fact(DisplayName = "UTCID07 - Exception returns 500")]
        public async Task UTCID07_Exception_Returns500()
        {
            var req = new GetListAccountRequest { PageNumber = 1, PageSize = 1 };
            _repoMock.Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ThrowsAsync(new Exception("fail"));

            var service = CreateService();

            var result = await service.GetAllAccountsAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("fail", result.Message);
        }

        [Fact(DisplayName = "UTCID08 - User with null Role returns (Unknown Role)")]
        public async Task UTCID08_UserWithNullRole_ReturnsUnknownRole()
        {
            var req = new GetListAccountRequest { PageNumber = 1, PageSize = 1 };
            var users = new List<User>
    {
        new User
        {
            UserId = 1,
            FullName = "A",
            Email = "a@email.com",
            Phone = "123",
            Role = null, // <<< branch cần test
            Status = new Status { StatusName = "Active" }
        }
    };
            _repoMock.Setup(x => x.GetAllAsync(1, 1, null, null, null)).ReturnsAsync(users);
            _repoMock.Setup(x => x.GetTotalAccountsAsync(null, null, null)).ReturnsAsync(1);

            var service = CreateService();

            var result = await service.GetAllAccountsAsync(req);

            Assert.True(result.Success);
            Assert.Equal("(Unknown Role)", result.Data.Items.First().RoleName);
            Assert.Equal("Active", result.Data.Items.First().StatusName);
        }

        [Fact(DisplayName = "UTCID09 - User with null Status returns (Unknown Status)")]
        public async Task UTCID09_UserWithNullStatus_ReturnsUnknownStatus()
        {
            var req = new GetListAccountRequest { PageNumber = 1, PageSize = 1 };
            var users = new List<User>
    {
        new User
        {
            UserId = 1,
            FullName = "A",
            Email = "a@email.com",
            Phone = "123",
            Role = new Role { RoleName = "R" },
            Status = null // <<< branch cần test
        }
    };
            _repoMock.Setup(x => x.GetAllAsync(1, 1, null, null, null)).ReturnsAsync(users);
            _repoMock.Setup(x => x.GetTotalAccountsAsync(null, null, null)).ReturnsAsync(1);

            var service = CreateService();

            var result = await service.GetAllAccountsAsync(req);

            Assert.True(result.Success);
            Assert.Equal("R", result.Data.Items.First().RoleName);
            Assert.Equal("(Unknown Status)", result.Data.Items.First().StatusName);
        }

        [Fact(DisplayName = "UTCID10 - Exception with InnerException returns 500 and inner message")]
        public async Task UTCID10_ExceptionWithInnerException_Returns500AndInnerMessage()
        {
            var innerEx = new Exception("inner error");
            var outerEx = new Exception("outer error", innerEx);

            var req = new GetListAccountRequest { PageNumber = 1, PageSize = 1 };
            _repoMock.Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ThrowsAsync(outerEx);

            var service = CreateService();

            var result = await service.GetAllAccountsAsync(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_45, result.Message);
            Assert.Contains("outer error", result.Message);
            Assert.Contains("Inner: inner error", result.Message);
            Assert.Contains("StackTrace", result.Message);
            Assert.Null(result.Data);
        }
    }
}