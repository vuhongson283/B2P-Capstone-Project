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

            // Test null case
            req.FacilityName = null;
            var result2 = await service.CreateFacility(req);
            Assert.False(result2.Success);
            Assert.Equal(400, result2.Status);
            Assert.Equal("Tên cơ sở không được để trống hoặc chỉ chứa khoảng trắng", result2.Message);
            Assert.Null(result2.Data);
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

            req.StatusId = -1;
            var result2 = await service.CreateFacility(req);
            Assert.False(result2.Success);
            Assert.Equal(400, result2.Status);
            Assert.Equal("Trạng thái không hợp lệ", result2.Message);
            Assert.Null(result2.Data);
        }

        [Fact(DisplayName = "UTCID03 - OpenHour or CloseHour invalid returns 400")]
        public async Task UTCID03_OpenHourOrCloseHourInvalid_Returns400()
        {
            var req = new CreateFacilityRequest
            {
                FacilityName = "Facility A",
                StatusId = 1,
                OpenHour = -1, // invalid
                CloseHour = 20,
                SlotDuration = 60
            };
            var service = CreateService();

            var result = await service.CreateFacility(req);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Giờ mở/đóng cửa không hợp lệ (0-24)", result.Message);
            Assert.Null(result.Data);

            req.OpenHour = 7;
            req.CloseHour = 25; // invalid
            var result2 = await service.CreateFacility(req);

            Assert.False(result2.Success);
            Assert.Equal(400, result2.Status);
            Assert.Equal("Giờ mở/đóng cửa không hợp lệ (0-24)", result2.Message);
            Assert.Null(result2.Data);
        }

        [Fact(DisplayName = "UTCID04 - OpenHour >= CloseHour returns 400")]
        public async Task UTCID04_OpenHourGreaterOrEqualCloseHour_Returns400()
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

            req.OpenHour = 11;
            req.CloseHour = 10;
            var result2 = await service.CreateFacility(req);
            Assert.False(result2.Success);
            Assert.Equal(400, result2.Status);
            Assert.Equal("Giờ mở cửa phải nhỏ hơn giờ đóng cửa", result2.Message);
            Assert.Null(result2.Data);
        }

        [Fact(DisplayName = "UTCID05 - SlotDuration invalid returns 400")]
        public async Task UTCID05_SlotDurationInvalid_Returns400()
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

        [Fact(DisplayName = "UTCID06 - Exception in repository returns 500")]
        public async Task UTCID06_ExceptionInRepository_Returns500()
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

        [Fact(DisplayName = "UTCID07 - Success returns 200")]
        public async Task UTCID07_Success_Returns200()
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
                TimeSlots = new List<TimeSlot>() // For assertion
            };
            _manageRepoMock.Setup(x => x.CreateFacilityAsync(It.IsAny<Facility>())).ReturnsAsync(created);

            var service = CreateService();

            var result = await service.CreateFacility(req);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Tạo cơ sở thành công với 2 khung giờ", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(123, result.Data.FacilityId);
            Assert.Equal("Facility A", result.Data.FacilityName);
            Assert.Equal(11, result.Data.UserId);
            Assert.Equal("Hanoi", result.Data.Location);
            Assert.Equal("123456", result.Data.Contact);
        }

        [Fact(DisplayName = "UTCID08 - OpenHour 0 and CloseHour 23 returns 200 (full day minus last hour)")]
        public async Task UTCID08_OpenHourZeroAndCloseHourTwentyThree_Returns200()
        {
            var req = new CreateFacilityRequest
            {
                FacilityName = "Facility A",
                StatusId = 1,
                OpenHour = 0,
                CloseHour = 23,
                SlotDuration = 60
            };
            var created = new Facility { FacilityId = 8, FacilityName = "Facility A", StatusId = 1, UserId = 1, TimeSlots = new List<TimeSlot>() };
            _manageRepoMock.Setup(x => x.CreateFacilityAsync(It.IsAny<Facility>())).ReturnsAsync(created);
            var service = CreateService();

            var result = await service.CreateFacility(req);

            if (!result.Success)
                Console.WriteLine($"[DEBUG UTCID08] Status: {result.Status} - Message: {result.Message}");

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Contains("khung giờ", result.Message);
            Assert.NotNull(result.Data);
        }
    }
}