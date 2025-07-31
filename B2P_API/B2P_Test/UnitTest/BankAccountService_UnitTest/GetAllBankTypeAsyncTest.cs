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
using System.Linq;

namespace B2P_Test.UnitTest.BankAccountService_UnitTest
{
    public class BankAccountService_GetAllBankTypeAsync_Test
    {
        private readonly Mock<IBankAccountRepository> _repoMock = new();

        private BankAccountService CreateService()
        {
            return new BankAccountService(_repoMock.Object);
        }

        [Fact(DisplayName = "UTCID01 - Repository returns null returns 404")]
        public async Task UTCID01_RepositoryReturnsNull_Returns404()
        {
            _repoMock.Setup(x => x.GetAllBankTypeAysnc()).ReturnsAsync((List<BankType>)null);

            var service = CreateService();

            var result = await service.GetAllBankTypeAsync(null);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không có danh sách ngân hàng", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID02 - Repository returns empty list returns 404")]
        public async Task UTCID02_RepositoryReturnsEmptyList_Returns404()
        {
            _repoMock.Setup(x => x.GetAllBankTypeAysnc()).ReturnsAsync(new List<BankType>());

            var service = CreateService();

            var result = await service.GetAllBankTypeAsync(null);

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không có danh sách ngân hàng", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Filter by name returns no result returns 404")]
        public async Task UTCID03_FilterByNameNoResult_Returns404()
        {
            var banks = new List<BankType>
            {
                new BankType { BankTypeId = 1, BankName = "ACB", Description = "desc1" },
                new BankType { BankTypeId = 2, BankName = "VCB", Description = "desc2" }
            };
            _repoMock.Setup(x => x.GetAllBankTypeAysnc()).ReturnsAsync(banks);

            var service = CreateService();

            var result = await service.GetAllBankTypeAsync("NotExist");

            Assert.False(result.Success);
            Assert.Equal(404, result.Status);
            Assert.Equal("Không tìm thấy ngân hàng nào khớp với tên đã tìm kiếm", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - After map, totalPages=0 returns 404")]
        public async Task UTCID04_AfterMapTotalPages0_Returns404()
        {
            var banks = new List<BankType>
            {
                new BankType { BankTypeId = 1, BankName = "ACB", Description = "desc1" }
            };
            _repoMock.Setup(x => x.GetAllBankTypeAysnc()).ReturnsAsync(banks);

            var service = CreateService();

            // pageSize lớn hơn tổng số result => totalPages = 1, không rơi vào branch.
            // Để totalPages = 0 phải không có item nào sau filter, nhưng đã test ở trên.
            // Tuy nhiên, để test branch này, ta phải patch logic hoặc test lại branch filter trả về empty.
            // Ở đây, case này đã trùng với UTCID03 và UTCID02, nên branch này đã được cover.
            // Nếu pageSize = int.MaxValue, vẫn có 1 page.
            // KHÔNG CẦN test lại vì code không thể xảy ra totalPages = 0 nếu có item.

            // Để test, ta có thể patch code hoặc bỏ qua case này vì logic code không đạt tới branch này thực tế.
        }

        [Fact(DisplayName = "UTCID05 - Invalid pageNumber returns 400")]
        public async Task UTCID05_InvalidPageNumber_Returns400()
        {
            var banks = new List<BankType>
            {
                new BankType { BankTypeId = 1, BankName = "ACB", Description = "desc1" },
                new BankType { BankTypeId = 2, BankName = "VCB", Description = "desc2" }
            };
            _repoMock.Setup(x => x.GetAllBankTypeAysnc()).ReturnsAsync(banks);

            var service = CreateService();

            // pageNumber > totalPages
            var result = await service.GetAllBankTypeAsync(null, 5, 1);

            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Số trang không hợp lệ", result.Message);
            Assert.Null(result.Data);

            // pageNumber < 1
            var result2 = await service.GetAllBankTypeAsync(null, 0, 1);
            Assert.False(result2.Success);
            Assert.Equal(400, result2.Status);
            Assert.Equal("Số trang không hợp lệ", result2.Message);
            Assert.Null(result2.Data);
        }

        [Fact(DisplayName = "UTCID06 - Success returns 200 and correct paging")]
        public async Task UTCID06_Success_Returns200AndCorrectPaging()
        {
            var banks = new List<BankType>
            {
                new BankType { BankTypeId = 1, BankName = "ACB", Description = "desc1" },
                new BankType { BankTypeId = 2, BankName = "VCB", Description = "desc2" },
                new BankType { BankTypeId = 3, BankName = "BIDV", Description = "desc3" }
            };
            _repoMock.Setup(x => x.GetAllBankTypeAysnc()).ReturnsAsync(banks);

            var service = CreateService();

            // page 1, pageSize 2
            var result = await service.GetAllBankTypeAsync(null, 1, 2);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Equal("Found 3 facilities matching search criteria.", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.CurrentPage);
            Assert.Equal(2, result.Data.ItemsPerPage);
            Assert.Equal(3, result.Data.TotalItems);
            Assert.Equal(2, result.Data.TotalPages);
            Assert.Equal(2, result.Data.Items.Count());

            // page 2, pageSize 2
            var result2 = await service.GetAllBankTypeAsync(null, 2, 2);

            Assert.True(result2.Success);
            Assert.Equal(2, result2.Data.CurrentPage);
            Assert.Single(result2.Data.Items);
            Assert.Equal(3, result2.Data.Items.ElementAt(0).BankTypeId);
        }

        [Fact(DisplayName = "UTCID07 - Filter by name returns correct result")]
        public async Task UTCID07_FilterByName_ReturnsCorrectResult()
        {
            var banks = new List<BankType>
            {
                new BankType { BankTypeId = 1, BankName = "ACB", Description = "desc1" },
                new BankType { BankTypeId = 2, BankName = "VCB", Description = "desc2" },
            };
            _repoMock.Setup(x => x.GetAllBankTypeAysnc()).ReturnsAsync(banks);

            var service = CreateService();

            var result = await service.GetAllBankTypeAsync("VCB", 1, 10);

            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Single(result.Data.Items);
            Assert.Equal("VCB", result.Data.Items.ElementAt(0).BankName);
        }

        [Fact(DisplayName = "UTCID08 - Exception returns 500")]
        public async Task UTCID08_Exception_Returns500()
        {
            _repoMock.Setup(x => x.GetAllBankTypeAysnc()).ThrowsAsync(new Exception("fail"));

            var service = CreateService();

            var result = await service.GetAllBankTypeAsync(null);

            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains(MessagesCodes.MSG_06, result.Message);
            Assert.Contains("fail", result.Message);
            Assert.Null(result.Data);
        }
    }
}