using B2P_API.DTOs.FacilityDTOs;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;
using B2P_API.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace B2P_Test.UnitTest.FacilityService_UnitTest
{
    public class CreateFacilityTest
    {
        private readonly Mock<IFacilityManageRepository> _manageRepoMock = new();
        private readonly Mock<IFacilityRepositoryForUser> _userRepoMock = new();

        private FacilityService CreateService()
        {
            return new FacilityService(_manageRepoMock.Object, _userRepoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - FacilityName is null or whitespace returns 400")]
        public async Task UTCID01_FacilityNameIsNullOrWhitespace_Returns400()
        {
            var req = new CreateFacilityRequest
            {
                FacilityName = "   ",
                StatusId = 1,
                OpenHour = 7,
                CloseHour = 20,
                SlotDuration = 60
            };
            var service = CreateService();

            var result = await service.CreateFacility(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Tên cơ sở không được để trống hoặc chỉ chứa khoảng trắng", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - StatusId <= 0 returns 400")]
        public async Task UTCID02_StatusIdInvalid_Returns400()
        {
            var req = new CreateFacilityRequest
            {
                FacilityName = "Facility A",
                StatusId = 0,
                OpenHour = 7,
                CloseHour = 20,
                SlotDuration = 60
            };
            var service = CreateService();

            var result = await service.CreateFacility(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Trạng thái không hợp lệ", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - OpenHour >= CloseHour returns 400")]
        public async Task UTCID03_OpenHourGreaterOrEqualCloseHour_Returns400()
        {
            var req = new CreateFacilityRequest
            {
                FacilityName = "Facility A",
                StatusId = 1,
                OpenHour = 10,
                CloseHour = 10,
                SlotDuration = 60
            };
            var service = CreateService();

            var result = await service.CreateFacility(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Giờ mở cửa phải nhỏ hơn giờ đóng cửa", result.Message);
            Assert.Null(result.Data);

            req.CloseHour = 9;
            var result2 = await service.CreateFacility(req);
            Assert.False(result2.Success);
            Assert.Equal(400, result2.Status);
            Assert.Equal("Giờ mở cửa phải nhỏ hơn giờ đóng cửa", result2.Message);
            Assert.Null(result2.Data);
        }

        [Fact(DisplayName = "UTCID04 - SlotDuration invalid returns 400")]
        public async Task UTCID04_SlotDurationInvalid_Returns400()
        {
            var req = new CreateFacilityRequest
            {
                FacilityName = "Facility A",
                StatusId = 1,
                OpenHour = 7,
                CloseHour = 20,
                SlotDuration = 0 // <= 0
            };
            var service = CreateService();

            var result = await service.CreateFacility(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Thời lượng mỗi lượt phải từ 1 đến 180 phút", result.Message);
            Assert.Null(result.Data);

            req.SlotDuration = 181; // > 180
            var result2 = await service.CreateFacility(req);

            Assert.False(result2.Success);
            Assert.Equal(400, result2.Status);
            Assert.Equal("Thời lượng mỗi lượt phải từ 1 đến 180 phút", result2.Message);
            Assert.Null(result2.Data);
        }

        [Fact(DisplayName = "UTCID05 - Exception in repository returns 500")]
        public async Task UTCID05_ExceptionInRepository_Returns500()
        {
            var req = new CreateFacilityRequest
            {
                FacilityName = "Facility A",
                StatusId = 1,
                OpenHour = 7,
                CloseHour = 20,
                SlotDuration = 60
            };
            _manageRepoMock.Setup(x => x.CreateFacilityAsync(It.IsAny<Facility>()))
                .ThrowsAsync(new Exception("db error"));

            var service = CreateService();

            var result = await service.CreateFacility(req);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.StartsWith("Tạo cơ sở thất bại:", result.Message);
            Assert.Contains("db error", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID06 - Success returns 200")]
        public async Task UTCID06_Success_Returns200()
        {
            var req = new CreateFacilityRequest
            {
                FacilityName = "Facility A",
                StatusId = 1,
                OpenHour = 7,
                CloseHour = 9,
                SlotDuration = 60,
                Location = "Hanoi",
                Contact = "123456",
                UserId = 11
            };
            var created = new Facility
            {
                FacilityId = 123,
                FacilityName = "Facility A",
                StatusId = 1,
                UserId = 11,
                Location = "Hanoi",
                Contact = "123456",
            };
            _manageRepoMock.Setup(x => x.CreateFacilityAsync(It.IsAny<Facility>())).ReturnsAsync(created);

            var service = CreateService();

            var result = await service.CreateFacility(req);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Tạo cơ sở thành công", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(123, result.Data.FacilityId);
            Assert.Equal("Facility A", result.Data.FacilityName);
            Assert.Equal(11, result.Data.UserId);
            Assert.Equal("Hanoi", result.Data.Location);
            Assert.Equal("123456", result.Data.Contact);

            // Check TimeSlots created
            Assert.NotNull(result.Data.TimeSlots);
            // Số slot: Open: 7h, Close: 9h, SlotDuration: 60 => 2 slots (7-8, 8-9)
            Assert.True(result.Data.TimeSlots.Count >= 0);
        }
    }
}