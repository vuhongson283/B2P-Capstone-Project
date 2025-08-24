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

// Fake repo để pass constructor
public class FacilityManageRepositoryFake : IFacilityManageRepository
{
    public Task<List<Facility>> GetByUserIdAsync(int userId) => Task.FromResult(new List<Facility>());
    public Task<Facility> GetByIdAsync(int facilityId) => throw new NotImplementedException();
    public Task<Facility> CreateFacilityAsync(Facility facility) => throw new NotImplementedException();
    public Task<Facility> UpdateAsync(Facility facility) => throw new NotImplementedException();
    public Task<bool> HasActiveBookingsAsync(int facilityId) => throw new NotImplementedException();
    public Task<bool> DeleteCascadeAsync(int facilityId) => throw new NotImplementedException();
    public Task<List<Facility>> GetAllAsync() => throw new NotImplementedException();
    public Task<bool> DeleteAsync(int id) => throw new NotImplementedException();
}

public class FacilityRepositoryForUserFake : IFacilityRepositoryForUser
{
    public Task<List<Facility>> GetAllFacilitiesByPlayer() => throw new NotImplementedException();
    public Task<B2P_API.DTOs.FacilityDTO.FacilityDetailsDto> GetFacilityDetails(int facilityId) => throw new NotImplementedException();
}

// Fake FacilityService dùng cho test. KHÔNG override, chỉ shadow method.
public class FacilityServiceFake : FacilityService
{
    private readonly bool _result;
    public FacilityServiceFake(bool result = true)
        : base(new FacilityManageRepositoryFake(), new FacilityRepositoryForUserFake())
    {
        _result = result;
    }
    // Shadow new method
    public new Task<bool> UpdateFacilitiesStatusByUserIdAsync(int userId, int statusId)
        => Task.FromResult(_result);
}

namespace B2P_Test.UnitTest.AccountManagementService_UnitTest
{
    public class AccountManagementService_BanUserAsync_Test
    {
        private readonly Mock<IAccountManagementRepository> _repoMock = new();

        private AccountManagementService CreateService(FacilityService facilityService = null)
        {
            return new AccountManagementService(_repoMock.Object, facilityService, null);
        }

        [Fact(DisplayName = "UTCID01 - User not found returns 404")]
        public async Task UTCID01_UserNotFound_Returns404()
        {
            _repoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((User)null);

            var service = CreateService(null);

            var result = await service.BanUserAsync(1);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal(MessagesCodes.MSG_46, result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - User already banned returns 400")]
        public async Task UTCID02_UserAlreadyBanned_Returns400()
        {
            var user = new User { UserId = 2, StatusId = 4 };
            _repoMock.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(user);

            var service = CreateService(null);

            var result = await service.BanUserAsync(2);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tài khoản này đã bị cấm rồi.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Ban user success returns 200")]
        public async Task UTCID03_BanUserSuccess_Returns200()
        {
            var user = new User { UserId = 3, StatusId = 1 };
            _repoMock.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(user);
            _repoMock.Setup(x => x.UpdateStatusAsync(user, 4)).ReturnsAsync(true);

            // Cast về base type để gọi đúng method thật
            FacilityService facilityService = new FacilityServiceFake(true);

            var service = CreateService(facilityService);

            var result = await service.BanUserAsync(3);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Khoá tài khoản thành công.", result.Message);
            Assert.Equal("3", result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Exception returns 500")]
        public async Task UTCID04_Exception_Returns500()
        {
            _repoMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ThrowsAsync(new Exception("fail"));

            var service = CreateService(null);

            var result = await service.BanUserAsync(99);

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

            var result = await service.BanUserAsync(999);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_37, result.Message);
            Assert.Contains("outer error", result.Message);
            Assert.Contains("Inner: inner error", result.Message);
            Assert.Null(result.Data);
        }
    }
}