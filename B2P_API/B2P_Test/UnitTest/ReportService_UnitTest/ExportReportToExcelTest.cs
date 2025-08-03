using B2P_API.Response;
using B2P_API.Services;
using B2P_API.Interface;
using Moq;
using Xunit;

namespace B2P_Test.UnitTest.ReportService_UnitTest
{
    public class ExcelExportServiceTest
    {
        private readonly Mock<IExcelExportService> _mockExcelService;

        // Test model class
        public class TestReportItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime CreatedDate { get; set; }
            public decimal Amount { get; set; }
            public bool IsActive { get; set; }
        }

        public ExcelExportServiceTest()
        {
            _mockExcelService = new Mock<IExcelExportService>();
        }

        [Fact(DisplayName = "UTCID01 - Xuất Excel thành công với dữ liệu hợp lệ")]
        public async Task UTCID01_ValidData_ExportsExcelSuccessfully()
        {
            // Arrange
            var testData = new List<TestReportItem>
            {
                new TestReportItem { Id = 1, Name = "Item 1", CreatedDate = DateTime.Now, Amount = 1000.50m, IsActive = true },
                new TestReportItem { Id = 2, Name = "Item 2", CreatedDate = DateTime.Now.AddDays(-1), Amount = 2000.75m, IsActive = false }
            };

            var pagedData = new PagedResponse<TestReportItem>
            {
                Items = testData,
                TotalItems = 2,
                CurrentPage = 1,
                TotalPages = 1,
                ItemsPerPage = 10
            };

            var expectedBytes = new byte[] { 80, 75, 3, 4 }; // Mock Excel file bytes (ZIP header)
            var expectedResponse = new ApiResponse<byte[]>
            {
                Success = true,
                Message = "Xuất file Excel thành công với 2 bản ghi (tổng cộng 2 bản ghi).",
                Status = 200,
                Data = expectedBytes
            };

            _mockExcelService
                .Setup(x => x.ExportToExcelAsync(pagedData, "Test Report", null))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _mockExcelService.Object.ExportToExcelAsync(pagedData, "Test Report");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Contains("Xuất file Excel thành công với 2 bản ghi", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(expectedBytes, result.Data);
            Assert.True(result.Data.Length > 0);

            // Verify method was called
            _mockExcelService.Verify(x => x.ExportToExcelAsync(pagedData, "Test Report", null), Times.Once);
        }

        [Fact(DisplayName = "UTCID02 - Dữ liệu null")]
        public async Task UTCID02_NullData_ReturnsFailure()
        {
            // Arrange
            PagedResponse<TestReportItem> pagedData = null;
            var expectedResponse = new ApiResponse<byte[]>
            {
                Success = false,
                Message = "Không có dữ liệu để xuất.",
                Status = 400,
                Data = null
            };

            _mockExcelService
                .Setup(x => x.ExportToExcelAsync<TestReportItem>(null, "Data", null))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _mockExcelService.Object.ExportToExcelAsync<TestReportItem>(null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Không có dữ liệu để xuất.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID03 - Items rỗng")]
        public async Task UTCID03_EmptyItems_ReturnsFailure()
        {
            // Arrange
            var pagedData = new PagedResponse<TestReportItem>
            {
                Items = new List<TestReportItem>(),
                TotalItems = 0,
                CurrentPage = 1,
                TotalPages = 1,
                ItemsPerPage = 10
            };

            var expectedResponse = new ApiResponse<byte[]>
            {
                Success = false,
                Message = "Không có dữ liệu để xuất.",
                Status = 400,
                Data = null
            };

            _mockExcelService
                .Setup(x => x.ExportToExcelAsync(pagedData, "Data", null))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _mockExcelService.Object.ExportToExcelAsync(pagedData);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.Status);
            Assert.Equal("Không có dữ liệu để xuất.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID04 - Xuất với tên sheet mặc định")]
        public async Task UTCID04_DefaultSheetName_ExportsSuccessfully()
        {
            // Arrange
            var testData = new List<TestReportItem>
            {
                new TestReportItem { Id = 1, Name = "Item 1", CreatedDate = DateTime.Now, Amount = 1000m, IsActive = true }
            };

            var pagedData = new PagedResponse<TestReportItem>
            {
                Items = testData,
                TotalItems = 1,
                CurrentPage = 1,
                TotalPages = 1,
                ItemsPerPage = 10
            };

            var expectedBytes = new byte[] { 80, 75, 3, 4, 20, 0 };
            var expectedResponse = new ApiResponse<byte[]>
            {
                Success = true,
                Message = "Xuất file Excel thành công với 1 bản ghi (tổng cộng 1 bản ghi).",
                Status = 200,
                Data = expectedBytes
            };

            _mockExcelService
                .Setup(x => x.ExportToExcelAsync(pagedData, "Data", null))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _mockExcelService.Object.ExportToExcelAsync(pagedData);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.NotNull(result.Data);
            Assert.Equal(expectedBytes, result.Data);
        }

        [Fact(DisplayName = "UTCID05 - Xuất với column mappings tùy chỉnh")]
        public async Task UTCID05_CustomColumnMappings_ExportsSuccessfully()
        {
            // Arrange
            var testData = new List<TestReportItem>
            {
                new TestReportItem { Id = 1, Name = "Item 1", CreatedDate = DateTime.Now, Amount = 1000m, IsActive = true }
            };

            var pagedData = new PagedResponse<TestReportItem>
            {
                Items = testData,
                TotalItems = 1,
                CurrentPage = 1,
                TotalPages = 1,
                ItemsPerPage = 10
            };

            var customMappings = new Dictionary<string, Func<TestReportItem, object>>
            {
                ["Mã ID"] = item => item.Id,
                ["Tên"] = item => item.Name,
                ["Số tiền"] = item => item.Amount,
                ["Trạng thái"] = item => item.IsActive ? "Hoạt động" : "Không hoạt động"
            };

            var expectedBytes = new byte[] { 80, 75, 3, 4, 20, 0, 6, 0 };
            var expectedResponse = new ApiResponse<byte[]>
            {
                Success = true,
                Message = "Xuất file Excel thành công với 1 bản ghi (tổng cộng 1 bản ghi).",
                Status = 200,
                Data = expectedBytes
            };

            _mockExcelService
                .Setup(x => x.ExportToExcelAsync(pagedData, "Custom Report", customMappings))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _mockExcelService.Object.ExportToExcelAsync(pagedData, "Custom Report", customMappings);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.NotNull(result.Data);
            Assert.Equal(expectedBytes, result.Data);

            // Verify custom mappings were passed
            _mockExcelService.Verify(x => x.ExportToExcelAsync(pagedData, "Custom Report", customMappings), Times.Once);
        }

        [Fact(DisplayName = "UTCID06 - Xuất với dữ liệu phân trang")]
        public async Task UTCID06_PaginatedData_ExportsWithPagingInfo()
        {
            // Arrange
            var testData = new List<TestReportItem>
            {
                new TestReportItem { Id = 11, Name = "Item 11", CreatedDate = DateTime.Now, Amount = 1000m, IsActive = true },
                new TestReportItem { Id = 12, Name = "Item 12", CreatedDate = DateTime.Now, Amount = 2000m, IsActive = false }
            };

            var pagedData = new PagedResponse<TestReportItem>
            {
                Items = testData,
                TotalItems = 25, // Total 25 records
                CurrentPage = 2,  // Page 2
                TotalPages = 3,   // Total 3 pages
                ItemsPerPage = 10 // 10 records per page
            };

            var expectedBytes = new byte[] { 80, 75, 3, 4, 20, 0, 6, 0, 8, 0 };
            var expectedResponse = new ApiResponse<byte[]>
            {
                Success = true,
                Message = "Xuất file Excel thành công với 2 bản ghi (tổng cộng 25 bản ghi).",
                Status = 200,
                Data = expectedBytes
            };

            _mockExcelService
                .Setup(x => x.ExportToExcelAsync(pagedData, "Paged Report", null))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _mockExcelService.Object.ExportToExcelAsync(pagedData, "Paged Report");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(200, result.Status);
            Assert.Contains("Xuất file Excel thành công với 2 bản ghi (tổng cộng 25 bản ghi)", result.Message);
            Assert.NotNull(result.Data);
            Assert.Equal(expectedBytes, result.Data);
        }

        [Fact(DisplayName = "UTCID07 - Xử lý exception")]
        public async Task UTCID07_ExceptionThrown_ReturnsErrorResponse()
        {
            // Arrange
            var testData = new List<TestReportItem>
            {
                new TestReportItem { Id = 1, Name = "Item 1", CreatedDate = DateTime.Now, Amount = 1000m, IsActive = true }
            };

            var pagedData = new PagedResponse<TestReportItem>
            {
                Items = testData,
                TotalItems = 1,
                CurrentPage = 1,
                TotalPages = 1,
                ItemsPerPage = 10
            };

            var expectedResponse = new ApiResponse<byte[]>
            {
                Success = false,
                Message = "Lỗi khi xuất file Excel: System error occurred",
                Status = 500,
                Data = null
            };

            _mockExcelService
                .Setup(x => x.ExportToExcelAsync(pagedData, "Error Report", null))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _mockExcelService.Object.ExportToExcelAsync(pagedData, "Error Report");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.Status);
            Assert.Contains("Lỗi khi xuất file Excel:", result.Message);
            Assert.Null(result.Data);
        }

        [Fact(DisplayName = "UTCID08 - Kiểm tra tham số truyền vào")]
        public async Task UTCID08_ParameterValidation_CallsWithCorrectParameters()
        {
            // Arrange
            var testData = new List<TestReportItem>
            {
                new TestReportItem { Id = 1, Name = "Test Item", CreatedDate = DateTime.Now, Amount = 500m, IsActive = true }
            };

            var pagedData = new PagedResponse<TestReportItem>
            {
                Items = testData,
                TotalItems = 1,
                CurrentPage = 1,
                TotalPages = 1,
                ItemsPerPage = 10
            };

            var customSheetName = "Parameter Test";
            var customMappings = new Dictionary<string, Func<TestReportItem, object>>
            {
                ["ID"] = item => item.Id,
                ["Name"] = item => item.Name
            };

            var expectedResponse = new ApiResponse<byte[]>
            {
                Success = true,
                Message = "Success",
                Status = 200,
                Data = new byte[] { 1, 2, 3 }
            };

            _mockExcelService
                .Setup(x => x.ExportToExcelAsync(pagedData, customSheetName, customMappings))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _mockExcelService.Object.ExportToExcelAsync(pagedData, customSheetName, customMappings);

            // Assert
            Assert.True(result.Success);

            // Verify exact parameters were passed
            _mockExcelService.Verify(x => x.ExportToExcelAsync(
                It.Is<PagedResponse<TestReportItem>>(p => p.Items.Count() == 1 && p.TotalItems == 1),
                It.Is<string>(s => s == customSheetName),
                It.Is<Dictionary<string, Func<TestReportItem, object>>>(d => d.ContainsKey("ID") && d.ContainsKey("Name"))
            ), Times.Once);
        }

        [Fact(DisplayName = "UTCID09 - Kiểm tra output byte array không rỗng")]
        public async Task UTCID09_OutputValidation_ByteArrayNotEmpty()
        {
            // Arrange
            var testData = new List<TestReportItem>
            {
                new TestReportItem { Id = 1, Name = "Test", CreatedDate = DateTime.Now, Amount = 100m, IsActive = true }
            };

            var pagedData = new PagedResponse<TestReportItem>
            {
                Items = testData,
                TotalItems = 1,
                CurrentPage = 1,
                TotalPages = 1,
                ItemsPerPage = 10
            };

            var mockBytes = new byte[1024]; // Mock 1KB file
            for (int i = 0; i < mockBytes.Length; i++)
            {
                mockBytes[i] = (byte)(i % 256);
            }

            var expectedResponse = new ApiResponse<byte[]>
            {
                Success = true,
                Message = "File created successfully",
                Status = 200,
                Data = mockBytes
            };

            _mockExcelService
                .Setup(x => x.ExportToExcelAsync(pagedData, "Output Test", null))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _mockExcelService.Object.ExportToExcelAsync(pagedData, "Output Test");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.Length > 0);
            Assert.Equal(1024, result.Data.Length);
            Assert.Equal(mockBytes, result.Data);
        }

        [Fact(DisplayName = "UTCID10 - Kiểm tra message format")]
        public async Task UTCID10_MessageFormat_CorrectFormat()
        {
            // Arrange
            var testData = new List<TestReportItem>();
            for (int i = 1; i <= 15; i++)
            {
                testData.Add(new TestReportItem { Id = i, Name = $"Item {i}", CreatedDate = DateTime.Now, Amount = i * 100m, IsActive = true });
            }

            var pagedData = new PagedResponse<TestReportItem>
            {
                Items = testData,
                TotalItems = 150, // Total records in database
                CurrentPage = 1,
                TotalPages = 10,
                ItemsPerPage = 15 // Current page size
            };

            var expectedResponse = new ApiResponse<byte[]>
            {
                Success = true,
                Message = "Xuất file Excel thành công với 15 bản ghi (tổng cộng 150 bản ghi).",
                Status = 200,
                Data = new byte[] { 1, 2, 3, 4, 5 }
            };

            _mockExcelService
                .Setup(x => x.ExportToExcelAsync(pagedData, "Message Test", null))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _mockExcelService.Object.ExportToExcelAsync(pagedData, "Message Test");

            // Assert
            Assert.True(result.Success);
            Assert.Contains("15 bản ghi", result.Message);
            Assert.Contains("150 bản ghi", result.Message);
            Assert.Contains("Xuất file Excel thành công", result.Message);
        }
    }
}